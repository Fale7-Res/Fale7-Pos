namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from recordManualLedgerEntry in lib/store.tsx - see
/// RecordManualLedgerEntryRequest's own doc comment for why this is gated on `employees_ledger`
/// here even though the reference app's own function has no permission check at all (a real,
/// evidence-flagged gap, fixed the same way this codebase has fixed every other un-gated financial
/// write found this session).</summary>
public sealed class RecordManualLedgerEntryHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public RecordManualLedgerEntryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeLedgerEntryDto> HandleAsync(RecordManualLedgerEntryRequest request, CancellationToken ct = default)
    {
        // Parsed leniently, stored canonically. The type used to be validated case-insensitively
        // and then written down exactly as it arrived, so a bonus recorded as "bonus" was accepted,
        // appeared on the employee's statement, and was then skipped by the payroll calculation -
        // which matches "Bonus". The employee simply did not get paid it, and nothing anywhere said
        // so. Whatever casing the caller uses, one spelling reaches the database.
        if (!Enum.TryParse<LedgerEntryType>(request.Type, ignoreCase: true, out var entryType))
            throw new DomainException("نوع حركة كشف الحساب غير صالح.");
        var canonicalType = entryType.ToString();
        var amount = Money.FromPiastres(request.AmountPiastres);
        EmployeeRules.AssertLedgerAmount(amount);
        if (string.IsNullOrWhiteSpace(request.Description)) throw new DomainException("وصف الحركة مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", request.Description);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_ledger", approval, requireApproval: true);

        var currentBalance = Money.FromPiastres(await _db.EmployeeLedgerEntries.AsNoTracking()
            .Where(e => e.EmployeeId == request.EmployeeId)
            .Select(e => e.CreditPiastres - e.DebitPiastres).SumAsync(ct));
        var debit = request.IsCredit ? Money.Zero : amount;
        var credit = request.IsCredit ? amount : Money.Zero;
        var newBalance = currentBalance + credit - debit;

        var entity = new EmployeeLedgerEntryEntity
        {
            Id = $"led-{Guid.NewGuid():N}", EmployeeId = request.EmployeeId, Type = canonicalType,
            Description = request.Description.Trim(), DebitPiastres = debit.Piastres, CreditPiastres = credit.Piastres,
            BalanceAfterPiastres = newBalance.Piastres, RecordedBy = actorUser.Name,
            AuthorizedBy = authorization.Approver?.ApproverName, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        _db.EmployeeLedgerEntries.Add(entity);
        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToLedgerDto(entity);
    }
}
