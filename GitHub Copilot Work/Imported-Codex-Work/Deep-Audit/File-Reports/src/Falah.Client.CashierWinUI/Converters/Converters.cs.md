# src/Falah.Client.CashierWinUI/Converters/Converters.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `a064f7ba96e3eb7615d98a0b8be7582d7f6b01e5fd300a81ccc216a47dffc78c`; worktree SHA256 `90b9e4b4ccd5d737a239f66f0db5117c9c23c81e339ed2eb614c4191fb88d5e5`; LF-equivalent `True`.

Actual role: Shared one-way WinUI display, visibility, label, brush and image converters.

## Function/resource review

- `BoolToVisibleConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:8`: True alone visible; inverse treats null/false as visible. Null/empty string visibility and empty string inverse do not trim whitespace. All corresponding ConvertBack throw; bindings must remain one-way.

- `NullablePiastresToDisplayConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:41`: Long formats piastres using CashInput; absent/wrong numeric type remains unvalued, preserving missing cost. MoneyOrUnknown differs label only; PiastresToDisplay accepts long/int/null but wrong type returns0.00, potentially masking schema mismatch.

- `TreasuryMovementTypeLabelConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:54`: Maps immutable snake_case ledger types to Arabic without rewriting IDs; unknown passes through. PaymentMethod lowercases; user/employee/stock labels use exact server vocabulary and unknown fallbacks.

- `PrintRouteLabelConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:184`: All six routes including DeliveryOrder/DriverDispatch map. SignificantAction delegates by category; delivery assignment/reassignment/ready labels separate. Treasury null account explicitly outside system.

- `MilliUnitsToDisplayConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:249`: Only long supported, no unit conversion ambiguity; positive amount accepts long>0 only. Count positive accepts int only. Strict DTO types should be tested.

- `DeliveryStatusLabelConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:274`: PascalCase delivery statuses separate from case-normalized order statuses. Order type handles historical dine_in/dinein as pickup. General statuses map payroll/business day/backup Valid Invalid and audit severity.

- `BoolToBrushConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:416`: Splits required parameter without validation and resolves concrete theme Brush; string/int variants expect three pieces. Bad XAML parameters throw; ThemeResources/theme refresh risk UI004. Label bool split likewise unguarded.

- `ShiftDotBrushConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:397`: Static emerald/slate brushes independent of theme; require accompanying textual status for non-color access. Severity and delta resolve theme brushes; unknown severity defaults success, so unrecognized severity can appear green.

- `PercentOrUnknownConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:451`: Double formatted invariant; no finite check so malformed NaN/infinity would print. Fraction clamps positive shares to150px max with4px minimum, zero/negative/NaN to0; presentation only.

- `LocalTimestampConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:485`: Timestamp converts machine timezone; TimeOfDay also machine local with ar-EG culture. CairoBusinessDate extracts calendar DateTime directly without timezone conversion; correct only when input already business-date carrier, not arbitrary instant.

- `FileNameConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:475`: Reduces nonempty path with Path.GetFileName; null empty. NullToBool excludes null/empty not whitespace. NullToText trims whitespace semantics by IsNullOrWhiteSpace.

- `StringEqualsToVisibleConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:598`: Ordinal equality only; count>one expects ICollection. Empty/Nonempty collection depend on binding re-evaluation; ZeroCount explicitly binds Count int to avoid unchanged collection object bug.

- `InverseBoolConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:637`: Both directions value is not true; malformed/null value becomes true. Binding bool contract should prevent unintended coercion.

- `NavKeyToGlyphConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:716`: Known module keys map Segoe Fluent private-use glyphs; unknown generic fallback. Accessible textual navigation label still required.

- `AttendanceStatusLabelConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:756`: Lowercase attendance vocabulary maps Arabic; unknown source value passed through. StatusToPillBrush uses substring matching and success tests first: hypothetical unpaid/inactive contain paid/active and look successful; no current emitted contract verified for those strings, so candidate only.

- `ImageUriToSourceConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:799`: Process-static locked dictionary caches320px BitmapImage for every absolute URI, no eviction/error invalidation. Versioned URLs prevent stale photo reuse but repeated image versions accumulate until exit. Relative Uri.AbsoluteUri throws; caller currently expected absolute. NOT measured memory.

- `ZeroCountToVisibleConverter` at `src/Falah.Client.CashierWinUI/Converters/Converters.cs:844`: Only int0 visible; correctly bind Count property rather than collection to receive changes. Every ConvertBack unsupported except inverse bool, which is an intentional symmetric conversion.

## Tests and limitations

NOT_EXECUTED. Table-driven tests should cover every vocabulary, wrong/null DTO type, parameter arity, theme switch, relative URI, cache version churn, current-zone versus Cairo date and Count mutation. No generic Convert heading used to claim every method individually mapped.

## Recommendations (not implemented)

Keep strict schema types explicit, resource lookup dynamic per theme, bounded image cache and fail-safe neutral unknown status. No runtime visual contrast proof claimed.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
