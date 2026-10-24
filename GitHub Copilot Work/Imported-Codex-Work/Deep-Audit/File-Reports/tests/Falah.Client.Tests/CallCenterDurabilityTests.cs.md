# tests/Falah.Client.Tests/CallCenterDurabilityTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `732cb87fecf85c546233212658a667d360854dd8d949e02700f79465a34a8a0a`; worktree SHA256 `468af2e5e966c7e56d03aa3b186d0927cce58334d81e051c06ff1dadfbccd612`; LF-equivalent `True`.

Actual role: CallCenter station JSON retention contract tests.

## Function/resource review

- `A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key` at `tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:59`: Directly Save/Load DTO to JSON and compares serialized representation/key. Does NOT instantiate actual CallCenter VM, send network, terminate process or prove recovery retry button reachable.

- `A_resolved_checkout_leaves_nothing_behind` at `tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:77`: Manually clears store and asserts no record, useful primitive contract not proof SendCheckout clears all branches.

- `Two_operators_never_recover_each_others_unresolved_checkout` at `tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:87`: Distinct literal owner keys isolate load; production CheckoutOwnerKey separately inspected and actually includes OwnerUserId.

- `A_print_request_left_in_flight_is_recovered_separately_from_the_checkout` at `tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:96`: Two owner filenames recover both requests independently; request constructed with default IncludeKitchenRoutes true, no kitchen suppression assertion UI-012.

- `The_call_center_store_is_scoped_to_its_own_station` at `tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:109`: Compares DirectoryFor paths to cashier. Constructor/Dispose replace static ambient store and reset null; concurrent unrelated tests may share ambient state unless suite collection isolation, not established.

## Tests and limitations

Read-only review; tests NOT_EXECUTED by this reviewer. No actual power cut/process test or VM recovered print-clear test UI-011.

## Recommendations (not implemented)

Add concrete VM recovery/network branch tests and isolate ambient mutable store across parallel tests.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
