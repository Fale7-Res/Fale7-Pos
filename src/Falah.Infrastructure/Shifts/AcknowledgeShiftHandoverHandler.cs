namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// P4: the cash handed over at a shift close is received only when the receiver says so. The close itself still moves
/// the counted cash drawer -> main (the drawer must be empty for the next shift, and every existing recovery and retry
/// of the close is unchanged), but the handover stays "pending" until:
///
/// - the designated receiver's own session confirms it, or
/// - the receiver types a PIN at the till: an approval issued for shifts_manage, spent here, whose approver must be the
///   designated receiver (or, with nobody designated, becomes the receiver);
///
/// never by the cashier who handed it over. The receiver states what was counted. The same amount is "acknowledged";
/// a different one is "disputed" with a note, flagged critical, and moves no money - who answers for the difference
/// is the owner's decision (HANDOVER-DIFF), so nothing is posted on a guess.
/// </summary>
public sealed class AcknowledgeShiftHandoverHandler
{
    public const string Pending = "pending";
    public const string Acknowledged = "acknowledged";
    public const string Disputed = "disputed";

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public AcknowledgeShiftHandoverHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<ShiftHandoverReceiptDto> HandleAsync(AcknowledgeShiftHandoverRequest request, CancellationToken ct = default)
    {
        var replay = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.HandoverAcknowledgeRequestId == request.RequestId, ct);
        if (replay is not null) return await ToDtoAsync(replay, wasReplayed: true, ct);

        if (request.ReceivedPiastres < 0) throw new DomainException("المبلغ المستلم لا يمكن أن يكون سالبًا.");
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var shift = await _db.Shifts
            .FromSqlRaw("SELECT * FROM Shifts WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.ShiftId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        if (shift.HandoverAcknowledgeRequestId == request.RequestId)
        {
            await transaction.RollbackAsync(ct);
            return await ToDtoAsync(shift, wasReplayed: true, ct);
        }
        if (shift.Status != "Closed" || (shift.HandedOverPiastres ?? 0) <= 0)
            throw new DomainException("لا يوجد تسليم نقدية لهذه الوردية.");
        if (shift.HandoverReceiptStatus is Acknowledged or Disputed)
            throw new DomainException("تم تسجيل استلام هذا التسليم بالفعل.");

        // Who is receiving. A PIN typed at the till proves the receiver; otherwise the session must be the receiver's.
        UserEntity receiver;
        if (!string.IsNullOrEmpty(request.ReceiverApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ReceiverApproverUserId, ct)
                ?? throw new DomainException("المستلم غير موجود.");
            var issued = AuthorizationPolicy.ValidateApproval(_approvalRegistry, "shifts_manage",
                new ManagerApproval(request.ReceiverApprovalId, approverUser.Id, approverUser.Name, approverUser.Role,
                    DateTimeOffset.UtcNow, "shifts_manage", request.Note), actorUser.Id);
            receiver = issued.ApproverUserId == approverUser.Id ? approverUser
                : throw new DomainException(AuthorizationPolicy.ApprovalInvalidMessage);
        }
        else
        {
            receiver = actorUser;
        }

        if (shift.HandoverReceiverUserId is { } designated && receiver.Id != designated)
            throw new DomainException($"الاستلام يؤكده المستلم المحدد عند الإغلاق ({shift.HandoverReceiverName}) فقط، من جلسته أو برمزه.");
        if (receiver.Id == shift.ClosedByUserId && shift.HandoverReceiverUserId != receiver.Id)
            throw new DomainException("من سلّم النقدية لا يؤكد استلامها بنفسه.");
        if (!receiver.Active || !AuthorizationPolicy.HasPermission(OrderMapper.ToActor(receiver), "shifts_manage"))
            throw new DomainException("مستلم نقدية الإغلاق يجب أن يكون مديرًا مخولًا بإدارة الورديات.");

        var handedOver = shift.HandedOverPiastres!.Value;
        var difference = request.ReceivedPiastres - handedOver;
        if (difference != 0 && string.IsNullOrWhiteSpace(request.Note))
            throw new DomainException("المبلغ المستلم يختلف عن المسلَّم؛ اكتب ملاحظة بما حدث.");

        var now = DateTimeOffset.UtcNow;
        shift.HandoverReceiptStatus = difference == 0 ? Acknowledged : Disputed;
        shift.HandoverReceivedPiastres = request.ReceivedPiastres;
        shift.HandoverAcknowledgedAtUtc = now;
        shift.HandoverAcknowledgedByUserId = receiver.Id;
        shift.HandoverAcknowledgedByName = receiver.Name;
        shift.HandoverAcknowledgeRequestId = request.RequestId;
        shift.HandoverReceiptNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        shift.HandoverReceiverUserId ??= receiver.Id;
        shift.HandoverReceiverName ??= receiver.Name;
        shift.Version++;

        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", receiver.Id, receiver.Name, receiver.Role,
            difference == 0 ? "تأكيد استلام نقدية الإغلاق" : "اختلاف في استلام نقدية الإغلاق", "shift",
            $"{shift.ShiftName}: المسلَّم {handedOver} قرش والمستلم {request.ReceivedPiastres} قرش" + (difference == 0 ? "" : $" - فرق {difference} - {request.Note!.Trim()}"),
            difference == 0 ? "normal" : "critical", now, ShiftId: shift.Id, Reason: shift.HandoverReceiptNote,
            AuthorizedBy: receiver.Name, ReferenceId: shift.Id,
            PreviousValue: handedOver.ToString(), NewValue: request.ReceivedPiastres.ToString()));

        try
        {
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            var after = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.HandoverAcknowledgeRequestId == request.RequestId, ct);
            if (after is not null) return await ToDtoAsync(after, wasReplayed: true, ct);
            throw new DomainException("تعذر تسجيل الاستلام بسبب تعارض - أعد المحاولة.");
        }
        return await ToDtoAsync(shift, wasReplayed: false, ct);
    }

    /// <summary>Every closed shift whose handover is still pending or disputed, oldest first - now, not a period.</summary>
    public async Task<IReadOnlyList<ShiftHandoverReceiptDto>> ListUnconfirmedAsync(CancellationToken ct = default)
    {
        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.Status == "Closed" && (s.HandoverReceiptStatus == Pending || s.HandoverReceiptStatus == Disputed))
            .OrderBy(s => s.ClosedAtUtc).Take(200).ToListAsync(ct);
        var result = new List<ShiftHandoverReceiptDto>();
        foreach (var s in shifts) result.Add(await ToDtoAsync(s, wasReplayed: false, ct));
        return result;
    }

    /// <summary>The receipt state the close records: nothing handed over - none; handed to the closer himself - he
    /// is the receiver and his own session closed it; anyone else - pending until confirmed.</summary>
    public static void RecordAtClose(ShiftEntity shift, string closerId, string closerName, DateTimeOffset now)
    {
        if ((shift.HandedOverPiastres ?? 0) <= 0) { shift.HandoverReceiptStatus = null; return; }
        if (shift.HandoverReceiverUserId == closerId)
        {
            shift.HandoverReceiptStatus = Acknowledged;
            shift.HandoverReceivedPiastres = shift.HandedOverPiastres;
            shift.HandoverAcknowledgedAtUtc = now;
            shift.HandoverAcknowledgedByUserId = closerId;
            shift.HandoverAcknowledgedByName = closerName;
            return;
        }
        shift.HandoverReceiptStatus = Pending;
    }

    public static string StatusOf(ShiftEntity s) =>
        (s.HandedOverPiastres ?? 0) <= 0 ? "none" : s.HandoverReceiptStatus ?? "not_recorded";

    private async Task<ShiftHandoverReceiptDto> ToDtoAsync(ShiftEntity s, bool wasReplayed, CancellationToken ct)
    {
        var closedBy = s.ClosedByUserId is null ? null
            : await _db.Users.AsNoTracking().Where(u => u.Id == s.ClosedByUserId).Select(u => u.Name).FirstOrDefaultAsync(ct);
        return new ShiftHandoverReceiptDto(
            s.Id, s.ShiftName, s.BusinessDate, s.ClosedAtUtc, closedBy, s.HandedOverPiastres ?? 0,
            s.HandoverReceiverUserId, s.HandoverReceiverName, StatusOf(s), s.HandoverReceivedPiastres,
            s.HandoverReceivedPiastres - s.HandedOverPiastres, s.HandoverAcknowledgedByName, s.HandoverAcknowledgedAtUtc,
            s.HandoverReceiptNote, wasReplayed);
    }
}
