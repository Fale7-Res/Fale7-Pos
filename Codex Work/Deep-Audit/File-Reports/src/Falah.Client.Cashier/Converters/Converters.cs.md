# src/Falah.Client.Cashier/Converters/Converters.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `74073cc5fad0043260fb5b682404e13b1518216fa99ed221bad513f910703372`; worktree SHA256 `74073cc5fad0043260fb5b682404e13b1518216fa99ed221bad513f910703372`; LF-equivalent `True`.

Actual role: WPF presentation-only converters and note command pairing.

## Function/resource review

- `NullToCollapsedConverter` at `src/Falah.Client.Cashier/Converters/Converters.cs:7`: Null/whitespace collapses; Bool and inverse use truepattern, ConvertBack most throws because one-way. Unknown types often render absent rather than parser error; relevant to bindings.

- `BoolToLoginLabelConverter` at `src/Falah.Client.Cashier/Converters/Converters.cs:30`: Uses dictionary login/checkout/confirm busy/idle with fallback login, avoids parameter exception. No localization resource system; static Arabic strings.

- `NoteAppendPairConverter` at `src/Falah.Client.Cashier/Converters/Converters.cs:87`: Packages values[0]/[1]with no Length check, potential IndexOutOfRange if malformed MultiBinding, actual paired XAML expected2. EqualsConverter guards length==2correctly.

- `PiastresToEgpConverter` at `src/Falah.Client.Cashier/Converters/Converters.cs:101`: Long scaled100m, null/unknown fallback0.00misrepresents missing monetary data aszero. This is older UI contract; nullable truth-preserving active converters separate.

- `OnlineStatusBrushConverter` at `src/Falah.Client.Cashier/Converters/Converters.cs:74`: Literal green/red status plus text converter; CashOnlyVisible/ZeroToVisible/InverseBool maps straightforward. No parsing of user code, URI/process or printing.

## Tests and limitations

No WPF converter tests current suite, need null/unknown/malformed array/currentculture cases. No rendered contrast measured.

## Recommendations (not implemented)

Show unknown distinct fromzero and guard note argument count; maintain explicit one-way bindings.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
