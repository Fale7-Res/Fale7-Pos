namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Payroll;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from createPayroll in lib/enterprise-store.tsx. One run per period; a second
/// create for a period that already has a non-cancelled run is a safe no-op (returns the existing
/// run), matching legacy's own `if (existing) return`. Lines are computed once, from that period's
/// employee ledger entries (daily_wage/salary/overtime/extra_shift/bonus/other_earning/deduction/
/// adjustment types, exactly as PayrollRules.CalculatePayrollLines expects), and frozen from then on.</summary>
public sealed class CreatePayrollHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreatePayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<PayrollRunDto> HandleAsync(CreatePayrollRequest request, CancellationToken ct = default)
    {
        if (!DateOnly.TryParseExact(request.Period + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var periodStart))
            throw new DomainException("فترة Payroll غير صالحة.");

        var existing = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Period == request.Period && r.Status != nameof(PayrollRunStatus.Cancelled), ct);
        if (existing is not null) return ToDto(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_manage");

        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var employees = await _db.Employees.AsNoTracking().ToListAsync(ct);
        var periodEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
            .ForDays(periodStart, periodEnd)
            .ToListAsync(ct);
        var wageHistory = await EmployeeWages.LoadAllAsync(_db, ct);

        // Advance-deduction-during-payroll (legacy's optional per-employee advanceDeductions map)
        // is deliberately not wired up yet: no evidenced caller in the reference app ever supplies a
        // non-empty map (the UI always creates a run with none), so every employee's own
        // RequestedAdvanceDeduction is Money.Zero here - the same default legacy itself uses when
        // the map is omitted, not a lesser port.
        var lineInputs = employees.Select(emp =>
        {
            var empEntries = periodEntries.Where(e => e.EmployeeId == emp.Id)
                .Select(e => (e.Type, Credit: Money.FromPiastres(e.CreditPiastres), Debit: Money.FromPiastres(e.DebitPiastres)))
                .ToList();
            return new PayrollRules.PayrollLineInput(
                emp.Id, emp.IsActive, Enum.TryParse<CompensationType>(emp.CompensationType, true, out var savedType) && savedType == CompensationType.Monthly,
                // PAYR-0007 (owner decision 2026-11-22): the period's share of the salary - the days from
                // the hire date to the period's end - matching what SalaryAccrual posts day by day.
                // PAYR-0006: each day at the salary in effect that day (EmployeeWages).
                emp.MonthlySalaryPiastres is null ? null
                    : PayrollRules.MonthlyBaseForPeriod(day => Money.FromPiastres(EmployeeWages.At(wageHistory[emp.Id], emp, day).MonthlySalary ?? 0),
                        periodStart, periodEnd, emp.HireDate),
                empEntries, Money.Zero, Money.Zero);
        }).ToList();

        var computedLines = PayrollRules.CalculatePayrollLines(lineInputs);

        var runId = $"payroll-{request.RequestId}";
        var runEntity = new PayrollRunEntity
        {
            Id = runId, Period = request.Period, Status = nameof(PayrollRunStatus.Draft),
            CreatedBy = actorUser.Name, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.PayrollRuns.Add(runEntity);
            foreach (var line in computedLines)
            {
                _db.PayrollLines.Add(new PayrollLineEntity
                {
                    Id = $"pl-{Guid.NewGuid():N}", PayrollRunId = runId, EmployeeId = line.EmployeeId,
                    BasePiastres = line.Base.Piastres, OvertimePiastres = line.Overtime.Piastres,
                    ExtraShiftsPiastres = line.ExtraShifts.Piastres, BonusesPiastres = line.Bonuses.Piastres,
                    DeductionsPiastres = line.Deductions.Piastres, AdvanceDeductionsPiastres = line.AdvanceDeductions.Piastres,
                    NetPayablePiastres = line.NetPayable.Piastres, PaidOutsidePayrollPiastres = line.PaidOutsidePayroll.Piastres, Status = "unpaid",
                });
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Either this exact request already ran (same RequestId, same run id), or another
            // request created this period's run in the moment between the read above and this
            // insert - the unique index on Period is what makes the second case an exception here
            // instead of a second payroll run for the same month. Both mean the same thing to the
            // caller: the run exists, and this is it.
            _db.ChangeTracker.Clear();
            var replay = await _db.PayrollRuns.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == runId ||
                    (r.Period == request.Period && r.Status != nameof(PayrollRunStatus.Cancelled)), ct);
            if (replay is null) throw;
            return ToDto(replay, wasReplayed: true);
        }
        return ToDto(runEntity, wasReplayed: false);
    }

    internal static PayrollRunDto ToDto(PayrollRunEntity r, bool wasReplayed) => new(
        r.Id, r.Period, r.Status, r.CreatedBy, r.CreatedAtUtc, r.ApprovedBy, r.PaidAtUtc, r.ReviewedAtUtc,
        r.ReviewedBy, r.TreasuryMovementId, r.PaymentAccountId, wasReplayed);
}
