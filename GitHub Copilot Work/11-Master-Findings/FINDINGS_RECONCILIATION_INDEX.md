# Codex Findings to Copilot Review Outcomes — Reconciliation Index

This document tracks every original Codex finding, its source references, prior status, and independent Copilot audit outcome.

| ID | Severity | Title | Codex Prior Status | Copilot Disposition | Files Verified |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **FIN-001** | `HIGH` | Mixed cash/electronic checkout cannot confirm its electronic leg | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 6 files |
| **FIN-002** | `HIGH` | Cancelling pending mixed electronic payment hides collected cash without reversal | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 4 files |
| **FIN-003** | `HIGH` | Refund can debit money for an order that was never collected | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 4 files |
| **FIN-004** | `HIGH` | Historical cash refund omitted from current-shift expected cash | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 4 files |
| **FIN-005** | `HIGH` | Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 8 files |
| **FIN-006** | `HIGH` | Generic payroll transition marks run Paid without paying treasury or employee ledger | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 3 files |
| **FIN-007** | `MEDIUM` | Backup filenames collide within a second and WITH FORMAT can overwrite prior backup | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **REQUIRES_RUNTIME_VERIFICATION** | 4 files |
| **FIN-008** | `HIGH` | Restore pending-journal check does not exclude active or newly accepted financial work | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **REQUIRES_RUNTIME_VERIFICATION** | 6 files |
| **FIN-009** | `MEDIUM` | Concurrent different open-day requests create multiple active business days | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **REQUIRES_RUNTIME_VERIFICATION** | 4 files |
| **FIN-010** | `MEDIUM` | Collected sales report drops immediate money from pending mixed payments | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **REQUIRES_RUNTIME_VERIFICATION** | 3 files |
| **FIN-011** | `HIGH` | Shift opening and purchase receipt check protected account balance before their transactions | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 5 files |
| **FIN-012** | `HIGH` | Attendance correction credits do not enter calculated payroll | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 2 files |
| **FIN-013** | `MEDIUM` | Owner dashboard adds outgoing stock instead of subtracting it | `CONFIRMED_STATIC_INDEPENDENT_REVIEW` | **INDEPENDENTLY_CORROBORATED** | 1 files |
| **FIN-014** | `HIGH` | Restore success followed by MULTI_USER failure is classified as database not replaced | `SOURCE_CONFIRMED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **REQUIRES_RUNTIME_VERIFICATION** | 3 files |
| **FIN-015** | `HIGH` | Checkout can commit after its previously open shift has completed handover | `SOURCE_CONFIRMED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **INDEPENDENTLY_CORROBORATED** | 24 files |
| **FIN-016** | `HIGH` | Unvalidated signed delivery fee can bypass intended discount and comp authorization | `CONFIRMED_SOURCE_AND_PURE_ARITHMETIC_REPRO` | **INDEPENDENTLY_CORROBORATED** | 6 files |
| **FIN-017** | `MEDIUM` | Financial approval provenance can retain unvalidated shift presenter or discard verified driver variance approver | `SOURCE_CORROBORATED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **REQUIRES_OWNER_BUSINESS_DECISION** | 19 files |
| **FIN-018** | `MEDIUM` | Negative receipt quantity and unit cost can post stock reduction with supplier debt | `SOURCE_CORROBORATED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **REQUIRES_RUNTIME_VERIFICATION** | 9 files |
| **FIN-019** | `HIGH` | Pending mixed delivery includes already collected payment leg in driver custody | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **INDEPENDENTLY_CORROBORATED** | 24 files |
| **FIN-020** | `MEDIUM` | First stock receipt can race unit-change admission and reinterpret movement history | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 8 files |
| **FIN-021** | `MEDIUM` | Accepted numeric Inclusive tax mode is persisted raw and consumed as Exclusive | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_OWNER_BUSINESS_DECISION** | 7 files |
| **FIN-022** | `MEDIUM` | Accepted case-insensitive compensation type is stored raw but wage/payroll consumers require canonical names | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_OWNER_BUSINESS_DECISION** | 10 files |
| **FIN-023** | `MEDIUM` | Attendance checkout reconstructs check-in on checkout UTC date and can omit overtime | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 5 files |
| **FIN-024** | `MEDIUM` | Manual daily-wage and overtime commands admit negative earning credits | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING_WITH_IMPACT_LIMITS` | **INDEPENDENTLY_CORROBORATED** | 10 files |
| **FIN-025** | `MEDIUM` | Manual ledger accepts undefined numeric types excluded from named financial categories | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING_WITH_IMPACT_LIMITS` | **INDEPENDENTLY_CORROBORATED** | 9 files |
| **FIN-026** | `MEDIUM` | Distinct employee credit requests can persist and display stale running-balance snapshots | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING_WITH_IMPACT_LIMITS` | **INDEPENDENTLY_CORROBORATED** | 7 files |
| **FIN-027** | `MEDIUM` | Frozen business-day financial snapshots omit the corresponding live-report permission gate | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING_WITH_ACCESS_PRECONDITIONS` | **INDEPENDENTLY_CORROBORATED** | 12 files |
| **FIN-028** | `MEDIUM` | Spent manager approval prevents acknowledgement replay of committed overtime and extra-shift credits | `SOURCE_CONFIRMED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **REQUIRES_RUNTIME_VERIFICATION** | 12 files |
| **FIN-029** | `MEDIUM` | Owner dashboard stock projection ignores outbound movement direction | `SOURCE_CONFIRMED_WITH_INDEPENDENT_STATIC_CHALLENGE` | **REQUIRES_RUNTIME_VERIFICATION** | 7 files |
| **FIN-CAND-CASHCOUNT-001** | `MEDIUM` | Unvalidated denomination and unchecked arithmetic can corrupt finalized count values | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 58 files |
| **FIN-CAND-CLOSE-SNAPSHOT-REVISION-001** | `MEDIUM` | Close snapshot revision and authoritative-row admission lack an explicit shared invariant | `QUALIFIED_SOURCE_RISK_DUPLICATE_COMMIT_NOT_CONFIRMED` | **REQUIRES_OWNER_BUSINESS_DECISION** | 3 files |
| **FIN-CAND-CLOSEDAY-ADMISSION-001** | `HIGH` | Previously admitted treasury deposit can post after authoritative business-day close | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 5 files |
| **FIN-CAND-CORRECTION-BASIS-001** | `MEDIUM` | Attendance corrections revalue old effects at current rates instead of posted monetary basis | `POLICY_QUALIFIED_SOURCE_RECONCILIATION_RISK` | **CORROBORATED_WITH_QUALIFICATIONS** | 6 files |
| **FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001** | `HIGH` | Standalone employee settlements remain payable through frozen period payroll | `SOURCE_CONFIRMED_CROSS_FLOW_RISK_POLICY_QUESTION_NOT_CONFIRMED_DUPLICATE_PAY_DEFECT` | **REQUIRES_RUNTIME_VERIFICATION** | 12 files |
| **FIN-CAND-PACKAGING-COVERAGE-001** | `MEDIUM` | Unknown once-per-order packaging cost does not invalidate aggregate profit completeness | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_OWNER_BUSINESS_DECISION** | 56 files |
| **FIN-CAND-PAYROLL-001** | `HIGH` | Concurrent pay/cancel can overwrite payroll terminal status | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 5 files |
| **FIN-CAND-PREPGRAPH-001** | `MEDIUM` | Concurrent opposite prep edges can evade pretransaction cycle admission | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 6 files |
| **FIN-CAND-REFUND-REPLAY-001** | `LOW` | Overlapping full-refund replay can reject after same-key sibling committed | `INDEPENDENT_STATIC_CORROBORATION_ACKNOWLEDGEMENT_ONLY` | **CORROBORATED_WITH_QUALIFICATIONS** | 18 files |
| **FIN-CAND-REPORT-RANGE-001** | `MEDIUM` | Reversed report endpoints normalize sales but independently filter cost with contradictory date bounds | `INDEPENDENTLY_CORROBORATED_STATIC_FINDING` | **REQUIRES_RUNTIME_VERIFICATION** | 57 files |
| **FLOW-001** | `HIGH_REQUIREMENT_GAP` | Requested two-stage pickup preparation and final payment lifecycle is absent | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 6 files |
| **FLOW-002** | `MEDIUM` | Held electronic preparation is not automatically released on confirmation | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 5 files |
| **PRT-001** | `HIGH` | Two machines can submit the same pending kitchen job | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 6 files |
| **PRT-002** | `MEDIUM` | Succeeded and print health collapse transport acceptance into physical completion | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 7 files |
| **PRT-003** | `MEDIUM` | Blocking spooler submission ignores cancellation and completion failures | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 4 files |
| **PRT-004** | `MEDIUM` | Agent never authenticates again after session loss | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 3 files |
| **PRT-005** | `HIGH` | Printer verification tool can delete preexisting restaurant tickets | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 3 files |
| **PRT-006** | `LOW` | Saved printer width and receipt copy count do not control inspected print pipeline | `UNKNOWN` | **INDEPENDENTLY_CORROBORATED** | 5 files |
| **SEC-001** | `HIGH` | JSON actor casing mismatch permits body-selected authorization identity | `PARTIAL_AUDIT_VALIDATED_FINDING` | **INDEPENDENTLY_CORROBORATED** | 4 files |
| **SEC-002** | `MEDIUM` | PIN-bearing user-management requests are durably stored as unencrypted JSON | `SOURCE_CONFIRMED_BEHAVIOR_WITH_INDEPENDENT_STATIC_CHALLENGE` | **INDEPENDENTLY_CORROBORATED** | 5 files |
| **SEC-003** | `HIGH` | Authenticated employee reads expose complete sensitive profiles without employee permission or self-access restriction | `SOURCE_CONFIRMED_INDEPENDENT_STATIC_CHALLENGE; DEPLOYMENT_EXPOSURE_UNVERIFIED` | **INDEPENDENTLY_CORROBORATED** | 5 files |
| **UI-001** | `HIGH` | Primary cashier and treasury money requests lose original retry identity on process loss | `SOURCE_CONFIRMED_RECOVERY_GAP` | **CORROBORATED_WITH_QUALIFICATIONS** | 13 files |
| **UI-002** | `MEDIUM` | Unselected expense and payment buttons combine literal white with dark-theme pale text | `SOURCE_CONFIRMED_THEME_PAIR_DEFECT` | **CORROBORATED_WITH_QUALIFICATIONS** | 4 files |
| **UI-003** | `HIGH` | Requested pickup kitchen-before-payment stage is absent from active client workflow | `SOURCE_CONFIRMED_CLIENT_WORKFLOW_GAP` | **CORROBORATED_WITH_QUALIFICATIONS** | 3 files |
| **UI-004** | `MEDIUM` | Imperative/converter brush results do not track live per-window theme reliably | `SOURCE_CONFIRMED_WIRING_RISK_RUNTIME_UNVERIFIED` | **REQUIRES_RUNTIME_VERIFICATION** | 4 files |
| **UI-005** | `HIGH` | Recovered electronic confirmation/cancellation has no reachable cashier retry context | `SOURCE_CONFIRMED_RECOVERY_DEAD_END` | **CORROBORATED_WITH_QUALIFICATIONS** | 4 files |
| **UI-006** | `MEDIUM` | A new pickup inherits previous order optional collection schedule | `SOURCE_CONFIRMED_STATE_LIFECYCLE_DEFECT` | **CORROBORATED_WITH_QUALIFICATIONS** | 4 files |
| **UI-007** | `HIGH` | Late CallCenter customer lookup can overwrite a different phone/new order | `SOURCE_CONFIRMED_ASYNC_RACE` | **CORROBORATED_WITH_QUALIFICATIONS** | 3 files |
| **UI-009** | `MEDIUM` | Printer status UI exposes only four of six implemented routes | `SOURCE_CONFIRMED_UI_ROUTE_OMISSION_BACKEND_CROSSCHECK_PENDING` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-010** | `MEDIUM` | Workspace shortcut guard omits most open operational modals | `SOURCE_CONFIRMED_GUARD_OMISSION_NATIVE_TRIGGER_UNVERIFIED` | **CORROBORATED_WITH_QUALIFICATIONS** | 2 files |
| **UI-011** | `MEDIUM` | Discarded definitively rejected CallCenter reprint survives in journal | `SOURCE_CONFIRMED_JOURNAL_CLEAR_OMISSION` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-012** | `HIGH` | CallCenter customer-copy reprint requests kitchen preparation routes again | `SOURCE_CONFIRMED_CLIENT_FLAG_BACKEND_PEER_CORROBORATED` | **CORROBORATED_WITH_QUALIFICATIONS** | 3 files |
| **UI-013** | `MEDIUM` | Legacy WPF change display parses EGP tender as integer piastres | `SOURCE_CONFIRMED_LEGACY_CLIENT_DEFECT` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-014** | `HIGH` | Successful restore is reported incomplete because UI expects Completed instead of Succeeded | `SOURCE_CONFIRMED_CROSS_LAYER_STATUS_MISMATCH` | **CORROBORATED_WITH_QUALIFICATIONS** | 2 files |
| **UI-015** | `MEDIUM` | CallCenter lock/unlock retains old order-panel visual trees through cart subscription | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 3 files |
| **UI-016** | `HIGH` | Management shift actions abandon unknown outcomes and generate a new request identity per retry | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 3 files |
| **UI-017** | `HIGH` | Recovered payroll transition or payment cannot reach its retry command | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-018** | `HIGH` | Late recipe-detail response can replace another recipe selection before editing | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-019** | `MEDIUM` | Recipe yield conversion silently truncates fractional pieces-per-base-unit | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 1 files |
| **UI-020** | `HIGH` | PIN-change retry proof replaces shared bearer despite preserving visible user identity | `SECURITY_COORDINATOR_CHALLENGE_REQUIRED` | **CORROBORATED_WITH_QUALIFICATIONS** | 2 files |
| **UI-021** | `HIGH` | Late product-image completion changes save target beneath another product form | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 2 files |
| **UI-022** | `MEDIUM` | Recipe ingredient picker truncates to60 choices without exposing its search input | `SOURCE_CONFIRMED_STATIC_REASONING` | **CORROBORATED_WITH_QUALIFICATIONS** | 2 files |
| **FIN-CAND-BUSINESS-DAY-OPEN-RACE-001** | `MEDIUM` | Distinct concurrent business-day opens can pass pre-insert active-state validation | `INDEPENDENTLY_CORROBORATED_STATIC_CONCURRENCY_CANDIDATE` | **REQUIRES_RUNTIME_VERIFICATION** | 6 files |
| **FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001** | `LOW` | SafeDrop may attribute a later drawer movement to a closed shift and change its reprinted breakdown | `REQUIREMENTS_DEPENDENT_RECONCILIATION_ATTRIBUTION_RISK` | **CORROBORATED_WITH_QUALIFICATIONS** | 7 files |
| **FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001** | `LOW` | Legacy WPF refund submit does not preserve the same idempotency key after an ambiguous outcome | `OPEN_CONDITIONAL; deployment and owner policy unknown` | **CORROBORATED_WITH_QUALIFICATIONS** | 7 files |
| **FIN-CAND-CANCELLED-LOOKUP-PAYMENT-LABEL-001** | `LOW` | Call Center labels a cancelled unpaid electronic order as collectible on delivery | `SOURCE_CONFIRMED_PRESENTATION_MISMATCH; STATIC_FINANCIAL_SPECIALIST_CHALLENGE_SAVED; NO_LEDGER_EFFECT` | **REQUIRES_RUNTIME_VERIFICATION** | 8 files |
| **FIN-CAND-NEGATIVE-VARIANT-PRICE-001** | `MEDIUM` | Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed | `SOURCE_CORROBORATED_CANDIDATE_PENDING_FINANCIAL_SPECIALIST_AND_OWNER_PRICE_POLICY` | **REQUIRES_RUNTIME_VERIFICATION** | 12 files |
| **FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001** | `MEDIUM` | Delivery report field labeled expected unsettled custody aggregates collected amount instead | `INDEPENDENTLY_CORROBORATED_STATIC_METRIC_MISMATCH_PENDING_OWNER_FIELD_CONTRACT` | **REQUIRES_RUNTIME_VERIFICATION** | 11 files |