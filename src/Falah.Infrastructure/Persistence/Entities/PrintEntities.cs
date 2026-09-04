namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>One print job = one physical ticket destined for one station printer. An order that
/// touches two kitchen stations gets TWO separate rows here, each independently retryable - a
/// printer failure at one station can never affect the other's job, and creating/retrying/reporting
/// on a job never touches the Order/Payment/Inventory it describes (see the critical invariant this
/// whole feature exists to preserve: successful order/payment != successful print).</summary>
public class PrintJobEntity
{
    public string Id { get; set; } = default!;
    /// <summary>The RequestId of the CreatePrintJobs call that made this job - lets a retry of that
    /// exact call find every job it already created (a batch may contain several jobs, one per
    /// route) without parsing the Id string.</summary>
    public string CreateRequestId { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public string Route { get; set; } = default!;
    public string Status { get; set; } = default!;
    /// <summary>How many attempts have been reported against this job (success or failure) - a
    /// retry/reprint of the SAME job increments this rather than creating a new row, so the full
    /// history stays attached to one ticket's own identity.</summary>
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
    /// <summary>True when this job was created by an explicit reprint request rather than the
    /// original post-checkout print request - purely informational, never affects routing/content.</summary>
    public bool IsReprint { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<PrintJobAttemptEntity> Attempts { get; set; } = new();
}

/// <summary>One row per reported attempt (success or failure) against a PrintJob - the full audit
/// trail "attempt time, success/failure, ... last error" requires, not just the job's own latest
/// snapshot fields.</summary>
public class PrintJobAttemptEntity
{
    public int Id { get; set; }
    /// <summary>Same idempotency contract as every other command in this codebase - a network
    /// retry of "report this attempt's result" must never inflate AttemptCount or fabricate a
    /// second attempt that never physically happened.</summary>
    public string IdempotencyKey { get; set; } = default!;
    public string PrintJobId { get; set; } = default!;
    public DateTimeOffset AttemptedAtUtc { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public string ReportedBy { get; set; } = default!;
}
