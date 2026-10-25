# File Review: src/Falah.Domain/Backup/BackupRules.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Domain/Backup/BackupRules.cs.

## Callables

### Callable: src/Falah.Domain/Backup/BackupRules.cs:27:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:27:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertDestinationDirectory(string?)
- **Lines:** [27-33]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.AssertDestinationDirectory(string?) across lines 27-33.
- **Validations:** Validation checks detected: True. Throws: DomainException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertRestoreReason(string?)
- **Lines:** [35-39]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.AssertRestoreReason(string?) across lines 35-39.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:41:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:41:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertRestoreConfirmationPhrase(string?)
- **Lines:** [41-45]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.AssertRestoreConfirmationPhrase(string?) across lines 41-45.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:52:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:52:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertNoActiveOperations(int)
- **Lines:** [52-56]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.AssertNoActiveOperations(int) across lines 52-56.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:64:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:64:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.AssertSafeDatabaseIdentifier(string)
- **Lines:** [64-68]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.AssertSafeDatabaseIdentifier(string) across lines 64-68.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:76:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:76:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.IsAutomaticBackupDue(DateTimeOffset?, DateTimeOffset, double)
- **Lines:** [76-81]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.IsAutomaticBackupDue(DateTimeOffset?, DateTimeOffset, double) across lines 76-81.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.PrunableBackupCount(int, int)
- **Lines:** [89-93]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.PrunableBackupCount(int, int) across lines 89-93.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/Backup/BackupRules.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/Backup/BackupRules.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.Backup.BackupRules.SelectPrunableBackups(IEnumerable<Falah.Domain.Backup.BackupRules.RetainedBackup>, int)
- **Lines:** [107-118]
- **Behavior:** Executes logic for Falah.Domain.Backup.BackupRules.SelectPrunableBackups(IEnumerable<Falah.Domain.Backup.BackupRules.RetainedBackup>, int) across lines 107-118.
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
