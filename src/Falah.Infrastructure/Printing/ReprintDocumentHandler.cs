namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Audit;
using Falah.Domain.Common;
using Falah.Domain.Printing;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Prints a shift slip or an expense voucher again.
///
/// It creates a print job and nothing else: no shift is reopened, no expense is re-posted, no money
/// moves. The document must already exist - this reproduces it. Like an order reprint, the paper is
/// marked "إعادة طباعة" and the act is written to the audit trail, because reproducing a cash
/// document later is exactly the kind of thing a reviewer needs to see.
///
/// Replaying the same RequestId returns the job already created for it rather than queuing a second
/// one; a genuinely new press mints a new id and is a new, separately audited reprint.
/// </summary>
public sealed class ReprintDocumentHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;
    public ReprintDocumentHandler(FalahDbContext db, IAuditLog auditLog) { _db = db; _auditLog = auditLog; }

    public async Task<CreatePrintJobsResult> HandleAsync(ReprintDocumentRequest request, CancellationToken ct = default)
    {
        var existing = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
        if (existing.Count > 0)
            return new CreatePrintJobsResult(existing.Select(CreatePrintJobsHandler.ToDto).ToList(), WasReplayed: true);

        if (!Enum.TryParse<PrintDocumentKind>(request.DocumentKind, ignoreCase: true, out var kind)
            || kind == PrintDocumentKind.Order)
            throw new DomainException("نوع المستند المطلوب إعادة طباعته غير صالح.");

        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        var (label, exists) = kind switch
        {
            PrintDocumentKind.ShiftOpen => (PrintRules.ShiftOpenLabel,
                await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId, ct)),
            PrintDocumentKind.ShiftClose => (PrintRules.ShiftCloseLabel,
                await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId && s.ClosedAtUtc != null, ct)),
            _ => (PrintRules.ExpenseLabel,
                await _db.Expenses.AsNoTracking().AnyAsync(e => e.Id == request.DocumentId, ct)),
        };
        if (!exists) throw new InvalidOperationException("المستند المطلوب إعادة طباعته غير موجود.");

        var now = DateTimeOffset.UtcNow;
        var job = DocumentPrintJobs.Build(kind, request.DocumentId, label, actor.Name, now, reprintRequestId: request.RequestId);

        try
        {
            _db.PrintJobs.Add(job);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                $"إعادة طباعة {label}", kind == PrintDocumentKind.Expense ? "expense" : "shift",
                $"إعادة طباعة {label} ({request.DocumentId})"
                    + (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - السبب: {request.Reason.Trim()}"),
                "normal", now, ReferenceId: request.DocumentId, Reason: request.Reason));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
            if (replay.Count > 0)
                return new CreatePrintJobsResult(replay.Select(CreatePrintJobsHandler.ToDto).ToList(), WasReplayed: true);
            throw new PrintingConflictException("تعذر إنشاء أمر إعادة الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }

        return new CreatePrintJobsResult(new[] { CreatePrintJobsHandler.ToDto(job) }, WasReplayed: false);
    }
}
