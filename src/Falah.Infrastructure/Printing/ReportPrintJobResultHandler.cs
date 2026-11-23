namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Printing;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A station's print agent calls this once per physical attempt it makes against a job - success
/// or failure. Never touches the order/payment/inventory the job describes; only ever mutates the
/// job's own status/attempt-count/last-error and appends one row to its attempt history. A job that
/// failed stays reportable against again (a retry is simply another call here against the SAME
/// PrintJobId, with a NEW RequestId) - there is no separate "retry" command, because reporting
/// another attempt already IS the retry.
/// </summary>
public sealed class ReportPrintJobResultHandler
{
    private readonly FalahDbContext _db;
    private readonly Microsoft.Extensions.Configuration.IConfiguration? _configuration;
    public ReportPrintJobResultHandler(FalahDbContext db, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    { _db = db; _configuration = configuration; }

    /// <summary>Who holds a job that failed and waits for its automatic retry (PRT-0013). No agent id
    /// contains "|", so no agent's own-claim pickup ever matches it; the pause is ClaimedUntilUtc.</summary>
    public const string AutomaticRetryHolder = "automatic-retry|";

    private int Setting(string key, int fallback) => int.TryParse(_configuration?[key], out var value) ? value : fallback;

    public async Task<PrintJobDto> HandleAsync(ReportPrintJobResultRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"printattempt-{request.RequestId}";
        var existingAttempt = await _db.PrintJobAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.IdempotencyKey == idempotencyKey, ct);
        if (existingAttempt is not null)
        {
            var replayedJob = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.PrintJobId, ct)
                ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
            return CreatePrintJobsHandler.ToDto(replayedJob);
        }

        var reportedByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ReportedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var job = await _db.PrintJobs
                .FromSqlRaw("SELECT * FROM PrintJobs WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.PrintJobId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");

            var now = DateTimeOffset.UtcNow;
            // A stale outcome is still evidence, but never overwrites the current holder/result.
            var currentHolder = request.ClaimId is not null && job.ClaimedBy == request.ClaimId
                && job.Status is nameof(PrintJobStatus.Pending) or nameof(PrintJobStatus.Sending);
            var legacyUnclaimed = request.ClaimId is null && job.ClaimedBy is null
                && job.Status is nameof(PrintJobStatus.Pending) or nameof(PrintJobStatus.Failed);
            if (currentHolder || legacyUnclaimed)
            {
                job.AttemptCount++;
                job.LastAttemptAtUtc = now;
                job.Status = request.Succeeded ? nameof(PrintJobStatus.Succeeded) : nameof(PrintJobStatus.Failed);
                job.LastError = request.Succeeded ? null : request.Error;
                job.ClaimedBy = null;
                job.ClaimedUntilUtc = null;

                // PRT-0013 (owner decision 2026-11-22): a failed ticket is queued again by itself, after a
                // pause, up to PrintJobs:AutomaticRetries more times (default 2: three attempts in all).
                // The paper it then produces says "إعادة طباعة" (PrintJobQueryService.MarkIfReprint), since
                // the failed one may still come out of the Windows queue. Only the last failure stays Failed.
                var retries = Math.Max(0, Setting("PrintJobs:AutomaticRetries", 2));
                if (!request.Succeeded && job.AttemptCount <= retries)
                {
                    var pause = TimeSpan.FromSeconds(Math.Max(0, Setting("PrintJobs:RetryDelaySeconds", 30)));
                    job.Status = nameof(PrintJobStatus.Pending);
                    job.ClaimedBy = AutomaticRetryHolder;
                    job.ClaimedUntilUtc = now + pause;
                }
            }

            _db.PrintJobAttempts.Add(new PrintJobAttemptEntity
            {
                IdempotencyKey = idempotencyKey, PrintJobId = job.Id, AttemptedAtUtc = now,
                Succeeded = request.Succeeded, Error = request.Succeeded ? null : request.Error, ReportedBy = reportedByUser.Name,
            });

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return CreatePrintJobsHandler.ToDto(job);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.PrintJobAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null)
            {
                var replayedJob = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.PrintJobId, ct)
                    ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
                return CreatePrintJobsHandler.ToDto(replayedJob);
            }
            throw new PrintingConflictException("تعذر تسجيل نتيجة الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}
