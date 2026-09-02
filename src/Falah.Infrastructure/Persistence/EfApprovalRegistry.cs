namespace Falah.Infrastructure.Persistence;

using Falah.Domain.Authorization;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core-backed IApprovalRegistry - replaces InMemoryApprovalRegistry (kept in Falah.Domain for
/// unit tests only) so an issued approval survives a Server restart between the moment a manager
/// grants it and the moment the cashier's checkout/refund request actually consumes it. Unlike
/// EfFinancialJournal, issuing and consuming genuinely happen in two SEPARATE HTTP
/// requests/transactions (a manager approves, then - possibly much later - a cashier spends the
/// approval), so this always reads/writes the committed database, never relies on the current
/// request's own change tracker holding the row.
/// </summary>
public sealed class EfApprovalRegistry : IApprovalRegistry
{
    private readonly FalahDbContext _db;
    public EfApprovalRegistry(FalahDbContext db) => _db = db;

    public void Register(ManagerApproval approval) => _db.ManagerApprovals.Add(new ManagerApprovalEntity
    {
        ApprovalId = approval.ApprovalId,
        ApproverUserId = approval.ApproverUserId,
        ApproverName = approval.ApproverName,
        ApproverRole = approval.ApproverRole,
        Action = approval.Action,
        Reason = approval.Reason,
        ApprovedAtUtc = approval.ApprovedAtUtc,
        Consumed = false,
    });

    public ManagerApproval? Find(string approvalId)
    {
        var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId);
        return entity is null ? null
            : new ManagerApproval(entity.ApprovalId, entity.ApproverUserId, entity.ApproverName, entity.ApproverRole, entity.ApprovedAtUtc, entity.Action, entity.Reason);
    }

    public bool IsConsumed(string approvalId) =>
        (_db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId))?.Consumed ?? false;

    public void MarkConsumed(string approvalId)
    {
        var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.FirstOrDefault(a => a.ApprovalId == approvalId) // tracked - must be mutable
            ?? throw new InvalidOperationException("الاعتماد غير موجود لتحديده كمستهلك.");
        entity.Consumed = true;
    }
}
