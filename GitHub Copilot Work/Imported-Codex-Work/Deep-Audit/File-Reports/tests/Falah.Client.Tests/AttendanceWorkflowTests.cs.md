# tests/Falah.Client.Tests/AttendanceWorkflowTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `777bc0bdcc1b6d1a81f18d8f0a2286751c72c5ebfd9184ea5a1ebef3ce67783b`; worktree SHA256 `777bc0bdcc1b6d1a81f18d8f0a2286751c72c5ebfd9184ea5a1ebef3ce67783b`; LF-equivalent `True`.

Actual role: Deterministic fake-HTTP attendance/audit/employee read/write regressions.

## Function/resource review

- `Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed` at `tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:24`: Actual VM with fake API: unknown then422then success; edits form invalid between retries, verifies exact same3JSON bodies and correct Arabic digit conversion, gate/navigation, reload newest14hours. Valid same-session write safety; no reconstructed process.

- `Date_change_during_read_discards_stale_records_and_loads_the_selected_day` at `tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:55`: TaskCompletionSource old date response completed after date change; asserts2requests/new-day only, meaningful stale-read test.

- `Confirmed_correction_survives_a_failed_followup_read` at `tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:73`: Shows action succeeds independent failed refresh; does not falsely claim no records. Invalid correction table rejects NaN/negative/missing reason without writes; permission test blocks editor for pos-only role.

- `Audit_filter_changes_while_loading_only_publish_the_latest_filter_result` at `tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:111`: Deferred first read failure, updates filters; verifies new selected query wins. Ledger tests distinguish failure/empty and ignore old employee read after next selection.

- `Handler` at `tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:12`: Fake handler delegates no CancellationToken observation; fixture URLs .invalid never fetched outside injected HTTP. Employee helper sparse JSON DTO defaults; no database or XAML.

## Tests and limitations

These tests cover real source VM decisions, not native bindings/theme/layout/financial handler atomicity. Missing recovered attendance editor target/session registration and customer race UI-007.

## Recommendations (not implemented)

Keep meaningful races and extend concrete process reconstruction; document mocked-transport nature, never present fake results as restaurant operational data.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
