# src/Falah.Client.CashierWinUI/Views/CashierView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `c3e33f47b210063aa40d753c28140ca73cd06f992558550cace9510c10c80e30`

Worktree SHA-256: `4531af2dec4309d133530be9b28327dc14b7675c0179ac70a51bb300bc22a26f`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Main POS workspace and stacked custom modal overlays.

## Source paths and function/resource analysis

- `WorkspaceGrid` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml:43`: Actual F2/F3/F12 accelerators on workspace; overlays are sibling Grids, no ContentDialog focus trap/inert background semantics. No visual/keyboard proof presumed.

- `IsAddExpenseOpen` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml:292`: Seven expense category Buttons use unselected WhiteBrush and themed TextPrimaryBrush at lines 313-340, dark foreground #E6E9EE on #FFFFFF approximately 1.22:1. UI-002 source-confirmed. Selected state ActionBlue/White; none of category buttons bind IsEnabled to CanEditOperation.

- `IsCloseShiftOpen` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml:216`: Count and expected preview phases separated. PasswordBox for manager approval; custom overlay has fixed 420 width plus 48 outer margin.

- `HasCheckoutResult` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml:163`: Electronic actions only visible inside result overlay; result lost after process restart, preventing durable request retry UI-005. Print warning is queue failure, does not prove printed paper.

- `IsVariantPickerOpen` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml:123`: Variants use real star/auto columns and offered fixed-price banner. Offer banner literal light palette has mutually coherent pair, not same issue as WhiteBrush/themed foreground.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

No actual screenshot supplied to reviewer; dark contrast calculated from source tokens, not measured pixels. Missing keyboard focus trap and every category state screenshot tests.

## Recommendations (not implemented)

Pair surface/text semantic tokens in unselected states; bind operation disablement consistently; use accessible modal/focus behavior. No XAML modified.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
