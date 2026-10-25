# src/Falah.Domain/Authorization/PermissionCatalog.cs

- **Path:** `src/Falah.Domain/Authorization/PermissionCatalog.cs`
- **Component:** `Falah.Domain.Authorization.PermissionCatalog`, `PermissionDefinition`
- **Assembly:** `Falah.Domain`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`PermissionCatalog.cs` defines the authoritative, evidence-based permission taxonomy for the entire Fale7 POS system. 

### The Legacy Gap & Architectural Resolution:
In the legacy reference JavaScript application, permission strings were famously fragmented:
- The backend/logic checked up to 38 distinct permission strings scattered across business logic.
- However, the UI permissions screen (`UsersPermissionsScreen.tsx`) exposed only 10 of the 38 strings, making the remaining 28 permissions completely unmanageable from the till.
- Many strings gated features that had not yet been implemented or existed only as aspirational placeholders.

`PermissionCatalog` resolves this by establishing an **authoritative catalog built from construction**:
1. **Evidence-Based Completeness:** Every permission string checked by `AuthorizationPolicy`, minimal API endpoint gates, or domain handlers is explicitly registered here.
2. **Taxonomic Categorization:** Each permission is mapped to an operational functional area (e.g., `system`, `orders`, `shifts`, `expenses`, `treasury`, `business_day`, `purchasing`, `inventory`, `menu`, `settings`, `delivery`, `reports`, `users`, `audit`, `employees`, `backup`).
3. **Public Introspection:** Exposed via `GET /api/permissions/catalog` to allow client UIs (`UsersViewModel.cs`) to dynamically render permission toggles without hardcoding permissions locally or drifting from server rules.
4. **Validation Anchor:** Serves as the ground truth for `UserManagementRules.AssertKnownPermissions(IEnumerable<string> permissions)`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-PC-001` | `Keys::<lambda>` | `src/Falah.Domain/Authorization/PermissionCatalog.cs:57:67:SimpleLambdaExpression` | [57](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PermissionCatalog.cs#L57) | Lambda Expression / Projection |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-PC-001: `Keys::<lambda>` (`p => p.Key`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/PermissionCatalog.cs:57:67:SimpleLambdaExpression`
- **Exact Line Range:** Line 57
- **Signature:** `p => p.Key` (in `public static readonly IReadOnlySet<string> Keys = All.Select(p => p.Key).ToHashSet();`)
- **Actual Behavior & Inputs:**
  - Evaluates during static type initialization of `PermissionCatalog`.
  - Iterates over `All` (38 `PermissionDefinition` instances), projecting the `string Key` property of each item.
  - Passes the resulting `IEnumerable<string>` to `.ToHashSet()` to create an immutable `IReadOnlySet<string>`.
- **Callers & Dependencies:**
  - Invoked during static constructor execution.
  - The resulting `Keys` set is referenced by `UserManagementRules.AssertKnownPermissions` (`src/Falah.Domain/Authorization/UserManagementRules.cs#L67`) to reject any unknown permission string during user creation or permission modification.
- **Side Effects:** Allocates the static `HashSet<string>` holding 38 permission keys in memory.
- **Trust Boundaries & Security Implications:**
  - **O(1) Whitelist Enforcement:** By compiling `Keys` into a `HashSet`, lookup operations in `AssertKnownPermissions` execute in $O(1)$ constant time.
  - **Ordinal String Comparison:** The default `HashSet<string>` constructor performs strictly case-sensitive ordinal comparison (`StringComparer.Ordinal`). Thus, permission keys must match exact lowercase snake_case (e.g., `"users_manage"`, NOT `"Users_Manage"`).
- **Concurrency & Thread Safety:** The static set is initialized once by the .NET runtime type loader and exposed as `IReadOnlySet<string>`, guaranteeing thread-safe, lock-free concurrent reads across all HTTP request threads.
- **Test Coverage:** Tested in `UserManagementRulesTests.Rejects_unknown_permissions`.

---

## 4. Taxonomic Catalog Inventory (38 Permissions)

The static catalog `PermissionCatalog.All` establishes 38 distinct permissions structured across 16 functional domains:

| Category | Permission Key | Description / Gated Operation |
| :--- | :--- | :--- |
| **system** | `all` | Wildcard superuser bypass; clears all permission checks across the system. |
| **orders** | `pos` | Core point-of-sale order entry and creation. |
| **orders** | `cash_drawer_sale` | Opening physical cash drawer during standard sales checkout. |
| **orders** | `orders_comp` | Complimentary order discounting / food hospitality write-offs. |
| **orders** | `orders_refund` | Order refunding and return processing. |
| **orders** | `orders_discount` | Line-item and order-level promotional/manual discounts. |
| **orders** | `orders_view_all` | Cross-cashier order visibility on till screens. |
| **shifts** | `shifts_blind_close` | Submitting blind cash counts to close shifts. |
| **shifts** | `shifts_manage` | Administrative shift management and cash discrepancy overrides. |
| **shifts** | `shifts_open_own` | Opening cashier shift for the logged-in user. |
| **expenses** | `expenses_add_drawer` | Recording petty cash disbursements from till drawer. |
| **expenses** | `expenses_main_treasury` | Disbursing funds from Main Treasury, Bank, or InstaPay (Owner-only). |
| **treasury** | `treasury_manage` | Primary treasury deposits, safe drops, and transfers. |
| **treasury** | `safe_drop` | Mid-shift cash drops from till drawer to drop safe. |
| **treasury** | `cash_drawer` | Manual cash drawer opening outside sale workflow. |
| **business_day** | `business_day_open` | Initializing restaurant business day. |
| **business_day** | `business_day_close` | Closing restaurant business day. |
| **business_day** | `business_day_reopen` | Administrative emergency reopening of closed business day. |
| **business_day** | `control_center` | Accessing control center operations and checklists. |
| **business_day** | `cash_count` | Recording physical vault/treasury cash counts. |
| **purchasing** | `purchasing_manage` | Creating purchase orders and receiving vendor deliveries. |
| **purchasing** | `suppliers_manage` | Creating and managing vendor supplier profiles. |
| **inventory** | `inventory_manage` | Stock movements, waste tracking, and inventory adjustments. |
| **menu** | `menu_manage` | Creating and modifying menu items and categories. |
| **menu** | `offers_manage` | Creating and managing promotional offers and combos. |
| **settings** | `settings_manage` | Modifying system settings and thermal printer configurations. |
| **delivery** | `delivery_manage` | Creating couriers and delivery platform configurations. |
| **delivery** | `delivery_override` | Overriding delivery order status and address restrictions. |
| **delivery** | `delivery_settlement_approve` | Approving courier cash custody settlements. |
| **reports** | `reports_view` | Accessing financial and operational analytics and reports. |
| **users** | `users_manage` | Creating users, resetting PINs, and updating permissions. |
| **audit** | `audit_view` | Reading immutable system audit trail logs. |
| **employees** | `employees_manage` | Creating employee records and configuring wages. |
| **employees** | `employees_ledger` | Recording advances, bonuses, and manual employee ledger entries. |
| **employees** | `attendance_manage` | Clock-in, clock-out, and attendance adjustments. |
| **employees** | `payroll_approve` | Approving monthly/periodic payroll calculation runs. |
| **employees** | `payroll_pay` | Disbursing payroll funds to staff. |
| **backup** | `backup_manage` | Triggering manual database backups and restoring from archives. |

---

## 5. Security & Immutability Analysis

1. **Catalog Integrity & Immutability:**
   - `PermissionCatalog.All` is typed as `IReadOnlyList<PermissionDefinition>`, but instantiated with a standard `new List<PermissionDefinition>`. In .NET, a malicious or errant in-process component could cast `(List<PermissionDefinition>)PermissionCatalog.All` and mutate the catalog.
   - *Recommendation:* Wrap with `Array.AsReadOnly(...)` or `ImmutableList<PermissionDefinition>` to enforce true runtime immutability.
2. **Wildcard Superuser Privilege (`"all"`):**
   - The inclusion of `"all"` within `PermissionCatalog.All` means it is a valid, assignable permission key. `AuthorizationPolicy.HasPermission` checks `actor.Permissions.Contains("all")`. Holding `"all"` bypasses all 37 other granular permission checks.
   - Assignment of `"all"` is guarded at the domain level by `UserManagementRules.AssertNoPrivilegeEscalation`, which prevents any actor from granting `"all"` unless they already hold `"all"`.
3. **Static Verification:** Verified complete against `src/Falah.Domain/Authorization/PermissionCatalog.cs`. No application source modified.
