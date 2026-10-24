# src/Falah.Client.CashierWinUI/Views/PickupDetailsView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `b234cabbf16e211435df6500cda270b39e5104cd80005b95d57c72ec6784e0f9`

Worktree SHA-256: `6a63b7164a459557ef4f13568fc524d19d6e53d7a39c34cd2689fe568b706202`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Customer name/phone and optional date/time pickup overlay.

## Source paths and function/resource analysis

- `Pickup_Date` at `src/Falah.Client.CashierWinUI/Views/PickupDetailsView.xaml:22`: CalendarDatePicker bound nullable PickupDate; TimePicker SelectedTime bound nullable PickupTime; Cairo conversion belongs to VM. No explicit clear schedule action exposed.

- `Pickup_Save` at `src/Falah.Client.CashierWinUI/Views/PickupDetailsView.xaml:31`: Saves in-memory details only; no preparation order identity or kitchen print call. No ScrollViewer/MaxHeight around total form, so high DPI or short viewport can clip; plausible native layout risk, not screenshot-confirmed.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing high-DPI short-screen and DST invalid-time interaction tests; no user screenshot attached.

## Recommendations (not implemented)

Provide reachable schedule reset and scrollable constrained form; treat Stage A as persisted order workflow future design.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
