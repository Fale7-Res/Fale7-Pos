# docs/UNRESOLVED_OPERATION_RECOVERY.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `9da8378ae2bb8f542631a4ebc7d1934ec167ad8ec021c97a352151c401454047`; Git blob SHA256 `b751a3149f5aa9aab9321d9a422648b3669638c9e932fa8a525cde82372de5ce`. Read lines1–82.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Design/acceptance narrative for durable uncertain-operation retention and client recovery.

## reasoning

All82lines read. Opening universal statement that every money-moving command uses RetainedRequest is contradicted by in-memory cashier/Treasury paths documented UI001 and management shift UI016. Serialization-before-send is implemented by the generic helper, but save failure is explicitly swallowed, so durability is conditional. Atomic temp/move is not a tested power-loss flush guarantee. Passive recovery and per-station directories exist; UI005/017 show recovered DTO does not imply a reachable retry action. Current store catches corrupt reads and returns null without always deleting the file, unlike the broad dropped description. CallCenter per-operator keys differ from global management keys; do not infer user isolation for every request. Final nothing-outstanding assertion is invalid for the current audited source.

## relationships

- RetainedRequest.SendAsync
- FileRetainedRequestStore.Save/Load/List
- CallCenter retained checkout/print requests
- UI-001/005/016/017/020
- SEC002 credential-bearing journal hypothesis

## tests

Durability test-source scenarios exist but were not executed during this audit. Real power-cut/IO-failure/identity-switch/unknown-outcome recovery remains untested.

## recommendation

Correct future documentation scope, require explicit durable-save outcome policy, identity-scoped recovery and reachable retry context. Preserve immutable idempotency keys and never blindly replay real financial operations.

## risks

Operational overconfidence, fail-open persistence and plaintext credential-bearing DTO retention. No live journals read and no DB/financial operation executed.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
