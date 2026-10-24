# docs/LEGACY_PARITY_MAP.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `9ccc2896d0118c50e4b8071998108fd42fe52bc4f424a50551666e18a235bf94`; Git blob SHA256 `9ccc2896d0118c50e4b8071998108fd42fe52bc4f424a50551666e18a235bf94`. Read lines1–193.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Arabic historical legacy behavior/design-token and WPF migration reference, including deferred workflows and financial/printing invariants.

## reasoning

All193lines read in2chunks. Dated WPF/SQLite, plaintext PIN schema, missing CallCenter/CRUD/printing and NOT_STARTED labels are historical and contradicted by later current source; no external E-drive legacy code was accessed or claimed verified. Distinguishes functionally matched, visually matched, tested and approved appropriately. ReceiptModal paper preview and multiple receipt types describe legacy requirements, not current WinUI implementation evidence. Read-only order fetch does not prove safe print-job consumption or physical delivery. Whole-order pending cancellation without stock reversal is explicitly described legacy behavior, but split cash cancellation remains a concrete financial concern(FIN002). Manager approval independent of session is design intent contradicted by shared LoginByPin mutation(UI020). Expense non-drawer permission is permission-based, not strictly owner-role-only despite parenthetical wording. Shared dialog/state/unknown-cost principles are requirements, not current correctness proof.

## relationships

- Current WinUI and retained WPF source
- UI020 manager approval shared session
- FIN002 mixed-payment cancellation
- FLOW001 pickup requirements and PRT preview findings
- IMPLEMENTATION_MATRIX later superseding history

## tests

No legacy browser/window launched, login performed or source outside repository read. Historical DOM geometry and physical-paper assertions were not reproduced. No current native screenshot/visual acceptance inferred.

## recommendation

Use this map for behavior provenance with dates and explicit supersession links. Separate current pickup/payment lifecycle and safe preview acceptance criteria from browser window.print behavior, preserving server authority and financial idempotency.

## risks

Raw baseline contains a historical credential example and private paths; values are not copied. Obsolete TODOs may trigger unnecessary redevelopment if treated as current evidence. Legacy policy alone cannot justify a financial integrity defect.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
