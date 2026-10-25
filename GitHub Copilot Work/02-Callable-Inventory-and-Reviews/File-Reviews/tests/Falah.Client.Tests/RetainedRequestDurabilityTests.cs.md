# File Review: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:21:5:ConstructorDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:21:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.RetainedRequestDurabilityTests()
- **Lines:** [21-25]
- **Behavior:** Test fixture constructor generating an isolated temporary folder (`Path.Combine(Path.GetTempPath(), "falah-retained-" + Guid.NewGuid().ToString("N"))`) to host disk-persisted retained request files during test execution.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:27:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:27:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Dispose()
- **Lines:** [27-30]
- **Behavior:** Test cleanup method deleting the temporary file storage directory and any retained JSON files recursively upon test completion, shielding callers from I/O teardown exceptions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:32:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:32:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Store()
- **Lines:** [32-32]
- **Behavior:** Factory helper instantiating a `FileRetainedRequestStore` bound to the test instance's isolated temporary directory.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Ok()
- **Lines:** [34-34]
- **Behavior:** Helper constructing a successful `ApiClient.ApiResult<bool>(true, true, null)` result.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Unknown()
- **Lines:** [35-35]
- **Behavior:** Helper constructing an ambiguous/unresolved `ApiClient.ApiResult<bool>(false, false, "timeout", OutcomeUnknown: true)` result representing lost network connections or read timeouts.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:36:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:36:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Rejected()
- **Lines:** [36-36]
- **Behavior:** Helper constructing a definite business rejection `ApiClient.ApiResult<bool>(false, false, "rejected")` result.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:38:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:38:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.An_ambiguous_outcome_survives_the_process_that_submitted_it()
- **Lines:** [38-53]
- **Behavior:** Integration test asserting cross-process durability of in-flight requests across unexpected application terminations (e.g., sudden hardware power loss).
  - Scenario: Hardware crash/power cut recovery after network timeout.
  - Production code exercised: `RetainedRequest<T>.SendAsync`, `FileRetainedRequestStore.Save`, `FileRetainedRequestStore.TryLoad`.
  - Assertions: First instance submits `SampleRequest("req-1", 12345, "سلفة")` and receives `Unknown()`; process terminates; fresh `RetainedRequest` instance on next startup finds `HasPending == true`, `WasRecovered == true`, and `Value == submitted`.
  - Gaps: Does not test crash simulation specifically during partial file write (atomic file write semantics are tested separately).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:45:42:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:45:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [45-45]
- **Behavior:** Inline expression lambda 'await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_resolved_outcome_leaves_nothing_to_recover()
- **Lines:** [55-67]
- **Behavior:** Asserts disk cleanup semantics when an in-flight operation resolves successfully.
  - Scenario: Happy path completion cleans up persistence.
  - Production code exercised: `RetainedRequest<T>.SendAsync`, `FileRetainedRequestStore.Delete`.
  - Assertions: Submitting request with `Ok()` outcome purges disk storage (`store.List()` is empty); a new instance finds `HasPending == false` and `WasRecovered == false`.
  - Gaps: Does not test file locked or access denied errors during deletion.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:60:71:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:60:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [60-60]
- **Behavior:** Test mock callback returning successful `Ok()` outcome.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:69:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:69:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_plain_rejection_of_a_first_attempt_leaves_nothing_to_recover()
- **Lines:** [69-77]
- **Behavior:** Asserts that immediate first-attempt business rejections are not persisted to disk.
  - Scenario: Immediate business rule rejection (e.g. invalid arguments or 400 Bad Request).
  - Production code exercised: `RetainedRequest<T>.SendAsync`.
  - Assertions: `HasPending` is `false` on a fresh instance, confirming rejection does not create lingering pending operations.
  - Gaps: None.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:74:71:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:74:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Test mock callback returning definite `Rejected()` outcome.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:79:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:79:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_rejection_after_an_ambiguous_outcome_keeps_the_request_on_disk()
- **Lines:** [79-92]
- **Behavior:** Asserts the critical durability invariant that a rejection following an ambiguous outcome MUST NOT delete the request from disk.
  - Scenario: Error path & safety guarantee. An ambiguous timeout is followed on retry by a rejection (e.g., 409 conflict or shift closed).
  - Production code exercised: `RetainedRequest<T>.SendAsync`, `FileRetainedRequestStore`.
  - Assertions: A later 'no' does not prove the initial lost request did not commit on the server; the request remains persisted (`HasPending == true`, `Value == submitted`) to prevent silent financial drift or lost transactions.
  - Gaps: Does not test administrative dispute resolution or manual reconciliation tools.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:86:42:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:86:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Inline expression lambda 'await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:87:42:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:87:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-87]
- **Behavior:** Inline expression lambda 'await first.SendAsync(submitted, _ => Task.FromResult(Rejected()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:94:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:94:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.The_recovered_request_is_re_sent_byte_for_byte_not_rebuilt()
- **Lines:** [94-112]
- **Behavior:** Asserts that a recovered request is re-sent byte-for-byte rather than rebuilt from potentially modified screen state.
  - Scenario: Recovery retry payload integrity.
  - Production code exercised: `RetainedRequest<T>.SendAsync`.
  - Assertions: When the UI screen attempts to retry by passing a freshly constructed request object (`SampleRequest("req-2", 1, ...)`), `RetainedRequest` intercepts and transmits the exact persisted `req-1` byte-for-byte (`sent == submitted`).
  - Gaps: None.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:100:42:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:100:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Inline expression lambda 'await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:105:80:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:105:80:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [105-109]
- **Behavior:** Test mock capturing the actually dispatched request object and returning `Ok()`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:114:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:114:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Confirming_a_recovered_request_clears_it_for_good()
- **Lines:** [114-127]
- **Behavior:** Asserts that successful server confirmation of a recovered request permanently purges it from disk.
  - Scenario: Recovery confirmation lifecycle.
  - Production code exercised: `RetainedRequest<T>.SendAsync`, `FileRetainedRequestStore.Delete`.
  - Assertions: After recovering from disk and receiving `Ok()`, `HasPending` transitions to `false` in both current and subsequent instances.
  - Gaps: None.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:120:35:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:120:35:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [120-120]
- **Behavior:** Inline expression lambda '.SendAsync(submitted, _ => Task.FromResult(Unknown()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:123:46:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:123:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [123-123]
- **Behavior:** Inline expression lambda 'await recovered.SendAsync(submitted, _ => Task.FromResult(Ok()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:129:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:129:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Owners_do_not_recover_each_others_requests()
- **Lines:** [129-138]
- **Behavior:** Asserts owner key isolation between concurrent screens or workflows.
  - Scenario: Multi-flow partition isolation.
  - Production code exercised: `RetainedRequest<T>`, owner key namespace partitioning.
  - Assertions: A request saved under `"owner.a"` is strictly isolated; `"owner.b"` finds `HasPending == false`.
  - Gaps: Does not test path traversal attacks in ownerKey strings (e.g., `"../../"`).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:134:60:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:134:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [134-134]
- **Behavior:** Test mock callback returning `Unknown()` outcome for owner isolation test.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:140:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:140:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.An_owner_that_submits_several_command_types_recovers_the_right_one()
- **Lines:** [140-153]
- **Behavior:** Asserts polymorphic request type recovery in `RetainedRequest<object>`.
  - Scenario: Polymorphic command storage for screens managing multiple transaction kinds.
  - Production code exercised: `RetainedRequest<object>`, JSON type metadata preservation.
  - Assertions: Deserializes stored object back into concrete `OtherRequest` record, asserting `RequestId == "req-x"` and `Note == "ملاحظة"` without type loss.
  - Gaps: Does not test type version migration or missing assembly references on deserialize.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:146:61:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:146:61:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Test mock callback returning `Unknown()` for polymorphic test.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:155:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:155:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.Without_an_owner_key_nothing_is_written_to_disk()
- **Lines:** [155-167]
- **Behavior:** Asserts in-memory fallback behavior when no `ownerKey` is provided.
  - Scenario: Backward compatibility for owner-less in-memory retention.
  - Production code exercised: `RetainedRequest<T>`.
  - Assertions: When `ownerKey == null`, `HasPending == true` in memory, `WasRecovered == false`, and zero files are written to disk (`store.List()` is empty).
  - Gaps: None.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:162:72:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:162:72:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [162-162]
- **Behavior:** Test mock callback returning `Unknown()` for owner-less test.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:169:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:169:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_pending_operation_is_listed_with_the_time_it_was_submitted()
- **Lines:** [169-181]
- **Behavior:** Asserts diagnostic metadata tracking for pending operations.
  - Scenario: Diagnostic audit inspection.
  - Production code exercised: `FileRetainedRequestStore.List`.
  - Assertions: Returns metadata record verifying `record.OwnerKey == "owner.listed"`, `record.TypeName == "SampleRequest"`, and `record.SavedAtUtc >= before`.
  - Gaps: None.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:175:62:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:175:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [175-175]
- **Behavior:** Test mock callback returning `Unknown()` for metadata listing test.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:183:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:183:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_corrupt_pending_file_does_not_block_the_station()
- **Lines:** [183-193]
- **Behavior:** Asserts fault-tolerant startup handling when a pending file contains corrupt or partially-written JSON.
  - Scenario: Power failure during disk write resulting in corrupted JSON (`"{ not json"`).
  - Production code exercised: `RetainedRequest<T>`, exception shielding in `FileRetainedRequestStore`.
  - Assertions: Corrupt pending file is caught and handled gracefully; `HasPending == false`, preventing station launch crash.
  - Gaps: Does not test administrative alert generation or backup file renaming on corruption.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:195:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:195:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.RetainedRequestDurabilityTests.A_store_that_cannot_write_still_protects_the_current_session()
- **Lines:** [195-212]
- **Behavior:** Asserts in-memory protection continuity when the underlying disk store is completely unwritable.
  - Scenario: Filesystem error / disk full resilience.
  - Production code exercised: `RetainedRequest<T>`, exception handling during store persistence.
  - Assertions: When directory is replaced with an unwritable file, `SendAsync` still retains the operation in memory (`HasPending == true`, `Value == submitted`) rather than crashing or dropping the transaction.
  - Gaps: Does not test UI disk space error notifications.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:205:45:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:205:45:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [205-205]
- **Behavior:** Inline expression lambda 'await retained.SendAsync(submitted, _ => Task.FromResult(Unknown()));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
