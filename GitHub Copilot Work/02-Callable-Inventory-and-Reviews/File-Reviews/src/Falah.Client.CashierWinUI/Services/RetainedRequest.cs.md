# src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `7cf2985b1e7764377ea5c972efae4ae418790bab38ef8aeebfd5198f86b5a68b`

Worktree SHA-256: `7cf2985b1e7764377ea5c972efae4ae418790bab38ef8aeebfd5198f86b5a68b`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Generic durable request retention helper.

## Source paths and function/resource analysis

- `RetainedRequest` at `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs:11`: Owner key optionally enables ambient store and restores typed payload. WasRecovered set only if cast succeeds; wrong typed object appears absent, though disk may remain.

- `SendAsync` at `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs:34`: Retains first immutable request, saves before sending; first definite failure clears, ambiguous then later rejection remains. Success nonnull clears; generic primitive T result nullability differs from reference values. Send exceptions outside ApiClient allowed set Value but not normalize error.

- `FailureMessage` at `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs:53`: Unknown outstanding state takes precedence over newer rejection, appropriately avoids pretending earlier commit impossible. No cancellation token parameter beyond caller delegate.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

RetainedRequestDurabilityTests covers helper process reconstruction, ambiguous retry and basic error storage. Does not prove each concrete screen adopts helper or can reach retry after recovery.

## Recommendations (not implemented)

Require trustworthy persistence result for money commands or clearly surface degraded recovery; add concrete screen reconstruction tests; namespace owner keys by operator where context matters.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
