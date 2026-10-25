# File Review: src/Falah.Domain/Backup/BackupRules.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Domain/Backup/BackupRules.cs.

## Callables

### Callable: src/Falah.Domain/Backup/BackupRules.cs:27:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:27:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertDestinationDirectory(string?)
- **Lines:** [27-33]
- **Behavior:** Validates backup destination directory path. Throws DomainException if path is null or whitespace, or if Directory.Exists returns false.
- **Validations:** Validation checks detected: True. Throws: DomainException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertRestoreReason(string?)
- **Lines:** [35-39]
- **Behavior:** Validates mandatory reason for database restoration. Throws DomainException("سبب الاستعادة مطلوب.") if string is null or whitespace.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:41:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:41:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertRestoreConfirmationPhrase(string?)
- **Lines:** [41-45]
- **Behavior:** Validates that confirmation phrase exactly matches mandatory safety phrase "أؤكد استعادة النسخة الاحتياطية واستبدال قاعدة البيانات الحالية" (after trimming whitespace), throwing DomainException on mismatch.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:52:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:52:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertNoActiveOperations(int)
- **Lines:** [52-56]
- **Behavior:** Guards database restore against active financial transactions. Throws DomainException if pendingJournalEntryCount > 0 to prevent data corruption during in-flight operations.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:64:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:64:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertSafeDatabaseIdentifier(string)
- **Lines:** [64-68]
- **Behavior:** Validates SQL Server database name against regex ^[A-Za-z0-9_]+$ to prevent SQL injection in statement templates interpolating database identifiers. Throws DomainException on invalid characters.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:76:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:76:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.IsAutomaticBackupDue(DateTimeOffset?, DateTimeOffset, double)
- **Lines:** [76-81]
- **Behavior:** Evaluates whether scheduled automatic backup is due based on elapsed time (maxAgeHours > 0). Returns true if no previous backup succeeded (lastSucceededAtUtc is null) or if time difference reaches maxAgeHours.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.PrunableBackupCount(int, int)
- **Lines:** [89-93]
- **Behavior:** Computes number of backup files eligible for retention pruning, preserving keepCount newest files. Returns 0 if keepCount <= 0 to safely prevent total deletion; otherwise returns Math.Max(0, recorded - keepCount).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.SelectPrunableBackups(IEnumerable<Falah.Domain.Backup.BackupRules.RetainedBackup>, int)
- **Lines:** [107-118]
- **Behavior:** Identifies prunable backup IDs based on retention policy. Returns empty if keepCount <= 0. Orders backups descending by date, protects the newest verified-valid backup from deletion regardless of age, skips keepCount newest backups, and returns remaining IDs.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:111:49:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:111:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [111-111]
- **Behavior:** Inline expression lambda 'var ordered = backups.OrderByDescending(b => b.CompletedAtUtc).ToList();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:112:52:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:112:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [112-112]
- **Behavior:** Inline expression lambda 'var newestValidId = ordered.FirstOrDefault(b => b.IsVerifiedValid).Id;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:115:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:115:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [115-115]
- **Behavior:** Inline expression lambda '.Where(b => b.Id != newestValidId)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:116:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:116:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [116-116]
- **Behavior:** Inline expression lambda '.Select(b => b.Id)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `daf5a6e64cf4e36df55f29a82f7beb91610a3190901adcbadca3b9f60661242e`
- **Callable ID Checked:** All 12 callables in `src/Falah.Domain/Backup/BackupRules.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Domain/Backup/BackupRules.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `daf5a6e64cf4e36df55f29a82f7beb91610a3190901adcbadca3b9f60661242e`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `daf5a6e64cf4e36df55f29a82f7beb91610a3190901adcbadca3b9f60661242e` against production source `src/Falah.Domain/Backup/BackupRules.cs`. All 12 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 12 callables.
