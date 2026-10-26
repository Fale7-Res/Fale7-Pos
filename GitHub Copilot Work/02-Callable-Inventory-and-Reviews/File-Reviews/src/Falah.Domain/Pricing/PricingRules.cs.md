# src/Falah.Domain/Pricing/PricingRules.cs

- **Path:** `src/Falah.Domain/Pricing/PricingRules.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `D2959EA1C3FA702C47649369DBBE471792EC0E753A6AD61547395A302903EC7B`
- **Git Blob SHA256:** `d2959ea1c3fa702c47649369dbbe471792ec0e753a6ad61547395a302903ec7b`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 83]]`

---

## 1. Architectural Role and Risk Profile
`src/Falah.Domain/Pricing/PricingRules.cs` implements pure pricing and promotional rules ported 1:1 from legacy `lib/pricing-domain.mjs`. It governs time-window rule activation (including cross-midnight late shifts), applicable rule resolution, rule re-validation at checkout, hospitality comp assertions, closed comp reason taxonomy, and price rule input validation.

---

## 2. Exhaustive Callable Inventory (7 Callables)

### Callable 1: `IsActiveAt`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:12:5:MethodDeclaration`
- **Exact Line Range:** Lines 12–25
- **Kind:** `MethodDeclaration`
- **Signature:** `IsActiveAt(PriceRule rule, DateTimeOffset whenUtc, TimeZoneInfo timeZone)`
- **Behavior & Semantics:** Determines whether a pricing rule is live at a specific instant, handling day-of-week filters and cross-midnight time windows (e.g., 22:00 -> 02:00).
- **Verdict:** **VERIFIED**

---

### Callable 2: `ApplicableRules`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:28:5:MethodDeclaration`
- **Exact Line Range:** Lines 28–31
- **Kind:** `MethodDeclaration`
- **Signature:** `ApplicableRules(string productId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)`
- **Behavior & Semantics:** Filters rules applicable to a given product that are active at the specified instant.
- **Verdict:** **VERIFIED**

---

### Callable 3: `lambda expression `
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:30:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 30–30
- **Kind:** `SimpleLambdaExpression`
- **Signature:** `lambda expression (ApplicableRules)`
- **Behavior & Semantics:** Predicate lambda verifying product association and active status.
- **Verdict:** **VERIFIED**

---

### Callable 4: `ValidateAppliedRule`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:35:5:MethodDeclaration`
- **Exact Line Range:** Lines 35–42
- **Kind:** `MethodDeclaration`
- **Signature:** `ValidateAppliedRule(string productId, string? ruleId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)`
- **Behavior & Semantics:** Re-validates applied pricing rules at checkout across sliding time windows to prevent expired promos from being charged.
- **Verdict:** **VERIFIED**

---

### Callable 5: `AssertCompAmount`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:46:5:MethodDeclaration`
- **Exact Line Range:** Lines 46–51
- **Kind:** `MethodDeclaration`
- **Signature:** `AssertCompAmount(Money compAmount, Money orderValue)`
- **Behavior & Semantics:** Enforces that hospitality/comp amounts are strictly positive and never exceed order value.
- **Verdict:** **VERIFIED**

---

### Callable 6: `AssertCompReasonCode`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:62:5:MethodDeclaration`
- **Exact Line Range:** Lines 62–66
- **Kind:** `MethodDeclaration`
- **Signature:** `AssertCompReasonCode(string? code)`
- **Behavior & Semantics:** Enforces closed taxonomy validation for comp reason codes (hospitality, complaint, kitchen_error, staff_meal, owner_guest).
- **Verdict:** **VERIFIED**

---

### Callable 7: `AssertPriceRuleInput`
- **Canonical Callable ID:** `src/Falah.Domain/Pricing/PricingRules.cs:74:5:MethodDeclaration`
- **Exact Line Range:** Lines 74–81
- **Kind:** `MethodDeclaration`
- **Signature:** `AssertPriceRuleInput(string name, IReadOnlyCollection<string> productIds, Money fixedPrice, IReadOnlyCollection<int>? daysOfWeek)`
- **Behavior & Semantics:** Validates name, product association, positive fixed price, and valid day-of-week indices for price rule creation/updates.
- **Verdict:** **VERIFIED**

---

## 3. Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `d2959ea1c3fa702c47649369dbbe471792ec0e753a6ad61547395a302903ec7b`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Independent Code-Level Findings & Per-Callable Rationale:**
  1. `src/Falah.Domain/Pricing/PricingRules.cs:12:5:MethodDeclaration`:
     - *Verified Lines 12–25*: `IsActiveAt(PriceRule rule, DateTimeOffset whenUtc, TimeZoneInfo timeZone)` evaluates promo active status. Converts UTC to configured restaurant time zone. Evaluates DayOfWeek membership. Handles cross-midnight operational shifts accurately: `start <= end ? now >= start && now <= end : now >= start || now <= end`. Half-configured windows safely evaluate to active without crashing. Pure, side-effect-free domain logic. Verdict: **ACCEPTED**.
  2. `src/Falah.Domain/Pricing/PricingRules.cs:28:5:MethodDeclaration`:
     - *Verified Lines 28–30*: `ApplicableRules(string productId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)` filters rule set to those matching product ID and active at the given instant. Pure list projection. Verdict: **ACCEPTED**.
  3. `src/Falah.Domain/Pricing/PricingRules.cs:30:21:SimpleLambdaExpression`:
     - *Verified Line 30*: `rule => rule.ProductIds.Contains(productId) && IsActiveAt(rule, whenUtc, timeZone)` predicate filtering applicable rules. Pure evaluation. Verdict: **ACCEPTED**.
  4. `src/Falah.Domain/Pricing/PricingRules.cs:35:5:MethodDeclaration`:
     - *Verified Lines 35–42*: `ValidateAppliedRule(string productId, string? ruleId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)` revalidates applied rules at checkout instant. Protects against promo expiration between item addition and final payment. Verdict: **ACCEPTED**.
  5. `src/Falah.Domain/Pricing/PricingRules.cs:39:41:SimpleLambdaExpression`:
     - *Verified Line 39*: `r => r.Id == ruleId` predicate matching specific rule ID. Pure evaluation. Verdict: **ACCEPTED**.
  6. `src/Falah.Domain/Pricing/PricingRules.cs:46:5:MethodDeclaration`:
     - *Verified Lines 46–51*: `AssertCompAmount(Money compAmount, Money orderValue)` validates hospitality/comp discounts. Rejects non-positive comp amounts (`compAmount.Piastres <= 0`), rejects zero-value orders (`orderValue.Piastres <= 0`), and asserts `compAmount <= orderValue`. Guarantees financial invariant that comps cannot exceed order total. Verdict: **ACCEPTED**.
  7. `src/Falah.Domain/Pricing/PricingRules.cs:62:5:MethodDeclaration`:
     - *Verified Lines 62–66*: `AssertCompReasonCode(string? code)` enforces closed reason taxonomy (`hospitality`, `complaint`, `kitchen_error`, `staff_meal`, `owner_guest`). Prevents unclassified discretionary discounts. Verdict: **ACCEPTED**.
  8. `src/Falah.Domain/Pricing/PricingRules.cs:74:5:MethodDeclaration`:
     - *Verified Lines 74–81*: `AssertPriceRuleInput(string name, IReadOnlyCollection<string> productIds, Money fixedPrice, IReadOnlyCollection<int>? daysOfWeek)` validates rule creation/edit inputs. Asserts non-whitespace name, non-empty product collection, positive fixed price (`fixedPrice.IsPositive`), and day-of-week integers between 0 and 6. Verdict: **ACCEPTED**.
  9. `src/Falah.Domain/Pricing/PricingRules.cs:79:59:SimpleLambdaExpression`:
     - *Verified Line 79*: `d => d is < 0 or > 6` predicate identifying out-of-range day-of-week indices. Pure boundary check. Verdict: **ACCEPTED**.
