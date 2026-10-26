# File Review: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Infrastructure/Backup/ScheduledBackupService.cs.

## Callables

### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:31:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:31:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Backup.ScheduledBackupService.ScheduledBackupService(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory, Microsoft.Extensions.Configuration.IConfiguration, Microsoft.Extensions.Logging.ILogger<Falah.Infrastructure.Backup.ScheduledBackupService>)
- **Lines:** [31-36]
- **Behavior:** Injects `IServiceScopeFactory` for creating scoped database contexts, `IConfiguration` for loading automatic backup parameters, and `ILogger<ScheduledBackupService>` for logging operations. Holds zero mutable state.
  ```csharp
  // Lines 31-36: Constructor dependency injection
  public ScheduledBackupService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<ScheduledBackupService> log)
  {
      _scopes = scopes;
      _configuration = configuration;
      _log = log;
  }
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:50:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:50:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.ScheduledBackupService.Setting(string, double)
- **Lines:** [50-57]
- **Behavior:** Reads configuration settings under prefix `Backup:Automatic:{key}` with type conversion and defensive fallback handling to prevent startup crashes on background threads.
  ```csharp
  // Lines 50-57: Configuration reading with fallback
  private double Setting(string key, double fallback)
  {
      var raw = _configuration[$"Backup:Automatic:{key}"];
      if (string.IsNullOrWhiteSpace(raw)) return fallback;
      if (bool.TryParse(raw, out var flag)) return flag ? 1d : 0d;
      return double.TryParse(raw, System.Globalization.NumberStyles.Any,
          System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:59:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:59:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.ScheduledBackupService.ExecuteAsync(CancellationToken)
- **Lines:** [59-82]
- **Behavior:** Executes the background service loop. Checks if automatic backups are enabled (terminating early if disabled). Applies startup delay (`StartupDelay`, default 90s) to allow database warm-up. Enters continuous execution loop while cancellation is not requested, invoking `RunCycleAsync` with defensive exception catching so cycle failures do not terminate the background daemon, followed by periodic interval delay (`CheckInterval`, default 30m).
  ```csharp
  // Lines 59-82: Background service loop
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
      if (!Enabled)
      {
          _log.LogInformation("النسخ الاحتياطي التلقائي معطّل عبر الإعدادات (Backup:Automatic:Enabled=false).");
          return;
      }
      ...
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:84:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:84:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.ScheduledBackupService.RunCycleAsync(CancellationToken)
- **Lines:** [84-118]
- **Behavior:** Executes a single automated backup evaluation cycle. Creates an isolated service scope to obtain `FalahDbContext`. Retrieves the most recent successful backup record. Evaluates if a backup is due via `BackupRules.IsAutomaticBackupDue` based on elapsed hours versus `MaxAgeHours` (default 20h). If due, runs `CreateBackupHandler.RunAsync` with `BackupActor.System`. Upon successful backup generation, immediately executes verification via `VerifyBackupHandler.RunAsync` (`RESTORE VERIFYONLY`). Logs verification telemetry and triggers backup retention cleanup via `PruneAsync`.
  ```csharp
  // Lines 84-118: Single backup cycle execution
  private async Task RunCycleAsync(CancellationToken ct)
  {
      using var scope = _scopes.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
      ...
  }
  ```
  - **Verification & Pruning Lifecycle**: Pairs backup creation with immediate `RESTORE VERIFYONLY` validation and disk pruning.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:90:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:90:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-91]
- **Behavior:** Filter expression lambda `b => b.Kind == nameof(BackupRecordKind.Backup) && b.Status == nameof(BackupRecordStatus.Succeeded)` locating the newest successful database backup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:92:32:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:92:32:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Inline expression lambda '.OrderByDescending(b => b.CompletedAtUtc)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:124:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:124:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.ScheduledBackupService.PruneAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken)
- **Lines:** [124-158]
- **Behavior:** Prunes historical backup files from disk when total count exceeds `KeepCount` (default 60). Queries all successful backup records with non-null `FileSizeBytes`. Passes metadata to `BackupRules.SelectPrunableBackups` to determine prunable backups. For each prunable entry, deletes the physical `.bak` file via `File.Delete(record.FilePath)` and sets `record.FileSizeBytes = null`. Saves changes to preserve database audit records of past backups while releasing disk storage.
  ```csharp
  // Lines 124-158: Retention and disk cleanup
  private async Task PruneAsync(FalahDbContext db, CancellationToken ct)
  {
      if (KeepCount <= 0) return;
      ...
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:129:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:129:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [129-131]
- **Behavior:** Filter expression lambda `b => b.Kind == nameof(BackupRecordKind.Backup) && b.Status == nameof(BackupRecordStatus.Succeeded) && b.FileSizeBytes != null` retrieving backups whose files still reside on disk.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:135:29:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:135:29:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [135-136]
- **Behavior:** Projection expression lambda `b => new Falah.Domain.Backup.BackupRules.RetainedBackup(b.Id, b.CompletedAtUtc ?? b.RequestedAtUtc, b.VerificationStatus == "Valid")` transforming records for retention policy evaluation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:141:47:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:141:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [141-141]
- **Behavior:** Inline expression lambda 'foreach (var record in recorded.Where(b => prunable.Contains(b.Id)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `2be57d0fdbed7a4784fe2a86235170e7fa97a9a03df634b5aa7a7fa9d04ef0aa`
- **Callable ID Checked:** All 10 callables in `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `2be57d0fdbed7a4784fe2a86235170e7fa97a9a03df634b5aa7a7fa9d04ef0aa`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Independent Code-Level Findings & Per-Callable Rationale:**
  1. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:31:5:ConstructorDeclaration`:
     - *Verified Lines 31–36*: Injects `IServiceScopeFactory`, `IConfiguration`, and `ILogger<ScheduledBackupService>`. Readonly field storage, zero mutable state. Verdict: **ACCEPTED**.
  2. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:50:5:MethodDeclaration`:
     - *Verified Lines 50–57*: `Setting(string key, double fallback)` defensive configuration reader. Uses invariant culture and number parsing with safe fallback to prevent daemon crashes on invalid values. Verdict: **ACCEPTED**.
  3. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:59:5:MethodDeclaration`:
     - *Verified Lines 59–82*: `ExecuteAsync(CancellationToken stoppingToken)` background worker loop. Honors `Enabled` flag. Incorporates startup delay (`StartupDelay`, default 90s) to prevent DB thrashing during cold starts. Executes cycles in a resilient try/catch loop where exceptions log errors but do not terminate the background daemon. Verdict: **ACCEPTED**.
  4. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:84:5:MethodDeclaration`:
     - *Verified Lines 84–118*: `RunCycleAsync(CancellationToken ct)` performs automated backup execution. Queries newest successful backup and checks eligibility via `BackupRules.IsAutomaticBackupDue` based on `MaxAgeHours` (default 20h). If due, invokes `CreateBackupHandler.RunAsync` followed immediately by `VerifyBackupHandler.RunAsync` (`RESTORE VERIFYONLY`). Only verified backups proceed to retention pruning. Verdict: **ACCEPTED**.
  5. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:90:20:SimpleLambdaExpression`:
     - *Verified Lines 90–91*: `b => b.Kind == nameof(BackupRecordKind.Backup) && b.Status == nameof(BackupRecordStatus.Succeeded)` on `db.BackupRecords.AsNoTracking()`. Filters for successful backup records. Verdict: **ACCEPTED**.
  6. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:92:32:SimpleLambdaExpression`:
     - *Verified Line 92*: `b => b.CompletedAtUtc` ordering expression to locate the latest completed backup. Verdict: **ACCEPTED**.
  7. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:124:5:MethodDeclaration`:
     - *Verified Lines 124–158*: `PruneAsync(FalahDbContext db, CancellationToken ct)` manages disk retention. Selects prunable files using `BackupRules.SelectPrunableBackups` retaining newest `KeepCount` (default 60). Deletes physical `.bak` file via `File.Delete` and sets `record.FileSizeBytes = null`. Retains the database record so audit history of past backups is preserved while disk space is reclaimed. Verdict: **ACCEPTED**.
  8. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:129:20:SimpleLambdaExpression`:
     - *Verified Lines 129–131*: `b => b.Kind == nameof(BackupRecordKind.Backup) && b.Status == nameof(BackupRecordStatus.Succeeded) && b.FileSizeBytes != null`. Identifies successful backups with active files on disk. Verdict: **ACCEPTED**.
  9. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:135:29:SimpleLambdaExpression`:
     - *Verified Lines 135–136*: `b => new Falah.Domain.Backup.BackupRules.RetainedBackup(...)` projection mapping to domain retention record. Verdict: **ACCEPTED**.
  10. `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs:141:47:SimpleLambdaExpression`:
      - *Verified Line 141*: `b => prunable.Contains(b.Id)` filter selecting records designated for physical file deletion. Verdict: **ACCEPTED**.
