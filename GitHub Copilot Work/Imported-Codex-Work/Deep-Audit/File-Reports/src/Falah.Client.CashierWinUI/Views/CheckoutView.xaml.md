# src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `37d0d489d8c56aeb38a8e988f3167760842741100aff957846844f12d705e135`

Worktree SHA-256: `0d6cc87cbff88a8682a199d69ebf3899f5986da39a211e8401f3eaea1b5c061d`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Tender dialog, discounts/comp/split, keypad and submit footer.

## Source paths and function/resource analysis

- `IsCheckoutOpen` at `src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml:3`: Pinned total/header/footer with scrolling two-column body. Total explicitly says estimate; backend response remains authoritative.

- `PaymentMethodButtonStyle` at `src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml:31`: Four method buttons override base style with unselected WhiteBrush/TextPrimaryBrush, repeats dark contrast root UI-002 beyond expense. Changing theme with bound payment value unchanged also needs converter refresh UI-004.

- `IsHitTestVisible` at `src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml:24`: Body blocked via hit-testing but keyboard focus behavior must be separately verified; individual inputs use IsEnabled, some keypad buttons rely on command guards.

- `Checkout_Submit` at `src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml:118`: Visible fixed footer command; IsProcessingCheckout CanExecute controls command generated in VM. No prepayment kitchen stage UI-003.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing native dark/light payment method state test and per-DPI pinned-header/footer assertions. Constructor-only code-behind reviewed separately.

## Recommendations (not implemented)

Semantic unselected surface brush, theme-reactive visual states, native keyboard/RTL/high-scale verification.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
