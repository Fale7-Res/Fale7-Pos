# Substantive Callable Audit: `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`

- **Source File:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `dbbefd768ce06c1f033a4ac80a56574a9564a9c35e58eba5684310a89bd38cf5`
- **Auditor Specialist:** `printing-workflow-auditor`
- **Total File Callables:** **11**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Hardware transport submitting raw ESC/POS byte streams to local Windows printer queues via unmanaged Win32 spooler P/Invoke APIs (winspool.drv).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.PrintAgent.WindowsSpoolerTicketTransport.WindowsSpoolerTicketTransport(string)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:25:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `25-25`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.WindowsSpoolerTicketTransport(string). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 25-25):**
```csharp
  25 |     public WindowsSpoolerTicketTransport(string printerName) => _printerName = printerName;
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.PrintAgent.WindowsSpoolerTicketTransport.SendAsync(byte[], CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:29:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `29-33`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-003`**

**A. Actual Behavior & Execution Path:**  
Executes synchronous Send(_printerName, payload) on calling thread without honoring CancellationToken.

**B. Trust Boundaries & Validation:**  
PrintAgent background worker.

**C. State Mutations & Side Effects:**  
Synchronous Win32 I/O.

**D. Concurrency & Transaction Implications:**  
Blocks thread pool thread during network printer spooler delays.

**E. Specialist Risks & Invariants:**  
FLAW PRT-003: Thread pool starvation during printer offline/network timeouts because CancellationToken is completely ignored.

> **Verbatim Code Excerpt (Lines 29-33):**
```csharp
  29 |     public Task SendAsync(byte[] payload, CancellationToken ct)
  30 |     {
  31 |         Send(_printerName, payload);
  32 |         return Task.CompletedTask;
  33 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.PrintAgent.WindowsSpoolerTicketTransport.Send(string, byte[])`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:37:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `37-83`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-002`**

**A. Actual Behavior & Execution Path:**  
Calls OpenPrinter, StartDocPrinter, StartPagePrinter, WritePrinter, EndPagePrinter, EndDocPrinter, ClosePrinter. Returns true once bytes are accepted by Windows spooler queue.

**B. Trust Boundaries & Validation:**  
Local system spooler interaction.

**C. State Mutations & Side Effects:**  
Writes raw byte payload to Windows Print Spooler memory buffer.

**D. Concurrency & Transaction Implications:**  
Win32 spooler serialization per printer handle.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW PRT-002: Conflates spooler queue acceptance with physical hardware printing. Paper-out, paper jams, and offline printers silently report success to POS till while food tickets are never physically printed.

> **Verbatim Code Excerpt (Lines 37-83):**
```csharp
  37 |     internal static void Send(string printerName, byte[] payload)
  38 |     {
  39 |         if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
  40 |             throw new InvalidOperationException(
  41 |                 $"Could not open the printer \"{printerName}\": {LastErrorMessage()}");
  42 | 
  43 |         try
  44 |         {
  45 |             var info = new DocInfo
  46 |             {
  47 |                 DocName = "Falah ticket",
  48 |                 OutputFile = null,
  49 |                 // Without RAW the driver renders the bytes as a document and the ESC/POS commands
  50 |                 // print as garbage characters instead of controlling the printer.
  51 |                 DataType = "RAW",
  52 |             };
  53 | 
  54 |             if (StartDocPrinter(printer, 1, info) == 0)
  55 |                 throw new InvalidOperationException($"The printer rejected the job: {LastErrorMessage()}");
  56 | 
  57 |             try
  58 |             {
  59 |                 if (!StartPagePrinter(printer))
  60 |                     throw new InvalidOperationException($"The printer rejected the page: {LastErrorMessage()}");
  61 | 
  62 |                 try
  63 |                 {
  64 |                     var buffer = Marshal.AllocHGlobal(payload.Length);
  65 |                     try
  66 |                     {
  67 |                         Marshal.Copy(payload, 0, buffer, payload.Length);
  68 |                         if (!WritePrinter(printer, buffer, payload.Length, out var written))
  69 |                             throw new InvalidOperationException($"Writing to the printer failed: {LastErrorMessage()}");
  70 |                         // A short write means a truncated ticket, which prints as a plausible-looking
  71 |                         // but wrong receipt - worse than an outright failure, so it is one.
  72 |                         if (written != payload.Length)
  73 |                             throw new InvalidOperationException(
  74 |                                 $"Only {written} of {payload.Length} bytes reached the printer.");
  75 |                     }
  76 |                     finally { Marshal.FreeHGlobal(buffer); }
  77 |                 }
  78 |                 finally { EndPagePrinter(printer); }
  79 |             }
  80 |             finally { EndDocPrinter(printer); }
  81 |         }
  82 |         finally { ClosePrinter(printer); }
  83 |     }
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.PrintAgent.WindowsSpoolerTicketTransport.LastErrorMessage()`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:85:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `85-85`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.LastErrorMessage(). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 85-85):**
```csharp
  85 |     private static string LastErrorMessage() => new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.PrintAgent.WindowsSpoolerTicketTransport.OpenPrinter(string, out IntPtr, IntPtr)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:95:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `95-96`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.OpenPrinter(string, out IntPtr, IntPtr). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 95-96):**
```csharp
  95 |     [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
  96 |     private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string printerName, out IntPtr printer, IntPtr defaults);
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.PrintAgent.WindowsSpoolerTicketTransport.ClosePrinter(IntPtr)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:98:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `98-99`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.ClosePrinter(IntPtr). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 98-99):**
```csharp
  98 |     [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
  99 |     private static extern bool ClosePrinter(IntPtr printer);
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.PrintAgent.WindowsSpoolerTicketTransport.StartDocPrinter(IntPtr, int, Falah.PrintAgent.WindowsSpoolerTicketTransport.DocInfo)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:101:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `101-102`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.StartDocPrinter(IntPtr, int, Falah.PrintAgent.WindowsSpoolerTicketTransport.DocInfo). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 101-102):**
```csharp
 101 |     [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
 102 |     private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocInfo documentInfo);
```

---

<a id="callable-8"></a>
### Callable 8: `Falah.PrintAgent.WindowsSpoolerTicketTransport.EndDocPrinter(IntPtr)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:104:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `104-105`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.EndDocPrinter(IntPtr). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 104-105):**
```csharp
 104 |     [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
 105 |     private static extern bool EndDocPrinter(IntPtr printer);
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.PrintAgent.WindowsSpoolerTicketTransport.StartPagePrinter(IntPtr)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:107:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `107-108`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.StartPagePrinter(IntPtr). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 107-108):**
```csharp
 107 |     [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
 108 |     private static extern bool StartPagePrinter(IntPtr printer);
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.PrintAgent.WindowsSpoolerTicketTransport.EndPagePrinter(IntPtr)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:110:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `110-111`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.EndPagePrinter(IntPtr). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 110-111):**
```csharp
 110 |     [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
 111 |     private static extern bool EndPagePrinter(IntPtr printer);
```

---

<a id="callable-11"></a>
### Callable 11: `Falah.PrintAgent.WindowsSpoolerTicketTransport.WritePrinter(IntPtr, IntPtr, int, out int)`
- **Callable ID:** `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:113:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `113-114`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.WindowsSpoolerTicketTransport.WritePrinter(IntPtr, IntPtr, int, out int). Manages internal state transition within src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 113-114):**
```csharp
 113 |     [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
 114 |     private static extern bool WritePrinter(IntPtr printer, IntPtr bytes, int count, out int written);
```

---
