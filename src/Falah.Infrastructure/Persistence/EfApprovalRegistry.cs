namespace Falah.Infrastructure.Persistence;

using Falah.Domain.Authorization;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// EF Core-backed IApprovalRegistry - replaces InMemoryApprovalRegistry (kept in Falah.Domain for
/// unit tests only) so an issued approval survives a Server restart between the moment a manager
/// grants it and the moment the cashier's checkout/refund request actually consumes it. Unlike
/// EfFinancialJournal, issuing and consuming genuinely happen in two SEPARATE HTTP
/// requests/transactions (a manager approves, then - possibly much later - a cashier spends the
/// approval), so this always reads/writes the committed database, never relies on the current
/// request's own change tracker holding the row.
///
/// An approval stays usable for <c>Approvals:LifetimeMinutes</c> (default 15) after it was granted:
/// every screen spends it within seconds of the manager typing their PIN, and a till recovering an
/// unsent request after a restart is well inside that.
/// </summary>
public sealed class EfApprovalRegistry : IApprovalRegistry
{
    private readonly FalahDbContext _db;
    private readonly TimeProvider _time;

    public EfApprovalRegistry(FalahDbContext db, TimeProvider time, IConfiguration configuration)
    {
        _db = db;
        _time = time;
        ApprovalLifetime = TimeSpan.FromMinutes(
            int.TryParse(configuration["Approvals:LifetimeMinutes"], out var minutes) ? Math.Max(1, minutes) : 15);
    }

    public TimeSpan? ApprovalLifetime { get; }
    public DateTimeOffset UtcNow => _time.GetUtcNow();

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
        RequestedByUserId = approval.RequestedByUserId,
    });

    public ManagerApproval? Find(string approvalId)
    {
        var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId);
        return entity is null ? null
            : new ManagerApproval(entity.ApprovalId, entity.ApproverUserId, entity.ApproverName, entity.ApproverRole,
                entity.ApprovedAtUtc, entity.Action, entity.Reason, entity.RequestedByUserId);
    }

    public bool IsConsumed(string approvalId) =>
        (_db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId))?.Consumed ?? false;

    public void MarkConsumed(string approvalId)
    {
        var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
            ?? _db.ManagerApprovals.FirstOrDefault(a => a.ApprovalId == approvalId) // tracked - must be mutable
            ?? throw new InvalidOperationException("الاعتماد غير موجود لتحديده كمستهلك.");
        if (entity.Consumed)
            throw new Falah.Domain.Common.DomainException("اعتماد المدير مستخدم بالفعل.");
        entity.Consumed = true;
    }
}
