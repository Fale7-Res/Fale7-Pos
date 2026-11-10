namespace Falah.Infrastructure.Shifts;

using System.Text.Json;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Printing;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// UI-REQ-002: builds the closing statement inside the close's transaction and reads the frozen copy
/// back. The frozen copy lives on the close's own audit event (AuditEvents.NewValue): the audit trail
/// is append-only by design, it is written in the same transaction as the close, and it needs no
/// schema change - adding a table would be a production migration for no behavioural gain.
/// </summary>
public static class ShiftCloseStatements
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);
    private const string Marker = "{\"Kind\":\"" + ShiftCloseStatement.KindName + "\"";

    public static string Serialize(ShiftCloseStatement statement) => JsonSerializer.Serialize(statement, Json);

    /// <summary>The frozen statement of a closed shift, or null for a shift closed before statements
    /// were frozen (or not closed at all).</summary>
    public static async Task<ShiftCloseStatement?> LoadFrozenAsync(FalahDbContext db, string shiftId, CancellationToken ct)
    {
        var json = await db.AuditEvents.AsNoTracking()
            .Where(e => e.ShiftId == shiftId && e.Category == "shift" && e.NewValue != null && e.NewValue.StartsWith(Marker))
            .OrderBy(e => e.TimestampUtc)
            .Select(e => e.NewValue)
            .FirstOrDefaultAsync(ct);
        return json is null ? null : JsonSerializer.Deserialize<ShiftCloseStatement>(json, Json);
    }

    /// <summary>
    /// The statement for a shift whose closing figures are known. <paramref name="expected"/>,
    /// <paramref name="counted"/> and <paramref name="difference"/> are the values the close persisted;
    /// everything else is read from the same records and rules every other report uses. Reads run
    /// AsNoTracking, so the close's own reconciliation and handover movements (added but not yet saved)
    /// are not inputs - exactly as the reconciliation itself requires.
    /// </summary>
    public static async Task<ShiftCloseStatement> BuildAsync(
        FalahDbContext db, ReportingQueryService reporting, ShiftEntity shift,
        string cashierName, string closedByName, string? approvedByName, DateTimeOffset closedAtUtc,
        long expected, long counted, long difference, string? differenceReason, CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
        var breakdown = await ShiftReconciliation.ComputeBreakdownAsync(db, shift.Id, Money.FromPiastres(shift.OpeningCashPiastres), ct);
        var sales = await reporting.ComputeShiftSalesAsync(shift.Id, ct);
        var cancelled = await db.Orders.AsNoTracking().CountAsync(o => o.ShiftId == shift.Id && o.Status == "cancelled", ct);

        var expenses = await db.Expenses.AsNoTracking().Where(e => e.ShiftId == shift.Id).OrderBy(e => e.CreatedAtUtc).ToListAsync(ct);
        var userIds = expenses.Select(e => e.CreatedByUserId).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        // "يوم العمل" is the business day the shift ran in. shift.BusinessDate is the calendar date of the
        // opening, which differs after midnight; it is only the fallback for shifts that predate BusinessDayId.
        var businessDate = shift.BusinessDayId is null ? shift.BusinessDate
            : await db.BusinessDays.AsNoTracking().Where(d => d.Id == shift.BusinessDayId).Select(d => (DateOnly?)d.BusinessDate).FirstOrDefaultAsync(ct)
              ?? shift.BusinessDate;

        return new ShiftCloseStatement(
            ShiftCloseStatement.KindName, ShiftCloseStatement.CurrentSchemaVersion,
            settings?.RestaurantName ?? "", settings?.SubName,
            shift.Id, ShiftReference(shift.Id), shift.ShiftName, businessDate,
            shift.OpenedAtUtc, closedAtUtc, cashierName, closedByName, approvedByName,
            new ShiftStatementSales(
                sales.OrderCount, sales.ListSales.Piastres, sales.OffersValue.Piastres, sales.CompsValue.Piastres,
                sales.ManualDiscounts.Piastres, sales.GrossSales.Piastres, sales.Refunds.Piastres, sales.NetSales.Piastres,
                sales.Cash.Piastres, sales.Card.Piastres, sales.InstaPay.Piastres, sales.Wallet.Piastres,
                sales.PendingElectronicSales.Piastres, sales.CashWithDrivers.Piastres, sales.CollectedSales.Piastres, cancelled),
            expenses.Select(e => new ShiftStatementExpense(
                e.Category, e.Description, e.AmountPiastres, e.PaidFromDrawer, AccountName(e.PaymentAccountId),
                names.GetValueOrDefault(e.CreatedByUserId) ?? e.CreatedByUserId, e.CreatedAtUtc)).ToList(),
            new ShiftStatementCash(
                breakdown.OpeningCash.Piastres, breakdown.CashSales.Piastres, breakdown.Refunds.Piastres, breakdown.CashIn.Piastres,
                breakdown.Expenses.Piastres, breakdown.Withdrawals.Piastres, breakdown.SafeDrops.Piastres,
                expected, counted, difference,
                HandedOverPiastres: Math.Max(0, counted), HandoverDestinationName: AccountName(TreasuryAccounts.Main)),
            differenceReason,
            await LoadOutstandingAsync(db, shift.Id, ct));
    }

    /// <summary>What is still open as the shift is handed over - the same three checks the handover
    /// slip has always warned about, now frozen with the close. Driver custody is restaurant-wide
    /// (custody is not owned by a shift), and the line says so.</summary>
    private static async Task<List<string>> LoadOutstandingAsync(FalahDbContext db, string shiftId, CancellationToken ct)
    {
        var items = new List<string>();
        var pending = await db.Orders.AsNoTracking()
            .Where(o => o.ShiftId == shiftId && o.ElectronicPaymentStatus == "pending")
            .Select(o => new { o.OrderNumber }).ToListAsync(ct);
        if (pending.Count > 0)
            items.Add($"تحويلات إلكترونية بانتظار التأكيد: {pending.Count} ({string.Join("، ", pending.Select(p => p.OrderNumber))})");

        var unpaid = await db.Orders.AsNoTracking()
            .CountAsync(o => o.ShiftId == shiftId && !o.IsPaid && o.Status != "cancelled" && o.ElectronicPaymentStatus == null, ct);
        if (unpaid > 0) items.Add($"طلبات غير مدفوعة: {unpaid}");

        var custody = await db.DriverCustodies.AsNoTracking().Where(c => c.Status == "Open").Select(c => c.ExpectedPiastres).ToListAsync(ct);
        if (custody.Count > 0)
            items.Add($"عهدة سائقين غير مسواة (على مستوى المطعم): {custody.Count} بقيمة {Money.FromPiastres(custody.Sum()).Egp} ج");
        return items;
    }

    /// <summary>Same short code the shift slips have always printed.</summary>
    public static string ShiftReference(string shiftId) =>
        shiftId.Length <= 6 ? shiftId.ToUpperInvariant() : shiftId[^6..].ToUpperInvariant();

    public static string AccountName(string? accountId) => accountId switch
    {
        TreasuryAccounts.Drawer => "درج الكاشير",
        TreasuryAccounts.Main => "الخزينة الرئيسية",
        TreasuryAccounts.Bank => "البنك",
        TreasuryAccounts.InstaPay => "إنستاباي",
        TreasuryAccounts.Wallet => "المحفظة",
        null or "" => "غير محدد",
        var other => other,
    };
}
