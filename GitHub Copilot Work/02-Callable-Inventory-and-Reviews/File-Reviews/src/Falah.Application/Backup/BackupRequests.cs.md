# src/Falah.Application/Backup/BackupRequests.cs

Backup API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `b54085cdb58c4f33e9cfcaae987c1351c223d0eadf5892796aaf0f64479a8d4e`. Working SHA-256 `b54085cdb58c4f33e9cfcaae987c1351c223d0eadf5892796aaf0f64479a8d4e`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-29.

Symbols indexed (index is not a claim every method was reviewed): CreateBackupRequest@7, BackupRecordDto@9, VerifyBackupRequest@17, RestoreBackupRequest@27.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 7: `public sealed record CreateBackupRequest(string ActorUserId, string? DestinationDirectory = null);`
- Line 9: `public sealed record BackupRecordDto(`
- Line 17: `public sealed record VerifyBackupRequest(string BackupRecordId, string ActorUserId);`
- Line 27: `public sealed record RestoreBackupRequest(`

Backup request accepts optional server-side destination; verify and restore requests use record IDs. Restore is explicitly destructive and must retain strict handler authorization and operation gates. DTO comments do not make VERIFYONLY a full restore proof.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
