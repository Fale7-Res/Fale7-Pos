# src/Falah.Client.CashierWinUI/Views/AuditView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9940906778e799bbf0efb38281129837298f83a8223164d54eb06031dbdbea8c`; worktree SHA256 `08ede66e41202c7c4823b988a653729671f0cf204916007aab57c873bbf8ac86`; LF-equivalent `True`.

Actual role: Read-only audit event filter/list presentation.

## Function/resource review

- `LoadCommand` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:24`: Header refresh icon has no explicit AutomationProperties.Name/content text; retry has label. Client read-only markup alone cannot prove server append-only enforcement.

- `SelectedCategory` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:31`: Category/severity2way filters distinct from event source; server filter behavior belongs VM/API. Horizontal180px combos may fit normal workspace but no breakpoint here.

- `TimestampUtc` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:76`: LocalTimestampConverter displays local timestamp; table retains action, actor, details and severity. Fixed130+170+120+110 plus detail star can squeeze detail at small widths; vertical-only scrolling lacks horizontal overflow remedy.

- `Events` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:63`: ItemsControl in ScrollViewer eagerly realizes event rows, no virtualization. BoundedAPI count must prevent excessive long-log native cost; no performance measurement.

- `Severity` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:33`: Textual Arabic label plus pill color helps non-color users. StatusToPillBrush concrete theme resolver inherits UI004; native contrast unverified.

- `HasNoEvents` at `src/Falah.Client.CashierWinUI/Views/AuditView.xaml:111`: Distinct loading/error/empty states with retry. HasEvents may preserve old rows during refresh; VM flag semantics must ensure correct presentation.

## Tests and limitations

NOT_EXECUTED. Need refresh accessible name, narrow table/text scale, long details, native row performance and filter-response races; XAML has no callable symbols.

## Recommendations (not implemented)

Name icon-only refresh and verify table adaptation/virtualization with real bounded datasets. No log or source mutation performed.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
