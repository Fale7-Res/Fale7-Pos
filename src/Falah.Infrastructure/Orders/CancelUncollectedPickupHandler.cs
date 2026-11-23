namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// UI-0025 (owner decision 2026-11-22): the customer never came for a pickup registered unpaid. Until it
/// is collected or cancelled it blocks the business-day close; a manager cancels it. Nothing was paid, so
/// no money moves and nothing is refunded - it was never in any sales figure. The food is accounted for
/// the way the operator says it went: "prepared" = wasted, "not_prepared" = back in stock.
///
/// Who (owner, 2026-11-23): always a manager. A manager logged in at the till cancels it; anyone else
/// needs a manager's PIN at the till - an approval issued for "pickup_cancel" - whatever permissions they
/// hold. A permission alone never bypasses it. Replays by RequestId.
/// </summary>
public sealed class CancelUncollectedPickupHandler
{
    /// <summary>The approval action. Owners and managers issue it (CanIssueApproval); a supervisor only with "all".</summary>
    public const string ApprovalAction = "pickup_cancel";

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CancelUncollectedPickupHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<string> HandleAsync(CancelUncollectedPickupRequest request, CancellationToken ct = default)
    {
        if (await ReplayAsync(request.RequestId, ct) is { } replay) return replay;

        if (string.IsNullOrWhiteSpace(request.RequestId)) throw new DomainException("رقم العملية مطلوب.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإلغاء مطلوب.");
        var disposition = Falah.Domain.Inventory.InventoryRules.ParseStockDisposition(request.StockDisposition);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, ApprovalAction, request.Reason);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await FinancialAdmission.AcquireAsync(_db, ct);
        if (await ReplayAsync(request.RequestId, ct) is { } admissionReplay) return admissionReplay;

        var order = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException("الطلب غير موجود.");
        var cancelledWire = Falah.Domain.Orders.OrderStatus.Cancelled.ToWireString();
        if (order.Status == cancelledWire) throw new DomainException("الطلب ملغى بالفعل.");
        if (!order.AwaitingCollection)
            throw new DomainException("الإلغاء ده لطلب استلام لسه ما اتدفعش بس. الطلب المدفوع يترد بالاسترجاع.");
        var openDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        if (order.BusinessDayId is not null && order.BusinessDayId != openDayId)
            throw new DomainException("الطلب من يوم عمل مقفول؛ لا يمكن إلغاؤه بعد قفل اليوم.");

        // Checked last, so a refused cancellation never spends the manager's approval.
        var actor = OrderMapper.ToActor(actorUser);
        if (!AuthorizationPolicy.HasPermission(actor, "pos")) throw new DomainException("ليس لديك صلاحية على الطلبات.");
        string? approvedBy;
        if (AuthorizationPolicy.CanIssueApproval(actor, ApprovalAction)) approvedBy = actorUser.Name;
        else if (approval is null) throw new DomainException("إلغاء طلب الاستلام محتاج رمز المدير.");
        else approvedBy = AuthorizationPolicy.ValidateApproval(_approvalRegistry, ApprovalAction, approval, actorUser.Id).ApproverName;

        var now = DateTimeOffset.UtcNow;
        var reason = request.Reason.Trim();
        order.Status = cancelledWire;
        order.AwaitingCollection = false;
        order.CancelledAtUtc = now;
        order.CancelReason = reason;
        order.CancelledBy = actorUser.Name;
        order.CollectionRequestId = $"cancel-{request.RequestId}";
        order.Version++;

        await OrderStockDisposition.ApplyAsync(_db, order.Id, disposition, reason, actorUser.Name, now, ct);
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"إلغاء طلب استلام ما اتدفعش {order.OrderNumber}", "order",
            $"العميل ما جاش - {order.TotalPiastres} قرش ما اتحصلتش - السبب: {reason}",
            "warning", now, AuthorizedBy: approvedBy, ShiftId: order.ShiftId, Reason: reason, ReferenceId: order.Id));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return order.Status;
    }

    // The cancel's request id is kept in CollectionRequestId ("cancel-" + id): an order is either paid on
    // collection or cancelled, never both, so one column answers both replays.
    private Task<string?> ReplayAsync(string requestId, CancellationToken ct) =>
        _db.Orders.AsNoTracking().Where(o => o.CollectionRequestId == $"cancel-{requestId}").Select(o => o.Status).FirstOrDefaultAsync(ct);
}
