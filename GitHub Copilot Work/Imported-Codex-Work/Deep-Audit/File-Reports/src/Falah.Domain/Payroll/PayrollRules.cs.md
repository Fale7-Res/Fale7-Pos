# src/Falah.Domain/Payroll/PayrollRules.cs

**path**: src/Falah.Domain/Payroll/PayrollRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86

**git_blob_sha256**: 4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 126]]

**functionality_and_reasoning**: Transition map allows Approved->Paid and generic handler misuses it. OutstandingAdvance expects lowercase types while actual rows PascalCase; callers not yet traced. Lines ignore standalone Settlement/Withdrawal; duplicate payout possibility requires coordinated ledger trace. Hours/rates truncate and double multiplier rounding deserve explicit payroll rules.

**significant_methods**: AssertTransition; NormalHourlyRate; CalculateLatePenaltyHours; CalculateLateDeduction; CalculateOvertimeAmount; OutstandingAdvance; CalculatePayrollLines; PayrollReport

**interactions_and_risks**: Transition map allows Approved->Paid and generic handler misuses it. OutstandingAdvance expects lowercase types while actual rows PascalCase; callers not yet traced. Lines ignore standalone Settlement/Withdrawal; duplicate payout possibility requires coordinated ledger trace. Hours/rates truncate and double multiplier rounding deserve explicit payroll rules.

**findings**: ["FIN-006"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
