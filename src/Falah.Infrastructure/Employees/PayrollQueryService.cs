namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class PayrollQueryService
{
    private readonly FalahDbContext _db;
    public PayrollQueryService(FalahDbContext db) => _db = db;

    public Task<List<PayrollRunDto>> ListRunsAsync(CancellationToken ct = default) =>
        _db.PayrollRuns.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new PayrollRunDto(r.Id, r.Period, r.Status, r.CreatedBy, r.CreatedAtUtc, r.ApprovedBy,
                r.PaidAtUtc, r.ReviewedAtUtc, r.ReviewedBy, r.TreasuryMovementId, r.PaymentAccountId, false))
            .ToListAsync(ct);

    public async Task<PayrollRunDto> GetRunAsync(string runId, CancellationToken ct = default)
    {
        var r = await _db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId, ct)
            ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
        return CreatePayrollHandler.ToDto(r, wasReplayed: false);
    }

    public Task<List<PayrollLineDto>> ListLinesAsync(string runId, CancellationToken ct = default) =>
        _db.PayrollLines.AsNoTracking().Where(l => l.PayrollRunId == runId)
            .Select(l => new PayrollLineDto(l.Id, l.PayrollRunId, l.EmployeeId, l.BasePiastres, l.OvertimePiastres,
                l.ExtraShiftsPiastres, l.BonusesPiastres, l.DeductionsPiastres, l.AdvanceDeductionsPiastres,
                l.NetPayablePiastres, l.Status, l.PaidAtUtc))
            .ToListAsync(ct);
}
