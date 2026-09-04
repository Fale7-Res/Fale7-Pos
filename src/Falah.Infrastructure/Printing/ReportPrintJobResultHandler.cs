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
    public ReportPrintJobResultHandler(FalahDbContext db) => _db = db;

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
            job.AttemptCount++;
            job.LastAttemptAtUtc = now;
            job.Status = request.Succeeded ? nameof(PrintJobStatus.Succeeded) : nameof(PrintJobStatus.Failed);
            job.LastError = request.Succeeded ? null : request.Error;

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
