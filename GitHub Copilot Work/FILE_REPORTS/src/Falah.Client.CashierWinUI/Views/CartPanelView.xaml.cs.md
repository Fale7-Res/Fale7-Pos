# src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `56b96463748c0aa592df53edf2104e7044b20c56f8ff75379c147d7ed0c1cd0d`

Worktree SHA-256: `006643e346cc5e43ced82b22140a80be53e648bc4b9e5ee35b43aec9f5da82f9`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Cart collection subscription, compact footer and note chip routing.

## Source paths and function/resource analysis

- `CartPanelView` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs:9`: DataContext/Loaded subscribe and Unloaded detaches collection callback correctly, avoiding retained draft event leak.

- `ApplyFooterDensity` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs:18`: Threshold 520 DIP changes segments to compact selector, total font/row heights and button grid positions. Both full/compact primary labels always payment, independent of pickup kitchen stage UI-003.

- `Subscribe` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs:21`: Unsubscribes prior VM then attaches current cart; no duplicate subscription across normal Loaded/DataContext transitions.

- `OnNoteChipClicked` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs:68`: Requires Button CartLine/Tag and delegates to VM edit guard; no arbitrary printing or money action.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NativeFooterLayoutProbe exists but not executed in this audit by reviewer. Need full nonempty/long-cart/short-screen cases.

## Recommendations (not implemented)

Implement pickup state-aware primary action only in approved future change; preserve explicit unsubscribe and test native compact threshold.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
