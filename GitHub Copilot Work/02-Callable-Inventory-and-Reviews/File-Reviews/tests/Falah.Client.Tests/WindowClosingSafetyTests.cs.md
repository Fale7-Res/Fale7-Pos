# tests/Falah.Client.Tests/WindowClosingSafetyTests.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `2c975e1b40951623b7acffc6f366ff42b471a926a6bd8a8e10ed5a96cd2bbc70`

Worktree SHA-256: `2c975e1b40951623b7acffc6f366ff42b471a926a6bd8a8e10ed5a96cd2bbc70`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Unit tests for normal cashier window-close decision.

## Source paths and function/resource analysis

- `Cashier_window_close_respects_both_pos_and_management_operations` at `tests/Falah.Client.Tests/WindowClosingSafetyTests.cs:18`: Uses fake HTTP handler, admin-like test permissions and directly toggles busy/pending owner. Asserts TryClose and notice state, stops timers. Valid narrow close gate test; no actual native window, pending DTO, crash/restart or in-flight HTTP completion.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Runtime NOT_TESTED by reviewer; explicit distinction from forced termination required.

## Recommendations (not implemented)

Retain test and add durable request recovery checks; native AppWindow Closing event wiring separate integration test.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
