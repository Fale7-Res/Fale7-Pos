# src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs

- **Path:** `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs`
- **Component:** `Falah.Infrastructure.Approvals.IssueManagerApprovalHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `a934e9e87f27da2f2e7a40000685a8806b4239f73c99f500953360132ee9117c`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 4
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`IssueManagerApprovalHandler` executes the server-side minting of single-use manager approval tokens in Fale7 POS. It handles `POST /api/approvals/issue`, converting an in-person manager gesture (typing a 4-to-6 digit PIN on the cashier terminal) into a persisted approval token that authorizes subsequent high-risk operations (refunds, comps, drawer opens, and shift discrepancy overrides).

### Forensic Security & Domain Invariants:
1. **PIN-Based Identity Resolution:** Because the approving manager steps up to someone else's physical screen, the request carries no username or claimed user ID. The handler loads all active users and verifies candidate PINs via `PinHasher.Verify`.
2. **Absence of Self-Approval Prevention:** The endpoint does not receive or validate the identity of the cashier currently logged into the station. If a user holding manager or supervisor permissions operates the till, they can supply their own PIN and approve their own actions with zero segregation of duties.
3. **Absence of Expiration (`ValidUntilUtc` / SEC-006):** The issued token records `ApprovedAtUtc = DateTimeOffset.UtcNow`, but specifies no `ValidUntilUtc` or TTL. As detailed in **SEC-006**, tokens remain valid indefinitely until consumed.
4. **Denial-of-Service Surface (Candidate PBKDF2 Scan):** A failed PIN attempt causes sequential execution of 210,000 PBKDF2 rounds across all active users in the database, imposing heavy CPU load.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-IA-001` | `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:19:5:ConstructorDeclaration` | [19-23](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs#L19-L23) | Constructor (DI Hook) |
| `CAL-IA-002` | `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:25:5:MethodDeclaration` | [25-44](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs#L25-L44) | Substantive (Security Boundary) |
| `CAL-IA-003` | `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:34:63:SimpleLambdaExpression` | [34](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs#L34) | Lambda Predicate |
| `CAL-IA-004` | `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:35:54:SimpleLambdaExpression` | [35](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs#L35) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:19:5:ConstructorDeclaration
- **Exact Line Range:** Lines 19–23
- **Signature:** `public IssueManagerApprovalHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for reading users and persisting approvals, and `IApprovalRegistry` for domain registration.
- **Concurrency & Lifecycle:** Registered with scoped lifetime per HTTP request.

---

### Callable: src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:25:5:MethodDeclaration
- **Exact Line Range:** Lines 25–44
- **Signature:** `public async Task<IssueManagerApprovalResult> HandleAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Queries all active users: `candidates = await _db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct)`.
  2. Enumerates candidates sequentially:
     `approverUser = candidates.FirstOrDefault(u => PinHasher.Verify(request.Pin ?? string.Empty, u.PinHash, u.PinSalt, u.PinIterations))`.
  3. If no candidate matches, throws `DomainException("رمز المدير غير صحيح.")`.
  4. Maps entity to domain actor: `approver = OrderMapper.ToActor(approverUser)`.
  5. Mints approval: `approval = AuthorizationPolicy.IssueManagerApproval(_approvalRegistry, approver, request.Action, request.Reason, DateTimeOffset.UtcNow)`.
  6. Persists approval: `await _db.SaveChangesAsync(ct)`.
  7. Returns `new IssueManagerApprovalResult(approval.ApprovalId, approval.ApproverUserId, approval.ApproverName, approval.Action, approval.ApprovedAtUtc)`.
- **Trust Boundaries & Security Implications:**
  - **No Self-Approval Check:** The handler does not compare `approver.Id` against the active HTTP session caller. A cashier holding a manager PIN can authorize their own comps and refunds.
  - **Indefinite Lifetime (SEC-006):** No `ValidUntilUtc` property exists on `ManagerApprovalEntity` or `IssueManagerApprovalResult`. Tokens do not expire.
  - **Single Approval Double-Spending (SEC-007):** The approval is registered in `EfApprovalRegistry`, which lacks row locking and concurrency tokens on `Consumed`.
- **Test Coverage:** Tested in `ManagerApprovalTests.Manager_approval_cannot_be_consumed_twice` and `AuthenticationTests.Manager_approval_cannot_be_issued_with_an_empty_action`.

---

### Callable: src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:34:63:SimpleLambdaExpression
- **Exact Line Range:** Line 34
- **Signature:** `u => u.Active`
- **Actual Behavior:** LINQ predicate filtering SQL query to active user accounts only, preventing deactivated staff from issuing approvals.

---

### Callable: src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:35:54:SimpleLambdaExpression
- **Exact Line Range:** Line 35
- **Signature:** `u => PinHasher.Verify(request.Pin ?? string.Empty, u.PinHash, u.PinSalt, u.PinIterations)`
- **Actual Behavior & Execution Path:** LINQ in-memory predicate executing PBKDF2 cryptographic key derivation against candidate hash metadata.
- **Security Implications:** Sequential execution of expensive PBKDF2 rounds (210,000 iterations per user) creates an asymmetric CPU consumption surface.

---

## 4. Verification & Recommendations

1. **Enforce TTL Expiration Window:** Introduce `ValidUntilUtc = DateTimeOffset.UtcNow.AddMinutes(15)` on approvals and enforce expiry in `AuthorizationPolicy.ValidateApproval`.
2. **Segregation of Duties (Self-Approval Prevention):** Inject `HttpContext` and assert `approver.Id != context.ActorId()` unless the user role is `owner`.
3. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `a934e9e87f27da2f2e7a40000685a8806b4239f73c99f500953360132ee9117c`
- **Callable ID Checked:** `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:19:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:25:5:MethodDeclaration`, `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:34:63:SimpleLambdaExpression`, `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs:35:54:SimpleLambdaExpression` (4 of 4 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs` (46 lines).
  2. Confirmed `SEC-006`: `IssueManagerApprovalHandler` records `ApprovedAtUtc = DateTimeOffset.UtcNow` but mints approval records with NO `ValidUntilUtc` or expiration TTL; tokens remain indefinitely valid until consumed.
  3. Confirmed lack of self-approval segregation: The handler does not compare the resolved manager identity against the cashier initiating the transaction on the POS station. A manager working a cashier till can approve their own high-risk actions.
  4. Confirmed candidate scan performance impact: On invalid PIN, all active users undergo PBKDF2 (210,000 iterations) verification sequentially.
  5. All 4 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `a934e9e87f27da2f2e7a40000685a8806b4239f73c99f500953360132ee9117c`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `a934e9e87f27da2f2e7a40000685a8806b4239f73c99f500953360132ee9117c` against `src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs` (46 lines).
2. Concur with L1 on `SEC-006` (absence of expiration TTL on issued approval tokens) and absence of maker-checker self-approval segregation.
3. Additional L2 Challenger Assessment:
   - In `HandleAsync` (lines 25-45), candidate manager PIN verification executes PBKDF2 (210,000 iterations) sequentially across all active candidate users. An attacker submitting invalid PINs can exhaust CPU threads, introducing a denial-of-service vector.
   - Tokens are persisted to `EfApprovalRegistry` with indefinite validity, allowing cashiers to hoard approvals for future unauthorized operations.
4. All 4 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
