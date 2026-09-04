namespace Falah.Infrastructure.Persistence.Entities;

public enum BackupRecordKind { Backup, Restore }
public enum BackupRecordStatus { Requested, Succeeded, Failed, Blocked }

/// <summary>One row per backup/restore attempt - including blocked ones (a restore refused by a
/// confirmation-phrase mismatch or an active-operations check still gets a row, so the history is a
/// complete audit trail of every attempt, not just successful ones).</summary>
public class BackupRecordEntity
{
    public string Id { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string Status { get; set; } = default!;
    public long? FileSizeBytes { get; set; }
    public string? VerificationStatus { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string RequestedBy { get; set; } = default!;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
}
