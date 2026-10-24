# src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `515219765f36ce15d4c0470c3f1272a191da27af2cc4508d8ce8bcaaec21bd33`

Worktree SHA-256: `abe203792ef03f7e0562f6f1029dbaba99cda77914f7f220d16893b9dd2b9373`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Older WPF technical-reference checkout; distinguish from active WinUI.

## Source paths and function/resource analysis

- `ChangeDuePiastres` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:29`: Parses AmountPaidText as integer long directly while OpenCheckout formats EGP decimal text. Default e.g. 10.00 fails long parse so change zero; quick cash 100 becomes100 piastres rather than10000 when compared to total. Source-confirmed display unit mismatch UI-013, affects WPF reference not active WinUI.

- `SubmitPaymentAsync` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:83`: Uses current-culture decimal parse, silently substitutes zero if parse fails and rounds >2decimals; overflow can escape. Rebuilds request on retry from editable cart with same key, no frozen DTO/outcomeUnknown gate. CloseCheckout unconditional, unlike active WinUI safeguards.

- `OpenCheckout` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:40`: Checks cart only, defaults cash and formats estimated total. No live menu/shift guard in this function. Data requirements for pickup/customer absent; this older client is functionally narrower.

- `SetPaymentMethod` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:54`: Sets amount equal estimate for noncash; PressAmountKey/QuickCash alter amount and refresh computed change. No cross-partial amount setter notification found in this file.

- `StartNewOrder` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:143`: Clears result/error only; no durable recovery. Active WinUI must remain deployment target until reference client officially retired or parity addressed.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Current client test project compiles WinUI sources, not WPF; WPF money display lacks coverage here.

## Recommendations (not implemented)

Do not deploy reference client accidentally. If maintained later, use CashInput integer units and immutable durable operations, but no source fixes during audit.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
