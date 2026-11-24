namespace Falah.Infrastructure.Customers;

using Falah.Application.Customers;
using Falah.Domain.Audit;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>CUS-0002 (owner decision 2026-11-22): the owner "deletes" a customer. Staff screens no
/// longer find them; the record, its addresses and its order history stay stored and the owner still
/// sees them. Nothing is removed. A new order from the same phone (upsert) brings the customer back.
/// Repeating the delete is harmless.</summary>
public sealed class HideCustomerHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;

    public HideCustomerHandler(FalahDbContext db, IAuditLog auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(HideCustomerRequest request, CancellationToken ct = default)
    {
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new DomainException("المستخدم غير موجود.");
        var permissions = (actor.Permissions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!permissions.Contains("all")) throw new DomainException("مسح العميل متاح للمالك فقط.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new DomainException("العميل غير موجود.");
        if (customer.HiddenAtUtc is not null) return;

        var now = DateTimeOffset.UtcNow;
        customer.HiddenAtUtc = now;
        customer.HiddenBy = actor.Id;
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
            $"مسح العميل {customer.Name}", "customer",
            $"العميل {customer.Phone} اتشال من شاشات الموظفين وبياناته محفوظة عند المالك",
            "info", now, ReferenceId: customer.Id));
        await _db.SaveChangesAsync(ct);
    }
}
