# src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: e531e400a2194017e664c848ce8a13679ce064ea1b40d1c07f392a210db1571b

**git_blob_sha256**: e531e400a2194017e664c848ce8a13679ce064ea1b40d1c07f392a210db1571b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 80]]

**functionality_and_reasoning**: Full80 lines read. Employee rates/lifecycle free values. Ledger debit/credit described positive but no constraints, optional financial reference fields not populated consistently across flows. PayrollRunId never set by EmployeeLedgerPoster which writes ReferenceIdrun. Attendance time string vs Cairodate cannot represent overnight full instants. LastEventId only latest event not replay inbox. No rowversion.

**significant_methods**: EmployeeEntity; EmployeeLedgerEntryEntity; AttendanceRecordEntity properties

**interactions_and_risks**: Full80 lines read. Employee rates/lifecycle free values. Ledger debit/credit described positive but no constraints, optional financial reference fields not populated consistently across flows. PayrollRunId never set by EmployeeLedgerPoster which writes ReferenceIdrun. Attendance time string vs Cairodate cannot represent overnight full instants. LastEventId only latest event not replay inbox. No rowversion.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared all employee writer implementations.

**recommendations**: Persist complete time instants and immutable source event ID, coherent payroll-reference FK and sign constraints after approved migrations.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
