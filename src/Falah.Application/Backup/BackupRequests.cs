namespace Falah.Application.Backup;

/// <summary>Ported for the current SQL Server architecture (native BACKUP DATABASE), not a copy of
/// the reference app's browser-localStorage JSON export - see Falah.Domain.Backup.BackupRules' own
/// doc comment. DestinationDirectory is the configurable destination the requirements ask for; a
/// null value falls back to the server's own configured default directory.</summary>
public sealed record CreateBackupRequest(string ActorUserId, string? DestinationDirectory = null);

public sealed record BackupRecordDto(
    string Id, string Kind, string FilePath, string Status, long? FileSizeBytes, string? VerificationStatus,
    DateTimeOffset? VerifiedAtUtc, string RequestedBy, DateTimeOffset RequestedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? ErrorMessage, string? Reason, string? ApprovedBy);

/// <summary>Runs SQL Server's own `RESTORE VERIFYONLY` against the backup file - checks the backup
/// set is structurally intact and readable WITHOUT actually restoring anything, so this is always
/// safe to run against a live system.</summary>
public sealed record VerifyBackupRequest(string BackupRecordId, string ActorUserId);

/// <summary>Ported from the reference app's own restore gate (manager-PIN-protected, explicit
/// confirmation, sensitive-data notice) but hardened further for a SQL Server RESTORE DATABASE's
/// much larger blast radius - see BackupRules' own doc comment for exactly what's new here and why.
/// Authorization itself is the same permission-or-approval shape every other gated command in this
/// codebase uses (`backup_manage`); the NEW hardening specific to this command is the mandatory
/// reason, the mandatory literal confirmation phrase, and the mandatory zero-active-operations
/// check - none of which any other command in this system requires, because none of them can erase
/// committed history the way a database restore can.</summary>
public sealed record RestoreBackupRequest(
    string BackupRecordId, string Reason, string ConfirmationPhrase, string ActorUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);
