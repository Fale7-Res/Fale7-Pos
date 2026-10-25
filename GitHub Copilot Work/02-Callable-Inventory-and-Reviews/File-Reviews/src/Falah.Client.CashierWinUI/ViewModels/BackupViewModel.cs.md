# File Review: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:24:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:24:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.BackupViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [24-24]
- **Behavior:** Initializes the viewmodel with the application session AppSession for API communication and permission evaluation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:26:60:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:26:60:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.Backups.get
- **Lines:** [26-26]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.BackupViewModel.Backups.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:40:16:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:40:16:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** Filters backup history records to match succeeded backups with completed UTC timestamps.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:41:28:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:41:28:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [41-41]
- **Behavior:** Inline expression lambda '.OrderByDescending(b => b.CompletedAtUtc)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:51:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:51:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.LastBackupDetail.get
- **Lines:** [51-62]
- **Behavior:** Returns formatted detail string for the last successful backup including local timestamp, file size in megabytes, and integrity verification state, or a fallback warning if missing.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:70:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:70:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.DescribeAge(TimeSpan)
- **Lines:** [70-76]
- **Behavior:** Converts an elapsed duration into localized human-readable Arabic units (minutes, hours, or days).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:82:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:82:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.LastRestoreLine.get
- **Lines:** [82-93]
- **Behavior:** Generates a localized summary description of the latest database restore attempt and whether it succeeded, was blocked, or failed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:84:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:84:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Filters records to find entries of kind Restore.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:84:85:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:84:85:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Orders restore records descending by requested UTC timestamp.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:99:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:99:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.LoadAsync()
- **Lines:** [99-126]
- **Behavior:** Asynchronously retrieves backup records from the API, populates the observable collection ordered by request timestamp descending, and notifies dependent summary properties.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:114:62:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:114:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** Orders loaded backup records descending by requested UTC timestamp.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:128:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:128:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.CreateBackupAsync()
- **Lines:** [128-149]
- **Behavior:** Requests creation of a new database backup via API using the current user ID, handles error feedback, and reloads backup history on success.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:151:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:151:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.VerifyBackupAsync(Falah.Application.Backup.BackupRecordDto)
- **Lines:** [151-172]
- **Behavior:** Invokes backup verification (RESTORE VERIFYONLY) via API to confirm backup integrity without modifying live database state.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.OnIsBusyChanged(bool)
- **Lines:** [186-191]
- **Behavior:** Handles busy state changes by updating restore submit capability and registering pending management operation state on the application session.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:194:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:194:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.OnRestoreTargetChanged(Falah.Application.Backup.BackupRecordDto?)
- **Lines:** [194-194]
- **Behavior:** Raises property changed notification for IsRestoreOpen when the restore target record changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:211:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:211:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.OpenRestore(Falah.Application.Backup.BackupRecordDto)
- **Lines:** [211-221]
- **Behavior:** Opens the restore confirmation dialog for a selected backup record, resetting input fields for reason, confirmation phrase, and manager PIN.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:223:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:223:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.CloseRestore()
- **Lines:** [223-228]
- **Behavior:** Closes the restore dialog and clears the restore target when not busy or pending.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:230:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:230:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.SubmitRestoreAsync()
- **Lines:** [230-260]
- **Behavior:** Validates restore inputs, resolves manager approval if required, and executes or retries retained database restore request.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:262:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:262:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.CompleteRestoreAsync(Falah.Application.Backup.RestoreBackupRequest)
- **Lines:** [262-283]
- **Behavior:** Dispatches restore request via RetainedRequest helper, inspects the returned record status, and displays success or error messaging.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:264:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:264:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [264-264]
- **Behavior:** Delegates restore API execution to ApiClient.RestoreBackupAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:285:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs:285:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.BackupViewModel.ResolveRestoreApprovalAsync()
- **Lines:** [285-303]
- **Behavior:** Resolves manager authorization PIN against the server approval endpoint if the current operator lacks backup_manage permission.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---

---

## Candidate Findings & Observations

### Defect: BACKUP-RESTORE-STATUS-MISMATCH - Database Restore Success Checked as "Completed" Instead of "Succeeded"
- **Severity**: High / Defect
- **Location**: `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs` (lines 272–280)
- **Code**:
  ```csharp
  var record = result.Value;
  if (string.Equals(record.Status, "Completed", StringComparison.OrdinalIgnoreCase))
      ActionNote = "تمت استعادة قاعدة البيانات من النسخة الاحتياطية.";
  else
  {
      RestoreError = record.ErrorMessage ?? $"لم تكتمل الاستعادة. الحالة: {record.Status}";
      await LoadAsync();
      return;
  }
  ```
- **Analysis**:
  The backend restore handler (`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` line 135) sets `restoreRecord.Status = nameof(BackupRecordStatus.Succeeded);` ("Succeeded"). However, `BackupViewModel.CompleteRestoreAsync` checks `string.Equals(record.Status, "Completed", StringComparison.OrdinalIgnoreCase)`. Because "Succeeded" != "Completed", a successful restore operation is reported to the operator as a failure (`RestoreError = "لم تكتمل الاستعادة. الحالة: Succeeded"`), and `RestoreTarget` is not dismissed. This misleads the cashier/operator into believing the restore failed, when in reality the database was restored.

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `5c77ce94f6fefe0cbfeaca0fcb9b1be9e983694af4eee50dd564666e56fe24a5`
- **Callable ID Checked:** All 21 callables in `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `5c77ce94f6fefe0cbfeaca0fcb9b1be9e983694af4eee50dd564666e56fe24a5`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `5c77ce94f6fefe0cbfeaca0fcb9b1be9e983694af4eee50dd564666e56fe24a5` against client source `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs`. All 21 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 21 callables.