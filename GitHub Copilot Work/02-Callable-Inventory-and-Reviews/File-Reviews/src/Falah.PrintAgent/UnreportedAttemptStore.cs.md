# src/Falah.PrintAgent/UnreportedAttemptStore.cs

Per-route durable local journal.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `2b3ff93246988ba9678861786ed944d73402944e2edc7f49b942b35c3be3ce62`. Working SHA-256 `2b3ff93246988ba9678861786ed944d73402944e2edc7f49b942b35c3be3ce62`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-97.

Symbols indexed (index is not a claim every method was reviewed): is@15, UnreportedAttemptStore@21, StoredAttempt@27.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 27: `public sealed record StoredAttempt(string AttemptRequestId, bool Succeeded, string? Error, DateTimeOffset AtUtc);`
- Line 29: `public UnreportedAttemptStore(string path)`
- Line 35: `public static UnreportedAttemptStore ForRoute(string directory, string route)`
- Line 59: `private void Save()`
- Line 71: `public StoredAttempt? Find(string printJobId)`
- Line 76: `public void Remember(string printJobId, StoredAttempt attempt)`
- Line 85: `public void Forget(string printJobId)`

Load fails closed on unreadable JSON. ForRoute canonicalizes route and imports legacy entries without deletion. Remember/Forget serialize the entire dictionary to a temporary file then overwrite-move. The in-process lock prevents thread races but there is no cross-machine shared store and no fsync/power-loss durability proof; journal directory must be stable/writable. Recommend server claim fencing, explicit uncertain state, bounded retained-entry cleanup and tested disk-full/power-failure behavior. Corrupt-journal test exists; process-restart/power-loss tests are distinct.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
