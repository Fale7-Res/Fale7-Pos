# src/Falah.Domain/Authorization/ApprovalRegistry.cs

- **Path:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs`
- **Component:** `Falah.Domain.Authorization.IApprovalRegistry`, `InMemoryApprovalRegistry`, `AuthorizationPolicy`
- **Assembly:** `Falah.Domain`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`ApprovalRegistry.cs` defines the core domain authorization contracts and policy evaluation engine for the Fale7 POS restaurant platform. It is the cryptographic and logical root for dual-custody authorization (the "maker-checker" pattern), where high-risk operational actions (such as order refunds, complimentary comps, cash drawer openings, shift discrepancy overrides, and manual ledger adjustments) require explicit manager sign-off.

### Core Architectural Responsibilities:
1. **Single-Use Approval Token Registry (`IApprovalRegistry`):** Provides the formal interface for registering, querying, checking consumption, and invalidating single-use manager approval tokens.
2. **In-Memory Test Double (`InMemoryApprovalRegistry`):** A dictionary- and hashset-backed implementation used primarily for domain unit testing and isolated command evaluation.
3. **Domain Authorization Engine (`AuthorizationPolicy`):**
   - Evaluates direct actor permissions against assigned strings or the wildcard `"all"`.
   - Mints cryptographically referenced manager approval records (`IssueManagerApproval`) after validating approver role eligibility.
   - Enforces dual-custody consumption rules (`ValidateApproval` / `AuthorizeCommand`) to prevent forged approvals or unauthorized overrides.
   - Enforces strict operational boundaries (e.g., owner-only funds via `AuthorizeExpense`, discrepancy-gated shift closures via `AuthorizeShiftClose`, and supplier management via `AuthorizeAddSupplier`).

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-AR-001` | `IApprovalRegistry.Register` | `void Register(ManagerApproval approval)` | [13](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L13) | Interface Contract |
| `CAL-AR-002` | `IApprovalRegistry.Find` | `ManagerApproval? Find(string approvalId)` | [14](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L14) | Interface Contract |
| `CAL-AR-003` | `IApprovalRegistry.IsConsumed` | `bool IsConsumed(string approvalId)` | [15](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L15) | Interface Contract |
| `CAL-AR-004` | `IApprovalRegistry.MarkConsumed` | `void MarkConsumed(string approvalId)` | [16](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L16) | Interface Contract |
| `CAL-AR-005` | `InMemoryApprovalRegistry.Register` | `public void Register(ManagerApproval approval)` | [24](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L24) | Substantive (Test Double) |
| `CAL-AR-006` | `InMemoryApprovalRegistry.Find` | `public ManagerApproval? Find(string approvalId)` | [25](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L25) | Substantive (Test Double) |
| `CAL-AR-007` | `InMemoryApprovalRegistry.IsConsumed` | `public bool IsConsumed(string approvalId)` | [26](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L26) | Substantive (Test Double) |
| `CAL-AR-008` | `InMemoryApprovalRegistry.MarkConsumed` | `public void MarkConsumed(string approvalId)` | [27](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L27) | Substantive (Test Double) |
| `CAL-AR-009` | `AuthorizationPolicy.HasPermission` | `public static bool HasPermission(Actor? actor, string permission)` | [37-38](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L37-L38) | Substantive (Authorization Core) |
| `CAL-AR-010` | `AuthorizationPolicy.IssueManagerApproval` | `public static ManagerApproval IssueManagerApproval(IApprovalRegistry registry, Actor approver, string action, string? reason, DateTimeOffset nowUtc)` | [40-65](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L40-L65) | Substantive (Security Boundary) |
| `CAL-AR-011` | `AuthorizationPolicy.AuthorizationResult` | `public readonly record struct AuthorizationResult(Actor Actor, ManagerApproval? Approver)` | [67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L67) | Data Definition / Struct |
| `CAL-AR-012` | `AuthorizationPolicy.AuthorizeCommand` | `public static AuthorizationResult AuthorizeCommand(IApprovalRegistry registry, Actor actor, string permission, ManagerApproval? approval = null, bool requireApproval = false, bool consumeApproval = true)` | [69-78](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L69-L78) | Substantive (Policy Enforcement) |
| `CAL-AR-013` | `AuthorizationPolicy.ValidateApproval` | `public static ManagerApproval ValidateApproval(IApprovalRegistry registry, string action, ManagerApproval approval, bool consume = true)` | [89-97](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L89-L97) | Substantive (Security Boundary) |
| `CAL-AR-014` | `AuthorizationPolicy.AuthorizeShiftClose` | `public static void AuthorizeShiftClose(IApprovalRegistry registry, Actor actor, ManagerApproval? approval, bool discrepancyNeedsManager)` | [105-111](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L105-L111) | Substantive (Operational Gate) |
| `CAL-AR-015` | `AuthorizationPolicy.AuthorizeExpense` | `public static AuthorizationResult AuthorizeExpense(IApprovalRegistry registry, Actor actor, bool paidFromDrawer, ManagerApproval? approval)` | [122-129](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L122-L129) | Substantive (Financial Boundary) |
| `CAL-AR-016` | `AuthorizationPolicy.AuthorizeShiftOpen` | `public static AuthorizationResult AuthorizeShiftOpen(Actor actor)` | [136-141](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L136-L141) | Substantive (Operational Gate) |
| `CAL-AR-017` | `AuthorizationPolicy.AuthorizeAddSupplier` | `public static AuthorizationResult AuthorizeAddSupplier(IApprovalRegistry registry, Actor actor, ManagerApproval? approval)` | [149-154](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L149-L154) | Substantive (Operational Gate) |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-AR-001
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:13:5:MethodDeclaration`
- **Exact Line Range:** Lines 13–13
- **Signature:** `IApprovalRegistry.Register(ManagerApproval)`
- **Linked Findings:** `SEC-006`, `SEC-007` to CAL-AR-004: `IApprovalRegistry` Interface Contract
- **Lines:** [11-17](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L11-L17)
- **Signatures:**
  - `void Register(ManagerApproval approval);`
  - `ManagerApproval? Find(string approvalId);`
  - `bool IsConsumed(string approvalId);`
  - `void MarkConsumed(string approvalId);`
- **Architectural Role:** Defines the lifecycle management contract for single-use approvals across physical till stations and background servers.
- **Implementations:**
  - `InMemoryApprovalRegistry` in `Falah.Domain` (ephemeral test double).
  - `EfApprovalRegistry` in `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` (production SQL persistence).
- **Classification Justification:** Core domain abstraction defining security token lifecycle operations.

---

### CAL-AR-005
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:24:5:MethodDeclaration`
- **Exact Line Range:** Lines 24–24
- **Signature:** `InMemoryApprovalRegistry.Register(ManagerApproval)`
- **Linked Findings:** `SEC-006`, `SEC-007` to CAL-AR-008: `InMemoryApprovalRegistry` Implementation
- **Lines:** [19-28](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L19-L28)
- **Actual Behavior & Inputs:**
  - `Register`: Stores `approval` in `_issued[approval.ApprovalId] = approval`.
  - `Find`: Returns `_issued.GetValueOrDefault(approvalId)`.
  - `IsConsumed`: Evaluates `_consumed.Contains(approvalId)`.
  - `MarkConsumed`: Appends `approvalId` to `_consumed.Add(approvalId)`.
- **Side Effects:** Mutates private dictionary `_issued` and private set `_consumed`.
- **Trust Boundaries & Security Implications:**
  - **Thread-Safety Vulnerability:** Uses standard non-concurrent collections (`Dictionary<string, ManagerApproval>` and `HashSet<string>`). Neither is thread-safe. Concurrent invocations from parallel unit tests or background threads can cause corrupted buckets or race conditions.
  - **Absence of Memory Eviction:** Does not prune expired or consumed approvals; memory usage grows monotonically during process lifetime.
- **Test Coverage:** Directly exercised across `tests/Falah.Domain.Tests/AuthorizationTests.cs`.

---

### CAL-AR-009
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:37:5:MethodDeclaration`
- **Exact Line Range:** Lines 37–38
- **Signature:** `AuthorizationPolicy.HasPermission(Actor?, string)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.HasPermission(Actor? actor, string permission)`
- **Line Span:** [37-38](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L37-L38)
- **Actual Behavior & Inputs:**
  - Evaluates whether `actor` is non-null and `actor.Active == true`. If inactive or null, evaluates to `false`.
  - Checks if `actor.Permissions` contains the wildcard string `"all"` OR contains the target `permission` string.
- **Callers & Dependencies:**
  - Invoked ubiquitously by `AuthorizeCommand`, `AuthorizeShiftOpen`, `AuthorizeExpense`, `AuthorizeAddSupplier`, and handlers throughout `Falah.Infrastructure`.
- **Trust Boundaries & Security Implications:**
  - **Strict Active Check:** Inactive users are unconditionally denied access regardless of role or permissions.
  - **Exact String Matching:** Relies on ordinal string comparison (`actor.Permissions.Contains(permission)`). If input permission or user permissions have casing discrepancies, evaluation fails (unless wildcard `"all"` is held).
  - **Wildcard Superuser Authority:** The literal `"all"` bypasses all individual permission requirements throughout the application.

---

### CAL-AR-010
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:40:5:MethodDeclaration`
- **Exact Line Range:** Lines 40–65
- **Signature:** `AuthorizationPolicy.IssueManagerApproval(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.IssueManagerApproval`
- **Line Span:** [40-65](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L40-L65)
- **Actual Behavior & Inputs:**
  1. **Action Validation:** Checks `string.IsNullOrWhiteSpace(action)`. If null/empty/whitespace, throws `DomainException("يجب تحديد العملية المطلوب اعتمادها.")`.
  2. **Role Authorization Gate:**
     - Evaluates `approver.Role is "owner" or "manager" || (approver.Role == "supervisor" && HasPermission(approver, action))`.
     - Validates `approver.Active`.
     - If non-permitted or inactive, throws `DomainException("هذا المستخدم غير مخول لاعتماد العملية.")`.
  3. **Token Construction:** Mints a new `ManagerApproval` record:
     - `ApprovalId`: `$"approval-{Guid.NewGuid()}"`
     - `ApproverUserId`: `approver.Id`
     - `ApproverName`: `approver.Name`
     - `ApproverRole`: `approver.Role`
     - `ApprovedAtUtc`: `nowUtc`
     - `Action`: `action`
     - `Reason`: `reason`
  4. **Registration:** Calls `registry.Register(approval)`.
  5. Returns the issued approval object.
- **Callers & Dependencies:**
  - Invoked by `IssueManagerApprovalHandler.cs` (lines 40-41) handling `POST /api/approvals/issue`.
- **Side Effects:** Adds a new approval entity to the database or in-memory registry.
- **Trust Boundaries & Security Implications:**
  - **Supervisor Privilege Bound:** A supervisor cannot issue approvals for actions they do not personally hold the permission for. However, owners and managers can approve any action without holding explicit individual permission strings.
  - **Token Generation Strength:** Relies on `Guid.NewGuid()`. While UUIDv4 offers 122 bits of pseudo-random entropy (defeating practical sequential guessing), it is not cryptographically signed (HMAC) or authenticated; validity is determined entirely by database existence.
- **Test Coverage:**
  - *Covered:* `AuthorizationTests.Manager_approval_cannot_be_issued_with_an_empty_action`, `AuthorizationTests.Cashier_cannot_issue_manager_approval`.

---

### CAL-AR-011: `AuthorizationPolicy.AuthorizationResult`
- **Line:** [67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L67)
- **Signature:** `public readonly record struct AuthorizationResult(Actor Actor, ManagerApproval? Approver);`
- **Actual Behavior:** Readonly record struct encapsulating the authorized actor and the optional manager approval token used to clear the gate.
- **Classification Justification:** Domain value object capturing audit attribution for downstream financial journaling and auditing.

---

### CAL-AR-012
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:69:5:MethodDeclaration`
- **Exact Line Range:** Lines 69–78
- **Signature:** `AuthorizationPolicy.AuthorizeCommand(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.AuthorizeCommand`
- **Line Span:** [69-78](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L69-L78)
- **Actual Behavior & Inputs:**
  1. Direct Permission Check: Calls `HasPermission(actor, permission)`. If true, returns `new AuthorizationResult(actor, null)`.
  2. Fallback Evaluation: If the actor lacks permission:
     - If `!requireApproval` or `approval is null`, throws `DomainException("ليس لديك صلاحية تنفيذ هذه العملية.")`.
     - Otherwise, delegates to `ValidateApproval(registry, permission, approval, consumeApproval)`.
  3. Returns `AuthorizationResult(actor, validatedApproval)`.
- **Callers & Dependencies:**
  - Invoked across command handlers when operations allow manager override (e.g., drawer cash movement, safe drop, purchasing receipt).
- **Trust Boundaries & Security Implications:**
  - **Maker-Checker Fallback:** Transparently permits a user lacking the permission to proceed if and only if they present a valid, unconsumed manager approval matching the exact action.
  - **Audit Completeness:** When authorized via approval, the resulting `AuthorizationResult.Approver` retains the manager's identity for audit logging.
- **Test Coverage:** Tested in `AuthorizationTests.Actor_with_matching_permission_does_not_need_approval` and `AuthorizationTests.Actor_without_permission_can_proceed_with_valid_manager_approval`.

---

### CAL-AR-013
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:89:5:MethodDeclaration`
- **Exact Line Range:** Lines 89–97
- **Signature:** `AuthorizationPolicy.ValidateApproval(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.ValidateApproval`
- **Line Span:** [89-97](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L89-L97)
- **Actual Behavior & Inputs:**
  1. Queries registry: `issued = registry.Find(approval.ApprovalId)`.
  2. Validates:
     - `issued is null`
     - `issued.Action != action`
     - `issued.ApproverUserId != approval.ApproverUserId`
     - `registry.IsConsumed(approval.ApprovalId)`
     If any condition fails, throws `DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.")`.
  3. If `consume == true`, calls `registry.MarkConsumed(approval.ApprovalId)`.
  4. Returns the validated `issued` approval.
- **Trust Boundaries & Security Implications (CRITICAL DEFECTS):**
  - **CRITICAL DEFECT (SEC-006 - Absence of Expiration / TTL):** The method never evaluates `issued.ApprovedAtUtc`. There is zero TTL check. Once issued, an approval token remains valid indefinitely until consumed. A cashier can stockpile approval tokens for days or weeks and execute unauthorized refunds/comps later.
  - **CRITICAL DEFECT (SEC-007 - Concurrency Double-Spending):** In production `EfApprovalRegistry`, `IsConsumed` and `MarkConsumed` are separate non-atomic calls without row-level locking or optimistic concurrency checks (`[Timestamp]`), permitting race conditions where two concurrent requests consume the same token simultaneously.
- **Test Coverage:** Tested in `AuthorizationTests.Manager_approval_cannot_be_consumed_twice`. Missing concurrency race and token TTL tests.

---

### CAL-AR-014
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:105:5:MethodDeclaration`
- **Exact Line Range:** Lines 105–111
- **Signature:** `AuthorizationPolicy.AuthorizeShiftClose(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.AuthorizeShiftClose`
- **Line Span:** [105-111](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L105-L111)
- **Actual Behavior & Inputs:**
  1. First verifies base blind close permission: `AuthorizeCommand(registry, actor, "shifts_blind_close")`.
  2. If `discrepancyNeedsManager == true`, invokes `AuthorizeCommand(registry, actor, "shifts_manage", approval, requireApproval: true)`.
- **Callers & Dependencies:**
  - Invoked by `CloseShiftHandler.cs` (lines 52-56).
- **Trust Boundaries & Security Implications:**
  - **No Role Bypass:** Deliberately enforces that managers must hold the explicit permission `shifts_manage` or supply a valid approval; there is no role shortcut. This guarantees that revoking `shifts_manage` from an administrator immediately stops them from overriding shift cash discrepancies.
- **Test Coverage:** Tested in `AuthorizationTests.Shift_close_with_discrepancy_requires_shifts_manage_or_approval`.

---

### CAL-AR-015
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:122:5:MethodDeclaration`
- **Exact Line Range:** Lines 122–129
- **Signature:** `AuthorizationPolicy.AuthorizeExpense(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.AuthorizeExpense`
- **Line Span:** [122-129](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L122-L129)
- **Actual Behavior & Inputs:**
  1. If `paidFromDrawer == true`:
     - Calls `AuthorizeCommand(registry, actor, "expenses_add_drawer", approval, requireApproval: true)`.
  2. If `paidFromDrawer == false` (money originating from Main Treasury, Bank, or InstaPay):
     - Validates `HasPermission(actor, "expenses_main_treasury")`.
     - If false, throws `DomainException("صرف المصروفات من الخزنة الرئيسية مسموح للمالك فقط.")`.
     - Returns `new AuthorizationResult(actor, null)`.
- **Callers & Dependencies:**
  - Invoked by `AddExpenseHandler.cs` (lines 46-52).
- **Trust Boundaries & Security Implications:**
  - **Strict Treasury Isolation:** For expenses paid from the Main Treasury, manager approval tokens are **strictly rejected**. Only actors possessing `expenses_main_treasury` (the restaurant owner) can authorize treasury disbursements, preventing managers from colluding to drain primary business reserves.
- **Test Coverage:** Tested in `AuthorizationTests.Main_treasury_expense_requires_expenses_main_treasury_and_disallows_approval_fallback`.

---

### CAL-AR-016
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:136:5:MethodDeclaration`
- **Exact Line Range:** Lines 136–141
- **Signature:** `AuthorizationPolicy.AuthorizeShiftOpen(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.AuthorizeShiftOpen`
- **Line Span:** [136-141](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L136-L141)
- **Actual Behavior & Inputs:**
  - Evaluates whether the actor possesses `shifts_manage` OR `shifts_open_own`.
  - If neither is held, throws `DomainException("ليس لديك صلاحية فتح وردية.")`.
  - Returns `new AuthorizationResult(actor, null)`.
- **Trust Boundaries & Security Implications:**
  - **No Approval Fallback:** Opening a shift does not accept a manager approval; the user must personally hold the permission.
- **Test Coverage:** Tested in `AuthorizationTests.Shift_open_requires_shifts_manage_or_shifts_open_own`.

---

### CAL-AR-017
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:149:5:MethodDeclaration`
- **Exact Line Range:** Lines 149–154
- **Signature:** `AuthorizationPolicy.AuthorizeAddSupplier(...)`
- **Linked Findings:** `SEC-006`, `SEC-007`: `AuthorizationPolicy.AuthorizeAddSupplier`
- **Line Span:** [149-154](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L149-L154)
- **Actual Behavior & Inputs:**
  - Checks if actor holds `purchasing_manage` OR `suppliers_manage`. If so, returns `AuthorizationResult(actor, null)`.
  - If neither is held, falls back to `AuthorizeCommand(registry, actor, "suppliers_manage", approval, requireApproval: true)`.
- **Trust Boundaries & Security Implications:**
  - Fixes a critical legacy parity gap where supplier creation was entirely unauthenticated.
- **Test Coverage:** Tested in `AuthorizationTests.Adding_supplier_allows_purchasing_manage_or_suppliers_manage_or_approval`.

---

## 4. Vulnerability & Risk Forensics

### SEC-006: Absence of Expiration (TTL) Check in Manager Approvals
- **Severity:** `HIGH` (CVSS v3.1: 7.4 - AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N)
- **Mechanics:**
  In `AuthorizationPolicy.ValidateApproval`:
  ```csharp
  var issued = registry.Find(approval.ApprovalId);
  if (issued is null || issued.Action != action || issued.ApproverUserId != approval.ApproverUserId || registry.IsConsumed(approval.ApprovalId))
      throw new DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.");
  ```
  The method inspects action, approver ID, and consumed status, but **never checks `issued.ApprovedAtUtc`**.
- **Exploit Scenario:**
  1. A manager issues an approval for an order refund (`orders_refund`) during the morning shift.
  2. The cashier aborts the transaction before committing. The approval ID remains unconsumed in `_db.ManagerApprovals`.
  3. Days or weeks later, when the manager is absent, any cashier can present the stale `approvalId` to execute an unauthorized refund or drawer opening.
- **Remediation:** Introduce a strict TTL check (e.g., 5 to 15 minutes maximum lifespan) in `ValidateApproval`:
  ```csharp
  if (nowUtc - issued.ApprovedAtUtc > TimeSpan.FromMinutes(15))
      throw new DomainException("انتهت صلاحية اعتماد المدير.");
  ```

### SEC-007: Approval Token Double-Spending Concurrency Hazard
- **Severity:** `HIGH` (CVSS v3.1: 7.5 - AV:N/AC:H/PR:L/UI:N/S:U/C:H/I:H/A:N)
- **Mechanics:**
  In `EfApprovalRegistry.MarkConsumed`:
  ```csharp
  var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
      ?? _db.ManagerApprovals.FirstOrDefault(a => a.ApprovalId == approvalId)
      ?? throw new InvalidOperationException("...");
  if (entity.Consumed) throw new DomainException("اعتماد المدير مستخدم بالفعل.");
  entity.Consumed = true;
  ```
  `ManagerApprovalEntity` lacks a concurrency stamp (`[Timestamp]`), and SQL SELECT does not issue `WITH (UPDLOCK, ROWLOCK)`.
- **Exploit Scenario:** Two concurrent requests (e.g. parallel checkouts or refunds) presenting the same `ApprovalId` can query `Consumed == false` simultaneously. Both write `Consumed = true` and call `SaveChangesAsync()`. SQL commits both updates without error, double-spending a single manager approval token.
- **Remediation:** Add `[Timestamp] public byte[] RowVersion { get; set; } = default!;` to `ManagerApprovalEntity`, or execute an atomic SQL update: `UPDATE ManagerApprovals SET Consumed = 1 WHERE ApprovalId = @id AND Consumed = 0`.

---

## 5. Verification & Recommendations

1. **Enforce TTL on Approvals:** Pass `DateTimeOffset nowUtc` into `ValidateApproval` and reject tokens older than 15 minutes.
2. **Prevent Double-Spending:** Add optimistic concurrency tokens (`RowVersion`) or atomic SQL condition (`Consumed == false`) in `EfApprovalRegistry.MarkConsumed`.
3. **Thread-Safe Test Double:** Update `InMemoryApprovalRegistry` to use `ConcurrentDictionary` and `ConcurrentBag` to avoid flakes under parallel xUnit runners.
4. **Static Verification:** Verified complete against `src/Falah.Domain/Authorization/ApprovalRegistry.cs`. No application source modified.

- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:14:5:MethodDeclaration` (Lines 14–14, `Find`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:15:5:MethodDeclaration` (Lines 15–15, `IsConsumed`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:16:5:MethodDeclaration` (Lines 16–16, `MarkConsumed`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:25:5:MethodDeclaration` (Lines 25–25, `Find`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:26:5:MethodDeclaration` (Lines 26–26, `IsConsumed`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:27:5:MethodDeclaration` (Lines 27–27, `MarkConsumed`)