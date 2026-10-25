# Substantive Callable Audit: `src/Falah.PrintAgent/UnreportedAttemptStore.cs`

- **Source File:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `2b3ff93246988ba9678861786ed944d73402944e2edc7f49b942b35c3be3ce62`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **9**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`UnreportedAttemptStore`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/UnreportedAttemptStore.cs) is the local, crash-resilient write-ahead journal and offline attempt store for `Falah.PrintAgent`.

In thermal POS printing, physical execution cannot be rolled back. Once ESC/POS bytes are committed to thermal paper and the cutter blade descends, the physical artifact exists in the kitchen or counter. The distributed communication gap occurs when reporting that outcome back to the central server:
1. The print agent sends raw ESC/POS bytes to the thermal printer (via USB spooler, TCP 9100 socket, or COM serial port).
2. The paper physically prints.
3. Before the agent can report `Succeeded = true` to `POST /api/print-jobs/{id}/report-result`, the local network drops, the POS server restarts, or the station experiences sudden power loss.
4. On the server, the job remains in `Pending` status.
5. When connectivity resumes, the agent polls pending jobs. Without persistent local state, the agent would fetch the same job and **print it a second time**.

Kitchen cooks prepare food for every ticket that drops into the bin. Multiple tickets mean duplicate meal preparation, wasted food, and kitchen confusion. [`UnreportedAttemptStore`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/UnreportedAttemptStore.cs) resolves this fundamental distributed consensus problem:
- It records an attempt to disk **before and after** physical transport.
- On subsequent polls or after agent reboots, [`PrintJobProcessor`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L85-L91) checks `Find(job.Id)`. If present, it bypasses the printer transport entirely and re-reports the previously recorded attempt ID.
- Disk persistence uses an atomic temporary file replacement (`.tmp` $\rightarrow$ `File.Move`), ensuring that power failure during disk writes cannot corrupt the attempt journal.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `UnreportedAttemptStore.StoredAttempt`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:27:5:RecordDeclaration`
- **Declaration Kind:** `RecordDeclaration` | **Line Range:** `27-27`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Defines the immutable persistent data structure representing an unconfirmed physical print attempt:
- `AttemptRequestId` (`string`): Unique idempotency key (format: `pra-{guid}`) generated prior to transport write.
- `Succeeded` (`bool`): Whether physical transport transmission succeeded or failed.
- `Error` (`string?`): Captured exception message if transmission failed; null on success.
- `AtUtc` (`DateTimeOffset`): Timestamp of the attempt.

**B. Callers, Dependencies & Side Effects:**  
Instantiated in [`PrintJobProcessor.ProcessAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L106-L107) and line 125. Serialized to and deserialized from JSON by `Load` and `Save`. Immutable record with value semantics; no side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Stores the exact error string returned by physical transports (e.g. `Win32Exception`, `SocketException`, `IOException`).

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
C# immutable record. Thread-safe by construction.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Serialized and deserialized in [`PrintAgentTests.A_job_whose_outcome_could_not_be_reported_is_re_reported_not_reprinted`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L203-L225).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Line 27):**
```csharp
    public sealed record StoredAttempt(string AttemptRequestId, bool Succeeded, string? Error, DateTimeOffset AtUtc);
```

---

<a id="callable-2"></a>
### Callable 2: `UnreportedAttemptStore.UnreportedAttemptStore(string)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:29:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `29-33`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Constructs an `UnreportedAttemptStore` instance for a specific JSON file path. Initializes `_path = path` and immediately populates the in-memory attempt cache `_attempts` by invoking `Load(path)`. Returns the initialized store.

**B. Callers, Dependencies & Side Effects:**  
Called by `ForRoute` factory method and test fixtures (`NewStore()` in [`PrintAgentTests.cs:102`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L102)). Side effect: performs synchronous file I/O on initialization.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Initializes before any hardware communication begins. Ensures that if the workstation crashed during a previous print attempt, the in-memory state is immediately restored from disk before the first poll executes.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Single-threaded initialization. Subsequent accesses are synchronized via `lock (_gate)`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Exercised across all tests in `PrintAgentTests.cs`.
- *Missing Tests:* Verification of constructor behavior when passed a directory path rather than a file path.

> **Verbatim Code Excerpt (Lines 29-33):**
```csharp
    public UnreportedAttemptStore(string path)
    {
        _path = path;
        _attempts = Load(path);
    }
```

---

<a id="callable-3"></a>
### Callable 3: `UnreportedAttemptStore.ForRoute(string, string)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:35:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `35-44`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Static factory method establishing route-isolated storage.
1. Normalizes the route string to canonical enum name via `Enum.Parse<PrintRoute>(route, true).ToString()`.
2. Targets route-specific file: `Path.Combine(directory, $"unreported-print-attempts-{canonical}.json")`.
3. Backward Compatibility Migration: Reads legacy non-partitioned store (`unreported-print-attempts.json`) if present, and imports any unmigrated attempts into the route store via `store.Remember(entry.Key, entry.Value)` without deleting the legacy file.
Returns `UnreportedAttemptStore`.

**B. Callers, Dependencies & Side Effects:**  
Invoked in [`Program.cs:123`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L123) when starting `Falah.PrintAgent`. Side effects: reads legacy file and creates/writes route-specific JSON journal on disk.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Route Partitioning Invariant:** Multiple print agents often run on the same physical server machine (e.g., Cashier running `Receipt` agent, and Kitchen relay running `Charcoal` and `Grill` agents). Partitioning the journal by route ensures that distinct processes never contend for or overwrite each other's offline state files.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Isolates file locks by route. Coupled with the machine-wide named mutex `Global\FalahPrintAgent-{options.Route}` in [`Program.cs:100`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L100), ensures exactly one process accesses a route's journal file at any time.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Route parsing tested in [`PrintAgentTests.Route_aliases_use_one_lock_and_queue_identity`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L16-L26).
- *Missing Tests:* Direct unit test verifying the legacy file migration path (asserting legacy entries are copied into the new route-specific file).

> **Verbatim Code Excerpt (Lines 35-44):**
```csharp
    public static UnreportedAttemptStore ForRoute(string directory, string route)
    {
        var canonical = Enum.Parse<Falah.Domain.Printing.PrintRoute>(route, true).ToString();
        var store = new UnreportedAttemptStore(Path.Combine(directory, $"unreported-print-attempts-{canonical}.json"));
        // Preserve any outcomes recorded by the earlier shared journal. Each route only ever
        // looks up its own pending job ids; do not delete the legacy file during an upgrade.
        foreach (var entry in Load(Path.Combine(directory, "unreported-print-attempts.json")))
            if (store.Find(entry.Key) is null) store.Remember(entry.Key, entry.Value);
        return store;
    }
```

---

<a id="callable-4"></a>
### Callable 4: `UnreportedAttemptStore.Load(string)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:46:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `46-57`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Reads and deserializes the JSON journal file from disk into `Dictionary<string, StoredAttempt>`.
- If file does not exist: returns a new empty dictionary.
- If file exists and contains valid JSON: returns deserialized dictionary.
- If an exception occurs (corrupted JSON, disk read error, access denied): **fails closed** by throwing `InvalidOperationException("Cannot read the print-attempt journal. Restore or inspect it before printing; ignoring it could duplicate tickets.", ex)`.

**B. Callers, Dependencies & Side Effects:**  
Private static method called by constructor and `ForRoute`. Reads from filesystem.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Fail-Closed Safety Invariant:** If the journal file is corrupted or half-written, returning an empty dictionary would lose track of already-printed jobs. The agent would poll the server, receive pending jobs, and reprint physical tickets. By throwing a fatal exception, the agent halts, forcing operator inspection before duplicate physical printing can occur.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Synchronous disk read. File is opened with shared read access.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintAgentTests.Corrupt_attempt_journal_fails_closed`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L45-L51) explicitly asserts that writing `"{broken"` causes `InvalidOperationException`.
- *Missing Tests:* Test for empty file (0 bytes) vs corrupted JSON.

> **Verbatim Code Excerpt (Lines 46-57):**
```csharp
    private static Dictionary<string, StoredAttempt> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, StoredAttempt>>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot read the print-attempt journal. Restore or inspect it before printing; ignoring it could duplicate tickets.", ex);
        }
    }
```

---

<a id="callable-5"></a>
### Callable 5: `UnreportedAttemptStore.Save()`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:59:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `59-69`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Flushes the in-memory `_attempts` dictionary to disk using an atomic write-replace algorithm:
1. Ensures the destination directory exists via `Directory.CreateDirectory`.
2. Serializes dictionary to JSON string.
3. Writes full contents to a temporary sibling file: `var temporary = _path + ".tmp"`.
4. Executes atomic replace: `File.Move(temporary, _path, overwrite: true)`.
Returns `void`.

**B. Callers, Dependencies & Side Effects:**  
Private method called exclusively under `lock (_gate)` by `Remember` and `Forget`. Modifies filesystem.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Power-Loss Atomicity Invariant:** Thermal receipt printers are deployed in harsh kitchen and cashier environments where accidental unplugging and power outages are frequent. If `File.WriteAllText(_path, ...)` were used directly, a power cut mid-write leaves a truncated, corrupt JSON file. Writing to `.tmp` and moving via the OS atomic directory entry rename guarantees that upon reboot, the operating system sees either the previous valid file or the new valid file, never a corrupted fragment.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Must always be called under `lock (_gate)` to ensure thread safety. `File.Move(..., overwrite: true)` relies on Windows NTFS atomic file replace semantics.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Verified indirectly by all persistence tests in `PrintAgentTests.cs`.
- *Missing Tests:* Verification of handling when target directory is write-protected or disk is full.

> **Verbatim Code Excerpt (Lines 59-69):**
```csharp
    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        // Written to a temp file and moved into place, so a station losing power mid-write finds
        // either the old file or the new one, never half of either.
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(_attempts));
        File.Move(temporary, _path, overwrite: true);
    }
```

---

<a id="callable-6"></a>
### Callable 6: `UnreportedAttemptStore.Find(string)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:71:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `71-74`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Looks up whether an unacknowledged attempt exists for `printJobId`. Synchronizes on `lock (_gate)` and queries `_attempts.GetValueOrDefault(printJobId)`. Returns `StoredAttempt?` (the stored attempt record if present; `null` if the job has not been attempted).

**B. Callers, Dependencies & Side Effects:**  
Invoked at the beginning of [`PrintJobProcessor.ProcessAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L85-L91) and during legacy migration in `ForRoute`. Read-only; no side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Duplicate Prevention Gate:** The decision point in `PrintJobProcessor`:
  ```csharp
  if (_unreported.Find(job.Id) is { } already)
  {
      _log($"job {job.Id} already printed; re-reporting attempt {already.AttemptRequestId} instead of reprinting");
      await ReportAsync(new ReportPrintJobResultRequest(already.AttemptRequestId, job.Id, already.Succeeded, already.Error, _reportedByUserId), ct);
      return;
  }
  ```
  If `Find` returns non-null, the agent completely skips `_transport.SendAsync`, eliminating the possibility of duplicate paper printing.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe read protected by `_gate` monitor.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintAgentTests.A_job_whose_outcome_could_not_be_reported_is_re_reported_not_reprinted`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L203-L225).
- *Missing Tests:* Concurrent reads while writes are occurring.

> **Verbatim Code Excerpt (Lines 71-74):**
```csharp
    public StoredAttempt? Find(string printJobId)
    {
        lock (_gate) return _attempts.GetValueOrDefault(printJobId);
    }
```

---

<a id="callable-7"></a>
### Callable 7: `UnreportedAttemptStore.Remember(string, StoredAttempt)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:76:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `76-83`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Adds or updates the attempt record for `printJobId`. Enters `lock (_gate)`, updates `_attempts[printJobId] = attempt`, and immediately calls `Save()` to synchronize disk state. Returns `void`.

**B. Callers, Dependencies & Side Effects:**  
Called by [`PrintJobProcessor.ProcessAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L106-L107) before sending to printer, called again at line 125 after transport outcome is known, and called at line 155 if server reporting fails. Mutates memory and writes to disk.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Pre-Transport Invariant (Uncertain State):** Line 106 records an uncertain failure attempt (`"Print interrupted; output may exist..."`) *before* the first byte is transmitted to `_transport.SendAsync`. If the application crashes or power cuts while the printer is mid-stream, the restored attempt forces human inspection rather than automatic reprint upon reboot.
- **Post-Transport Update:** Once transport completes (or fails), line 125 updates the record with real hardware results (`succeeded = true` or exception message).

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe; synchronized via `_gate`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintAgentTests.Cancellation_during_send_leaves_a_durable_uncertain_attempt_and_does_not_reprint`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L29-L43).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 76-83):**
```csharp
    public void Remember(string printJobId, StoredAttempt attempt)
    {
        lock (_gate)
        {
            _attempts[printJobId] = attempt;
            Save();
        }
    }
```

---

<a id="callable-8"></a>
### Callable 8: `UnreportedAttemptStore.Forget(string)`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:85:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `85-91`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Removes the unconfirmed attempt for `printJobId`. Enters `lock (_gate)`. If `_attempts.Remove(printJobId)` returns true, calls `Save()` to persist the removal. If key did not exist, avoids unnecessary disk I/O. Returns `void`.

**B. Callers, Dependencies & Side Effects:**  
Invoked by [`PrintJobProcessor.ReportAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L139) immediately after the central POS server successfully accepts `_server.ReportResultAsync`. Mutates memory and writes to disk.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Lifecycle Finalization Invariant:** Once the server records the attempt in `PrintJobAttempts` and updates `PrintJobEntity.Status`, the local store has fulfilled its mission. Removing the entry ensures the local journal does not grow indefinitely and allows genuine reprints (minted with new IDs) to proceed cleanly.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe; synchronized via `_gate`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintAgentTests.A_job_whose_outcome_could_not_be_reported_is_re_reported_not_reprinted`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L224) (asserts `Assert.Null(store.Find("job-1"))` after recovery).
- *Missing Tests:* Calling `Forget` for a non-existent job ID (verifying no disk write).

> **Verbatim Code Excerpt (Lines 85-91):**
```csharp
    public void Forget(string printJobId)
    {
        lock (_gate)
        {
            if (_attempts.Remove(printJobId)) Save();
        }
    }
```

---

<a id="callable-9"></a>
### Callable 9: `UnreportedAttemptStore.Count`
- **Callable ID:** `src/Falah.PrintAgent/UnreportedAttemptStore.cs:93:5:PropertyDeclaration`
- **Declaration Kind:** `PropertyDeclaration` | **Line Range:** `93-96`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Property getter returning the count of currently unconfirmed attempts held in memory. Enters `lock (_gate)` and returns `_attempts.Count`. Returns `int`.

**B. Callers, Dependencies & Side Effects:**  
Exposed for testing and operational diagnostics. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Surfaces how many completed physical print jobs are waiting for central server reconnection.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe read protected by `_gate`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Used in agent integration assertions.
- *Missing Tests:* Direct assertion verifying `Count` increments on `Remember` and decrements on `Forget`.

> **Verbatim Code Excerpt (Lines 93-96):**
```csharp
    public int Count
    {
        get { lock (_gate) return _attempts.Count; }
    }
```
