from ui_incremental import *
record(T+'CallCenterDurabilityTests.cs','CallCenter station JSON retention contract tests.',[
('A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key','Directly Save/Load DTO to JSON and compares serialized representation/key. Does NOT instantiate actual CallCenter VM, send network, terminate process or prove recovery retry button reachable.'),
('A_resolved_checkout_leaves_nothing_behind','Manually clears store and asserts no record, useful primitive contract not proof SendCheckout clears all branches.'),
('Two_operators_never_recover_each_others_unresolved_checkout','Distinct literal owner keys isolate load; production CheckoutOwnerKey separately inspected and actually includes OwnerUserId.'),
('A_print_request_left_in_flight_is_recovered_separately_from_the_checkout','Two owner filenames recover both requests independently; request constructed with default IncludeKitchenRoutes true, no kitchen suppression assertion UI-012.'),
('The_call_center_store_is_scoped_to_its_own_station','Compares DirectoryFor paths to cashier. Constructor/Dispose replace static ambient store and reset null; concurrent unrelated tests may share ambient state unless suite collection isolation, not established.')],
'Read-only review; tests NOT_EXECUTED by this reviewer. No actual power cut/process test or VM recovered print-clear test UI-011.',
'Add concrete VM recovery/network branch tests and isolate ambient mutable store across parallel tests.')
record(T+'AttendanceWorkflowTests.cs','Deterministic fake-HTTP attendance/audit/employee read/write regressions.',[
('Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed','Actual VM with fake API: unknown then422then success; edits form invalid between retries, verifies exact same3JSON bodies and correct Arabic digit conversion, gate/navigation, reload newest14hours. Valid same-session write safety; no reconstructed process.'),
('Date_change_during_read_discards_stale_records_and_loads_the_selected_day','TaskCompletionSource old date response completed after date change; asserts2requests/new-day only, meaningful stale-read test.'),
('Confirmed_correction_survives_a_failed_followup_read','Shows action succeeds independent failed refresh; does not falsely claim no records. Invalid correction table rejects NaN/negative/missing reason without writes; permission test blocks editor for pos-only role.'),
('Audit_filter_changes_while_loading_only_publish_the_latest_filter_result','Deferred first read failure, updates filters; verifies new selected query wins. Ledger tests distinguish failure/empty and ignore old employee read after next selection.'),
('Handler','Fake handler delegates no CancellationToken observation; fixture URLs .invalid never fetched outside injected HTTP. Employee helper sparse JSON DTO defaults; no database or XAML.')],
'These tests cover real source VM decisions, not native bindings/theme/layout/financial handler atomicity. Missing recovered attendance editor target/session registration and customer race UI-007.',
'Keep meaningful races and extend concrete process reconstruction; document mocked-transport nature, never present fake results as restaurant operational data.')
record(T+'EmptyResponseContractTests.cs','Successful empty-body mutation HTTP transport contract.',[
('Empty_success_response_is_a_confirmed_write_not_an_unknown_json_failure','Eight actions availability/inventory/ready/user-active/permissions/PIN/checklist/exception fake200empty; asserts success=true,value=true,outcomeUnknown=false,error=null. This meaningfully prevents JSON empty-response freeze.'),
('Request','Sparse deserialized DTO bypasses production validation intentionally to reach adapter only. It is not end-to-end API schema/auth proof.'),
('EmptyHandler','Always200empty, no token/status/content/assert request path. Does not exercise204vs200or non-JSON failures or other Menu bool wrappers.')],
'NOT_EXECUTED by reviewer; production endpoints/DB untouched. Missing all empty response routes and negative/cancel/5xx distinction.',
'Extend contract matrix deliberately; keep response handling tests separate from server authorization and native UI.')
record(P+'ViewModels/UsersViewModel.cs','User list, server permission vocabulary, grant/revoke and activation.',[
('PermissionToggle','Localized exact permission/category map with fallback to key; CanActorGrant true for existing held permission OR actor own permission allows revoke while preventing new unauthorized grant in normal UI.'),
('RebuildToggles','Uses actual fetched catalog and selected-user permissions; no invented backend privileges. Immutable per-entry CanActorGrant assumes login permissions fixed until next root login.'),
('ReloadAsync','Reads users+catalog sequential, keeps selected ID, clears list only after both valid. Recovered pending action makes public LoadAsync return early, so initial page list can stay blank while dedicated RetryUserAction must be visible.'),
('CanEditSelected','Requires users_manage, valid loaded selection, nonowner and no dialogs/pending. Server must independently enforce owner immunity, actor/session auth and escalation.'),
('SavePermissionsAsync','Computes grant/revoke diff against selected snapshot; uses durable request on unknown retry. Concurrent other admin edits between reads/writes resolved server-side; potential stale revoke semantics but explicit diff not full replacement.'),
('ToggleActiveAsync','Retains exact target state (not repeated inversion) across ambiguity, reloads target after success. OnIsBusyChanged registers pending; constructor does not register recovered state before busy flag transition.')],
'UserWorkflowTests/ManagementWriteSafety relevant not yet all read. Missing global recovered request ownership/initial pending gate and actual native retry reachability.',
'Use per-actor recovery context and initial session pending registration; authoritative server checks remain mandatory.')
record(P+'ViewModels/UsersViewModel.Actions.cs','Create accounts and change/reset PIN with retained requests/retry.',[
('RoleOptions','Real cashier/manager/owner values; owner emergency uniqueness delegated server. Role selected uses observable value and help, no local secret backdoor.'),
('OpenCreateUser','Requires catalog+permission and editable; resets fields, builds actor-grantable options. CloseUserDialog clears all PIN text when allowed; pending request cannot be closed.'),
('ValidatePin','Checks confirmation equality then actual Domain PinHasher.AssertValidPin, catches only DomainException. No client hashing mistaken for server proof.'),
('CreateUserAsync','Fresh name/role/PIN validation and no unauthorized grant; stable CreateUserRequest with ID, actual actor. Durable JSON store serializes raw PIN field; sensitive persistence source evidence sent to parent security reviewer, no runtime secret accessed.'),
('ChangePinAsync','Fresh self action requires CurrentPin; admin reset forbids owner unless self. Retained self retry calls shared Api.LoginByPinAsync(new PIN) to prove intended change; contrary to comment it mutates HttpClient bearer even on different-user proof. Session.CurrentUser remains unchanged, identity confusion candidate sent to security lead.'),
('RetryUserActionAsync','Dispatches first pending create/PIN/permissions/active without transient dialog/selection requirement; correct reachable concept compared UI-005. Keys global users.* not per logged-in actor, need explicit ownership and journal confidentiality.'),
('RefreshActions','Raises derived edit/create/change/dialog/labels; session pending updated only OnIsBusyChanged, so startup restored request may not block global nav yet.')],
'No credentials entered, saved or printed by reviewer. Need fake API verify token remains unchanged for successful/wrong-user proof, recovered other operator ownership and sensitive journal mitigation tests.',
'Use isolated credential proof HTTP/token-free method that cannot replace session; versioned protected journal for sensitive request; reconcile server idempotency after self PIN change.')
record(P+'ViewModels/AuditViewModel.cs','Read-only append-only history filter screen.',[
('Categories','Hardcodes8categories while comment asserts server actual taxonomy; backend may emit more categories. All-categories still exposes others if query no filter. Severities critical/warning/normal similarly explicit list, full vocabulary cross-check pending.'),
('LoadAsync','Coalesces filter changes with selected category/severity snapshots; discards old result/failure and loops requested filter. Clears Events before fetch and correctly separates loading/error/empty. No edit/delete method on UI.'),
('NotifyReadState','Raises HasNoEvents/HasEvents after load state; delegates server filtering/limit default200. No paging/continuation makes older audit records beyond returned cap unreachable through this screen.')],
'AttendanceWorkflowTests meaningful delayed filter test reviewed; no server append-only tamper-proof property established from read-only UI.',
'Add paginated query/date range and complete catalog from server where needed; preserve unfiltered view and safe error distinctions.')
record(P+'ViewModels/CustomersViewModel.cs','Phone-only read-only customer and saved-address lookup.',[
('SearchAsync','Synchronous IsSearching guard, trims phone and clears result/addresses.404 means legitimate not found, other failure surfaced; orders saved addresses by last used. Source does not claim browsable directory.'),
('AwaitingFirstSearch','Separates initial prompt from no-match and error. Notifications HasSearched/IsSearching/SearchError only update initial state, finally explicitly notifies NotFound, correct after response.'),
('PhoneQuery','Can change during search; request captures string but response not checked against current query. No overlapping commands, but text can display B while customer A results shown if edited while loading. Less harmful read-only directory variant of UI-007; native field disabled state must be inspected.')],
'No PII accessed. Need delayed lookup/edit query and no-match/error tests; no SQL or write operations here.',
'Freeze query or snapshot/display searched phone with result, discard stale response if editable query changed.')
