# src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f6ec8fbc23f9bb8d797c74ceecc74d23b4644c406a48a3d87715dbc78eb0b2df`

Worktree SHA-256: `f6ec8fbc23f9bb8d797c74ceecc74d23b4644c406a48a3d87715dbc78eb0b2df`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Station-local pending operation JSON journal.

## Source paths and function/resource analysis

- `FileRetainedRequestStore` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:40`: Constructor creates directory immediately. App passes no error callback, so failures in subsequent Save/Load/Clear are silent to operator; constructor failure can still abort startup.

- `PathFor` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:53`: Replaces invalid filename chars; collisions possible between owner strings differing only by invalid character. Current stable literal/ID owner keys constrain this, no exploitable path traversal asserted.

- `Save` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:59`: Writes serialized type, UTC time and payload to .tmp then move overwrite. Atomic rename reduces partial JSON exposure but no flush-to-disk guarantee; same owner concurrent processes share same .tmp/path without interprocess locking. Exceptions swallowed; financial request can still be sent with no recoverable journal.

- `Clear` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:84`: Deletes known journal file; failure swallowed so successful operation may recover again next launch. Server idempotency is necessary to keep replay safe.

- `Load` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:94`: Resolves assembly-qualified Type then deserializes payload. Unknown types trigger actual deletion of original journal; corrupt/unreadable envelope returns null without durable quarantine/report. No allow-list, version migration, authentication/integrity or encryption; local-user tampering is a trust boundary to assess, not proven RCE.

- `List` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:115`: Reads all JSON envelopes inside one catch; single malformed entry makes whole list empty, hiding even valid pending operations. No per-record error recovery; fresh UI notices can be falsely absent.

- `RetainedRequestStorage` at `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs:143`: Mutable process ambient hook configured before view models; no lock/per-user context. Cashier and CallCenter use separate station directories but duplicate implementation will drift.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

RetainedRequestDurabilityTests tests corrupt file as no pending and unwritable store still current-session protected. That verifies fail-open design, not money durability. Missing corruption alongside valid record, type version migration, interprocess collision and durable write failure UI tests.

## Recommendations (not implemented)

Fail safely or prominently expose unavailable financial journal; retain/quarantine unreadable evidence; add per-record list handling and explicit versioned allow-listed DTO schema; prevent same-station concurrent writers.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.

## Anchor correction

Implementation paragraphs now anchor the concrete constructor40, Save59, Clear84, Load94 and List115. Earlier19/20/25/27 were interface declarations, not implementation bodies. Existing analysis preserved; exact stable-symbol shards already distinguish interface and implementation receipts. Corrected after independent parent challenge; no source or runtime change.
