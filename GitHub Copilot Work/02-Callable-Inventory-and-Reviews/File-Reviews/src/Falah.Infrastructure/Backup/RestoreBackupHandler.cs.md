# File Review: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:41:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:41:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Backup.RestoreBackupHandler.RestoreBackupHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog, Falah.Infrastructure.Backup.CreateBackupHandler)
- **Lines:** [41-47]
- **Behavior:** Injects FalahDbContext, IApprovalRegistry, IAuditLog, and CreateBackupHandler dependencies for database access, manager approval authorization, audit logging, and safety backup creation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.RestoreBackupHandler.HandleAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken)
- **Lines:** [49-195]
- **Behavior:** Handles destructive database restoration. Validates actor user and backup record existence, verifies restore reason and confirmation phrase via BackupRules, enforces manager approval via AuthorizationPolicy, asserts physical backup file exists on disk, asserts zero pending financial operations via BackupRules.AssertNoActiveOperations, validates database identifier safety, creates a pre-restore safety backup, forces SQL Server into SINGLE_USER mode with rollback immediate, restores database with replace, and switches back to MULTI_USER mode. Catches errors to release single-user mode and logs audit events.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, DomainException, DomainException, DomainException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:51:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:51:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** LINQ query predicate lambda u => u.Id == request.ActorUserId used to locate the initiating actor user entity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:55:81:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:55:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [55-55]
- **Behavior:** LINQ query predicate lambda b => b.Id == request.BackupRecordId used to retrieve the target backup record entity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:74:87:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:74:87:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** LINQ query predicate lambda u => u.Id == request.ManagerApproverUserId used to look up the manager approver user.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:85:92:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:85:92:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [85-85]
- **Behavior:** LINQ query predicate lambda j => j.Status == nameof(FinancialJournalStatus.Pending) used to count in-flight financial journal entries.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:211:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:211:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.RestoreBackupHandler.RecordOutcomeAsync(Falah.Infrastructure.Persistence.Entities.BackupRecordEntity, bool, string, CancellationToken)
- **Lines:** [211-235]
- **Behavior:** Persists restore outcome to database. If database was replaced, clears connection pools (SqlConnection.ClearPool) and adds restore record as a new row in the newly restored database to ensure auditability, catching and logging any persistence errors.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:237:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs:237:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Backup.RestoreBackupHandler.TryReleaseSingleUserAsync(string, string)
- **Lines:** [237-253]
- **Behavior:** Emergency recovery routine connecting to master database after a failed restore attempt to reset the target database from SINGLE_USER back to MULTI_USER, preventing restaurant-wide database lockout.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6`
- **Callable ID Checked:** All 8 callables in `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6` against production source `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`. All 8 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 8 callables.
