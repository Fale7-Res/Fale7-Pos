# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `33ad90d64c3e48e372d9b8a0580d4071b98b79c5a8849e8de96828e848ebd22a`

Worktree SHA-256: `7799e030dd8ac3693288cc2cfd82163ef3cb7eb4846033f30eadaddc724e09e7`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Pickup name/phone and optional Cairo scheduled collection time.

## Source paths and function/resource analysis

- `RequestedPickupAtUtc` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs:35`: Requires both date/time, treats entered wall clock as Cairo and converts once to UTC. Conversion can throw ArgumentException for DST-invalid wall time before DomainException handler; property also evaluated from PickupSummary after each picker change. Candidate robustness issue requiring boundary repro.

- `PickupSummary` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs:27`: Composes customer and converted Cairo schedule. Depends on cross-partial notifications from customer and pickers; direct conversion is not guarded against invalid local time.

- `ClearRequestedPickupTime` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs:45`: Clears both nullable pickers, but only called when changing order type away from pickup, not when completing/resetting one pickup (UI-006).

- `SavePickupDetails` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs:77`: Validates shared DeliveryRules pickup fields, enforces date/time pair, invokes domain timing rule and trims name/phone; catches DomainException only. No persisted preparation stage exists in this file.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing successive pickup schedule isolation and DST gap property evaluation test; source-only review, no order writes.

## Recommendations (not implemented)

Reset schedule with new order lifecycle; catch or prevalidate timezone-invalid wall times; design kitchen preparation separately from payment.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
