# src/Falah.Client.Cashier/AssemblyInfo.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `d41e48d2bce7f9eda84eef27c4008920a5639dd3f56baeff39273d58381575fe`

Worktree SHA-256: `3fac2af92385f676e9a941befd23fe38f0a5fa6167d4a3a88f07e730c5595ca3`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WPF theme resource lookup metadata.

## Source paths and function/resource analysis

- `ThemeInfo` at `src/Falah.Client.Cashier/AssemblyInfo.cs:3`: Declares no external theme-specific dictionaries and SourceAssembly generic resources. No code functions, mutable runtime state, native API calls or secret material.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NOT_APPLICABLE functions and business tests; build/resource compilation checks relevant.

## Recommendations (not implemented)

No finding in this limited metadata declaration.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
