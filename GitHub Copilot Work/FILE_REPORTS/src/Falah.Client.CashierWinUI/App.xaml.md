# src/Falah.Client.CashierWinUI/App.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f908879d781a6369268d82ab0db8b6e1861a94c4fa2346f32ca4d94a5bc98fb4`

Worktree SHA-256: `92d676ceb94b33eefce212eafdf93fb09f00abd9508b5be0995cd6389357743e`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Application merged style/resource and converter registration.

## Source paths and function/resource analysis

- `ResourceDictionary.MergedDictionaries` at `src/Falah.Client.CashierWinUI/App.xaml:7`: Loads Fluent resources before Colors/Typography/Controls; actual Light/Dark dictionaries live in merged Colors, relevant to imperative ThemeResources.Get UI-004.

- `BoolToVisibleConverter` at `src/Falah.Client.CashierWinUI/App.xaml:18`: Registers converter objects process-wide; no stateful view logic in this XAML. NavKeyToGlyphConverter not registered but ShellView instantiates directly, not missing resource defect.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Semantic XAML parsing needed separately; no visual state snapshot tests performed.

## Recommendations (not implemented)

Validate every StaticResource reference and all theme keys; avoid source comments that expose demo PINs (Typography comment) in publicly shared deployment materials.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
