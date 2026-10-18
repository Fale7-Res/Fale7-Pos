# src/Falah.Client.CallCenterWinUI/Converters/Converters.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `4bd3b0ccf0d46539b5d8b171e5f0f28ce26efba090564106f7348f7268356860`; worktree SHA256 `926d37d8ddf52c6518a64537dfbeacfa307f37578c3493a486bf67a7756878f1`; LF-equivalent `True`.

Actual role: CallCenter shared display converters with exact forward/reverse contracts.

## Function/resource review

- `OrderStatusLabelConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:11`: Lowercases six known order states and preserves unknown source text; null/nonstring becomes empty. Localizes display only, never writes wire state.

- `BoolToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:28`: Only boxed true becomes Visible; false/null/wrong type collapses. Useful positive guard, not permission enforcement.

- `InverseBoolToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:37`: Only true collapses; null/false/wrong type visible, so unset loading flag may reveal content. This is exact inverse of positive converter.

- `NullToCollapsedConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:48`: Null and empty string collapse, whitespace and all other objects visible. Whitespace errors can reserve empty-looking panel; no trim.

- `EmptyStringToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:60`: Null/empty visible for placeholder; whitespace nonempty collapses placeholder. Counterpart of null display converter.

- `ShiftDotBrushConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:71`: True maps shared emerald brush, otherwise slate. Static brushes ignore theme changes; textual ShiftLabel must carry non-color meaning.

- `BoolToBrushConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:90`: Required string parameter split two keys, true selects first; direct Application.Resources lookup. Invalid arity/key throws and concrete brush does not auto-reevaluate theme. CallCenter uses static light palette unlike cashier ThemeResources helper.

- `StringEqualsToBrushConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:106`: Ordinal value versus first parameter segment picks second/third app brush key. No parameter length/type validation; wrong case unselected. No UI mutation beyond returned brush.

- `StringEqualsToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:121`: Ordinal string equality yields visibility. Two nonstrings cast null and compare equal if parameter null; actual callers use explicit strings.

- `CountGreaterThanOneToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:131`: Requires non-generic ICollection Count>1; DTO variants exposing collection satisfy, arbitrary IEnumerable does not. Bound collection mutation will not itself reevaluate unless binding notified.

- `PiastresToDisplayConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:143`: Accepts long/int using decimal money format; null em dash, wrong numeric type0.00 can hide schema mismatch. No arithmetic authority, display only.

- `InverseBoolConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:160`: Both forward and reverse return value is not true. Symmetric bool inversion for well-typed inputs; null/wrongtype becomes true. Reverse is implemented unlike other converters.

- `EmptyCollectionToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:170`: Null or ICollection Count0 visible; wrong type hidden. Changes require source notification; Count-based converter is safer for observable collection updates.

- `NonEmptyCollectionToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:181`: ICollection Count>0 visible, null/wrong type collapsed. Complement only for supported collection contract; not for arbitrary IEnumerable.

- `CairoBusinessDateConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:196`: Extracts supplied calendar date from DateOnly/DateTime/DateTimeOffset or culture-parsed string; does not convert instant to Cairo despite name. Correct when caller supplies business-date carrier; arbitraryUTCnear-midnight can display wrong Cairo day.

- `TimeOfDayConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:223`: DateTimeOffset converted machine local and ar-EG time; DateTime used as supplied. Not pinned Cairo, so remote station displays its clock zone. Unsupported em dash.

- `NavKeyToGlyphConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:239`: Known module-key dictionary to Fluent private-use glyph; unknown/nonstring generic fallback. Textual navigation label needed for accessibility, no new screen construction.

- `StatusToPillBrushConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:277`: Lowercase substring bucket tests success before failure/warning. Future unpaid/inactive could match paid/active; current emitted vocabulary not verified for those strings, candidate only. Direct app resources, neutral fallback.

- `ImageUriToSourceConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:308`: Absolute Uri key cached in static locked dictionary, BitmapImage decodewidth320. Relative URI AbsoluteUri throws; no eviction/download-failure retry invalidation. Version URLs accumulate process lifetime. No physical printing or database call.

- `ZeroCountToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:335`: Only int0 visible; Count-property binding receives changes unlike collection object. Negative/wrongtype collapses; actual Count cannot negative.

- `CountToVisibleConverter` at `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs:335`: Only positive int visible. Zero/null/wrongtype collapsed, matching saved-address count UI. No side effects or enumeration.

## Tests and limitations

SOURCE_REVIEW_ONLY. All43 callable stable spans individually reconciled, including21 forward/reverse pairs and Format. No native runtime tests; no blanket migration from legacy headings.

## Recommendations (not implemented)

Test required parameter arity, display types, absolute image URLs, bounded cache and timestamp semantics; keep unknown state neutral.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
