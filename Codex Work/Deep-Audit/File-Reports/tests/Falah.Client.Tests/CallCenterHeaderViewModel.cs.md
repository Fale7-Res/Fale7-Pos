# tests/Falah.Client.Tests/CallCenterHeaderViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `95b2174ebf8595702eeadce90d91132a06527cc7d9127fb1e4cff43d0b0d6083`; worktree SHA256 `888f2d8d2b3c6164d6316a379cf3e53a43db20e38d23c72a04d87056344352b5`; LF-equivalent `True`.

Actual role: CallCenter headless native-header substitute.

## Function/resource review

- `HeaderViewModel` at `tests/Falah.Client.Tests/CallCenterHeaderViewModel.cs:4`: Constructor accepts optional production session but ignores it, Stop/Refresh/SetIdentity all no-op. It substitutes not only clock but also native notifications and actual restaurant identity, despite comment only native clock omitted.

- `Refresh` at `tests/Falah.Client.Tests/CallCenterHeaderViewModel.cs:8`: Noop means tests cannot assert actual header user/role/shift/lock notifications or session OperationStateChanged subscription. Stop no timer semantics; SetIdentity no configured name behavior.

## Tests and limitations

This is test infrastructure, NOT_APPLICABLE standalone domain correctness. Native header paths separately reviewed; no test run here.

## Recommendations (not implemented)

State exact substitution scope in test evidence; add native header integration rather than infer tested UX from headless assertions.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
