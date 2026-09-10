namespace Falah.Infrastructure.Purchasing;

using Falah.Application.Purchasing;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Purchasing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Sets or clears the agreed due date on an invoice already received.
///
/// Terms are frequently agreed after the goods arrive, so this exists as its own command rather
/// than forcing a due date to be known at receipt time - a system that demanded one up front would
/// get an invented one. It moves no money and posts nothing to the ledger, so it carries none of
/// receiving's treasury machinery; it is audited because it changes what the payables report says
/// is overdue.
/// </summary>
public sealed class SetPurchaseDueDateHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SetPurchaseDueDateHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PurchaseDto> HandleAsync(SetPurchaseDueDateRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "purchasing_manage");

        var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
            ?? throw new DomainException("الفاتورة غير موجودة.");

        var receivedOn = BusinessClock.BusinessDate(purchase.CreatedAtUtc);
        PayablesAging.AssertValidDueDate(request.DueDate, receivedOn);

        var previous = purchase.DueDate;
        // Setting the date it already has changes nothing and is not worth an audit entry.
        if (previous == request.DueDate) return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: true);

        purchase.DueDate = request.DueDate;
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            request.DueDate is null ? "إلغاء تاريخ استحقاق فاتورة مورد" : "تحديد تاريخ استحقاق فاتورة مورد",
            "purchasing",
            $"الفاتورة: {purchase.InvoiceNumber ?? purchase.Id}",
            "normal", DateTimeOffset.UtcNow, ReferenceId: purchase.Id,
            PreviousValue: previous?.ToString("yyyy-MM-dd"),
            NewValue: request.DueDate?.ToString("yyyy-MM-dd")));

        await _db.SaveChangesAsync(ct);
        return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false);
    }
}
