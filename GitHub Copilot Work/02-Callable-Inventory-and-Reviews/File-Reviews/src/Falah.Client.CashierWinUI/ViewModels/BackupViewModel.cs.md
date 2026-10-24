# src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `a901af5185d49178a93586d2fbfb3c2eb85509fb4544ae9219ccb18601b32500`

Worktree SHA-256: `5c77ce94f6fefe0cbfeaca0fcb9b1be9e983694af4eee50dd564666e56fe24a5`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Backup creation/verification/history and actual restore UI command implementation.

## Source paths and function/resource analysis

- `LastGoodBackup` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:39`: Finds latest Kind Backup Status Succeeded record and displays verification separately; does not equate successful file creation with verified restore.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:100`: Fetches actual records, sorts descending and updates computed empty/headline/stale/history properties. No timer means age label/stale threshold only refreshes with load, acceptable snapshot if clear.

- `CreateBackupAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:129`: Requires backup_manage, busy guards, leaves destination server-configured; this is file/database backup operation, not executed in audit. No stable request ID in local construction visible.

- `VerifyBackupAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:152`: Requests real verify-only backend operation; reports Verified as sound/capable of restore, though actual restore remains stronger proof and environment compatibility unknown.

- `OpenRestore` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:212`: Contradicts file summary saying restore not exposed: actual properties/commands and view implement it. Refuses open if durable pending restore, resets reason/phrase/PIN.

- `SubmitRestoreAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:231`: Reuses durable request first; fresh requires target, current user, reason, exact domain phrase and optional real manager approval. Busy registers session gate.

- `CompleteRestoreAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:251`: Recognizes success only when record.Status Completed; LastRestoreLine recognizes Succeeded. Actual backend restore status must be cross-checked; source vocabulary inconsistency alone not proven runtime failure. Retained restore can exist with RestoreTarget null after reconstruction, while IsRestoreOpen depends target; recovery UI visibility requires inspection.

- `ResolveRestoreApprovalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:247`: Single server approval call when user lacks backup_manage. Normal shell page requires backup_manage, so fallback unreachable through ordinary nav unless role/context changes.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

No backup/restore command run; mandatory dangerous operation tests must remain isolated. Missing confirmed cross-layer restore status vocabulary and recovered restore UI reachability tests.

## Recommendations (not implemented)

Reconcile docs with actual UI; use exact shared status contract and restore recovery panel; never test against real database without explicit isolated approval.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
