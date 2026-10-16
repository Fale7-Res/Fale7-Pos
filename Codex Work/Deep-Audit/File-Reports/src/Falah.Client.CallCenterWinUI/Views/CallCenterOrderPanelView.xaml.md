# src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9e47e3d9fde09da74c2a2126485412d0e2b29243f1638661540cdfbe2b3c54be`; worktree SHA256 `9e47e3d9fde09da74c2a2126485412d0e2b29243f1638661540cdfbe2b3c54be`; LF-equivalent `True`.

Actual role: CallCenter phone-order draft, cart and payment footer.

## Function/resource review

- `IsHitTestVisible` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:26`: Header/cart/payment regions use CanEditOperation as hit-test flag rather than IsEnabled; keyboard focus may still interact, so command/model gates remain necessary. Phone field immediately updates and lookup is asynchronous UI007 context.

- `DeliverySummary` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:62`: Summary wraps3lines, check/warning icons use actual details flag, user can open full address editor. No independent text describes icon state, but summary may convey fields.

- `EmptyCartHint` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:77`: Named empty/cart scroll visibility controlled in codebehind, not static binding. External Cart event unsubscribe risk UI015 belongs codebehind.

- `RemoveLineCommand` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:106`: Nested root binds row operations correctly; delete and plus/minus have explicit accessible Arabic names. Quantity30px may be narrow for large numbers.

- `OnNoteChipClicked` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:135`: Flyout has two-way Notes and chip Click with44px menu opener. Popup DataContext inheritance and current line targeting require native test. Text is a real draft and no physical print sent here.

- `WalletProvider` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:174`: Wallet textbox lacks CanEditOperation disabling even where payment segments hit-test gated; immutable pending DTO protects retry but editable draft can diverge visually. VM server validation authoritative.

- `SubmitCallCenterOrderButton` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml:201`: Submit uses IsShiftOpen enablement and command; no busy caption in markup. Clear explicitly uses CanEditOperation. Footer includes subtotal and total delivery but no tax/breakdown or estimated qualifier; authoritative amount returned by server.

## Tests and limitations

NOT_EXECUTED. Verify keyboard edits during unresolved operation, wallet fields, many lines/scaled footer, note flyout binding, exact retry and totals after server repricing. No callable symbols in XAML.

## Recommendations (not implemented)

Prefer actual enabled states plus model guards and explicit busy/retry feedback. Keep command immutability and process recovery tests distinct from pointer disable.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
