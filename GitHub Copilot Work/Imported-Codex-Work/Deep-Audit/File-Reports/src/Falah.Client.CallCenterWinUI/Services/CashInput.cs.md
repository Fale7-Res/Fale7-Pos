# src/Falah.Client.CallCenterWinUI/Services/CashInput.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `1cdf784cc9e89f6a7bd3611e53905258ef0dd748a473df6ae19548c2c3579f5d`; worktree SHA256 `1cdf784cc9e89f6a7bd3611e53905258ef0dd748a473df6ae19548c2c3579f5d`; LF-equivalent `True`.

Actual role: Localized strict EGP to integer piastre parser shared behavior copy.

## Function/resource review

- `TryParse` at `src/Falah.Client.CallCenterWinUI/Services/CashInput.cs:7`: Arabic/Persian digits+Arabic decimal separator normalized to invariant; decimal point only, nonnegative, <=long.Max/100, effective<=2decimals; sets out0before parsing. Avoids double rounding and exponent/negative/group separator ambiguity.

- `Format` at `src/Falah.Client.CallCenterWinUI/Services/CashInput.cs:19`: decimal long/100m to two invariant decimals, safe display precision. Exact source copy of active cashier parser aside from namespace, potential future drift if maintained independently.

## Tests and limitations

Missing explicitly asserted all-boundary parser parity between station copies. No live input or money writes.

## Recommendations (not implemented)

Retain parse parity tests and optional shared pure utility only in authorized refactor.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
