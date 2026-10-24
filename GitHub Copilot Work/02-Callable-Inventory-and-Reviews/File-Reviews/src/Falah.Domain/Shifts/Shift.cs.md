# src/Falah.Domain/Shifts/Shift.cs

**path**: src/Falah.Domain/Shifts/Shift.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: e96a44389205732b3bb8a6c095d811bae3c117b03a1ec82fc7b9ca3036756690

**git_blob_sha256**: e96a44389205732b3bb8a6c095d811bae3c117b03a1ec82fc7b9ca3036756690

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 118]]

**functionality_and_reasoning**: Full118 lines read. Open rejects negative float, Cairo date snapshot. Reconstitution trusts historical state. BlindClose computes discrepancy and flag; Close trusts externally supplied BlindCloseResult without recomputing or nonnegative actual, handler constructs correctly. Above threshold requires nonempty authorizedBy+reason regardless actor own shifts_manage; handler uses approval name only. Permission-holding manager with no separate approval may fail despite policy authorize. No actor invariants inside aggregate.

**significant_methods**: Constructors; Open; Reconstitute; BlindCloseResult.IsBalanced; PrepareBlindClose; Close

**interactions_and_risks**: Full118 lines read. Open rejects negative float, Cairo date snapshot. Reconstitution trusts historical state. BlindClose computes discrepancy and flag; Close trusts externally supplied BlindCloseResult without recomputing or nonnegative actual, handler constructs correctly. Above threshold requires nonempty authorizedBy+reason regardless actor own shifts_manage; handler uses approval name only. Permission-holding manager with no separate approval may fail despite policy authorize. No actor invariants inside aggregate.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared CloseShiftHandler and policy, no shift writes.

**recommendations**: Return authorization actor/approver identity consistently for discrepancy and record actual granting authority; test owner/manager self-authorized close, nonprivileged approval and forged result direct domain use.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
