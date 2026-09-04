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
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Period, @"^\d{4}-\d{2}$"))
            throw new DomainException("فترة Payroll غير صالحة.");

        var existing = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Period == request.Period && r.Status != nameof(PayrollRunStatus.Cancelled), ct);
        if (existing is not null) return ToDto(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_manage");

        var periodYear = int.Parse(request.Period[..4]);
        var periodMonth = int.Parse(request.Period[5..]);
        var employees = await _db.Employees.AsNoTracking().ToListAsync(ct);
        var periodEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
            .Where(e => e.CreatedAtUtc.Year == periodYear && e.CreatedAtUtc.Month == periodMonth)
            .ToListAsync(ct);

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
                emp.Id, emp.IsActive, emp.CompensationType == nameof(CompensationType.Monthly),
                emp.MonthlySalaryPiastres is null ? null : Money.FromPiastres(emp.MonthlySalaryPiastres.Value),
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
                    NetPayablePiastres = line.NetPayable.Piastres, Status = "unpaid",
                });
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (replay is null) throw;
            return ToDto(replay, wasReplayed: true);
        }
        return ToDto(runEntity, wasReplayed: false);
    }

    internal static PayrollRunDto ToDto(PayrollRunEntity r, bool wasReplayed) => new(
        r.Id, r.Period, r.Status, r.CreatedBy, r.CreatedAtUtc, r.ApprovedBy, r.PaidAtUtc, r.ReviewedAtUtc,
        r.ReviewedBy, r.TreasuryMovementId, r.PaymentAccountId, wasReplayed);
}
