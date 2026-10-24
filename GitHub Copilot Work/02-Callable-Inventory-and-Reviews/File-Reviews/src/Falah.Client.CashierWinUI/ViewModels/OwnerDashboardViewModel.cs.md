# src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `10bd354942034056001cb72f047bddb9937559c3da0fd601062ef317262775d0`; worktree SHA256 `5f8e0b4a7e0a7a7b80094048531fdbf756546ecdf7c5efc3535f9475a48d65a0`; LF-equivalent `True`.

Actual role: Owner KPI, attention and trend presentation from one server DTO.

## Function/resource review

- `OwnerDashboardViewModel(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:28`: SelectedPeriod setter immediately starts LoadAsync; synchronous prefix checks reports_view and session. Navigation callback only routes to existing shell.

- `ResolveRange(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:104`: Shared Cairo clock defines inclusive periods consistently with reports.

- `LoadAsync(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:98`: Coalesces concurrent reloads, compares captured range after await and discards mismatched DTO. Sets Data null/error on failure; finally releases spinner. No cancellation/session identity generation; same-range identity change not checked.

- `Open(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:148`: Attention item null/nav null is ignored; downstream shell must permission-gate target.

- `RebuildVisibleAttention(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:159`: First three or all preserve insertion order; toggle labels/count notified. Contrary comment severity order is not sorted after later bad items are appended; critical backup/cost/printing warnings may remain below three expanded rows.

- `ToggleAttention(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:177`: Flips flag, generated hook rebuilds rows. Reset on fresh Data limits persistent expansion.

- `OnDataChanged(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:179`: Rebuilds all collections, limits low-stock to six, retains all print routes and refreshes derived displays. Clearing Data removes previous collections.

- `RebuildMetrics(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:181`: Formats server net/collected/driver/expense/cost values without recomputing business totals. Net and collected share net trend points; their headline deltas use respective server ChangeDto. Unknown food percent displays unknown.

- `Card(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:217`: No comparison percent produces none direction; nonzero delta without ratio overrides caption. Required metrics hold independent strings/point collections.

- `DescribeChange(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:247`: Null percent becomes no comparison; negative uses absolute magnitude with downward glyph. Money uses integer piastre formatter.

- `RebuildAttention(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:182`: Builds backup, discrepancy, driver, electronic, stock, exceptions, close, print, cost, payable, attendance, old-day and cost-change warnings. Later critical rows appended after earlier info; thresholds24h/72h backup and10minute print are UI policy. Missing success does not prove physical route failure.

- `RebuildSplit(` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:183`: Computes channel display shares only for positive total. Zero total uses em dash, not zero percent. Server financial totals remain authoritative.

- `UnknownCostLabel` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:201`: Distinguishes no known cost from incomplete cost; lists up to six missing names.

- `BackupLabel` at `src/Falah.Client.CashierWinUI/ViewModels/OwnerDashboardViewModel.cs:202`: Shows local-machine timestamp, reported server age, and Valid/Invalid vocabulary correctly. Null age falls back zero, which could misstate freshness if backend omits age despite timestamp.

## Tests and limitations

NOT_EXECUTED. Tests stale period result discard, permission absence, no data, null ratios, all late critical alerts with collapsed list, unknown cost and valid/invalid backup.

## Recommendations (not implemented)

Order attention explicitly by severity if comment and operator priority intended. Label collected sparkline with its actual series or provide matching collected trend.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
