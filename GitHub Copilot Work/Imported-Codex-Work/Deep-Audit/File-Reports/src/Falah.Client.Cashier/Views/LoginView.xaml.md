# src/Falah.Client.Cashier/Views/LoginView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ee6949eb110557636be5379d335094cc863e3537dc7fc7b937dba13ae4e4fb61`; worktree SHA256 `ee6949eb110557636be5379d335094cc863e3537dc7fc7b937dba13ae4e4fb61`; LF-equivalent `True`.

Actual role: Legacy WPF dark PIN keypad and login action.

## Function/resource review

- `PinDisplay` at `src/Falah.Client.Cashier/Views/LoginView.xaml:20`: Bound display withMinHeight34 but no visibleemptyentryborder; errors wrap. Raw PIN not in markup.

- `UniformGrid` at `src/Falah.Client.Cashier/Views/LoginView.xaml:27`: 3columns4rows inheritsRTL fromUserControl, unlike activeWinUI explicitLTRkeypad. Source predicts mirrored visualdigit order; nativeconfirmation unobserved.

- `BoolToLoginLabelConverter` at `src/Falah.Client.Cashier/Views/LoginView.xaml:42`: Busy label bound to submit command; digits/clear/backspace relyVMguards not IsEnabledbusy binding. Backspaceglyph no explicitaccessiblename.

- `Width="440"` at `src/Falah.Client.Cashier/Views/LoginView.xaml:12`: Fixed440/padding40 no ScrollViewer risks smallviewport/largetextclipping. Decorative red PINforeground and dark backdrop needpairwisecontrast tests.

## Tests and limitations

NOT_EXECUTED; no real authentication. No callabledeclarations. Legacykeypadorder/scaling/keyboard/accessiblenames remain native-test gaps.

## Recommendations (not implemented)

Give numeric keypad explicitLTR and accessible actions only in approvedlegacyfix; activeWinUI alreadyexplicitLTR.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
