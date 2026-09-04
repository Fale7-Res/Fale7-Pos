namespace Falah.Infrastructure.Expenses;

using Falah.Application.Expenses;
using Falah.Domain.Common;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from expensesVisibleToActor in the reference app's lib/expense-visibility.mjs.
/// A cashier sees only their OWN open shift's drawer-paid expenses (so they can reconcile their own
/// drawer, and nothing about anyone else's spending); everyone else sees every expense for a given
/// business date. This is not a UI-side filter - it is enforced here, server-side.</summary>
public sealed class ExpenseQueryService
{
    private readonly FalahDbContext _db;
    public ExpenseQueryService(FalahDbContext db) => _db = db;

    public async Task<IReadOnlyList<ExpenseDto>> GetVisibleExpensesAsync(
        string actorUserId, string? shiftId, string? businessDate, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        IQueryable<ExpenseEntity> query;
        if (actorUser.Role == "cashier")
        {
            if (string.IsNullOrEmpty(shiftId)) throw new DomainException("لا بد من تحديد الوردية.");
            var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct);
            if (shift is null || shift.Status != nameof(ShiftStatus.Open)) return Array.Empty<ExpenseDto>();
            query = _db.Expenses.AsNoTracking().Where(e => e.ShiftId == shiftId && e.PaidFromDrawer);
        }
        else
        {
            if (string.IsNullOrEmpty(businessDate) || !DateOnly.TryParse(businessDate, out var date))
                throw new DomainException("لا بد من تحديد تاريخ يوم العمل.");
            query = _db.Expenses.AsNoTracking().Where(e => e.BusinessDate == date);
        }

        var rows = await query.OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    private static ExpenseDto ToDto(ExpenseEntity e) => new(
        e.Id, e.Category, e.AmountPiastres, e.Description, e.ShiftId, e.PaidFromDrawer,
        e.PaymentAccountId, e.CreatedByUserId, e.BusinessDate.ToString("yyyy-MM-dd"), e.CreatedAtUtc);
}
