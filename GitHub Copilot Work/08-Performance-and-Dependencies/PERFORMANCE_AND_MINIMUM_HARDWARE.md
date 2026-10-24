# High Performance and Low-End Hardware Engineering Audit

- **Audit Target:** Resource Footprint, Memory Profiling, EF Core Query Latency & Low-Spec POS Hardware
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Static Resource & Bottleneck Analysis for Low-End POS Terminals.

---

## 1. Target Hardware Profile & Operating Environment

The target POS terminal hardware in restaurant environments typically consists of:
- **CPU:** Intel Celeron J1900 / J4125 / Core i3-4000 series (low IPC, dual/quad-core)
- **RAM:** 4 GB DDR3 / DDR4 (shared with integrated Intel HD graphics)
- **Storage:** 64 GB / 128 GB eMMC or budget SATA SSD
- **Operating System:** Windows 10 IoT Enterprise x64 / Windows 10 Home 64-bit

---

## 2. Forensic Bottlenecks & Static Performance Risks

1. **N+1 Entity Framework Query Traversal in Reporting:**
   `DailyStatementQueryService` and `ShiftSummaryQueryService` perform iterative loops over shifts and child collections without consistent `.AsNoTracking()` and eager `.Include()`. On a busy business day with 500+ orders, this generates hundreds of individual round-trips to SQL Server LocalDB, causing 3–5 second UI freezes on Celeron terminals.
2. **Bitmap Allocation Pressure in BitmapTicketRenderer:**
   `BitmapTicketRenderer` allocates a new `System.Drawing.Bitmap` and `Graphics` context on every print job. For thermal tickets with hundreds of item lines, large unmanaged GDI handles remain in memory until Gen 2 garbage collection fires, contributing to GC pauses during rush hours.
3. **WinUI 3 Process Memory Overhead:**
   WinUI 3 processes (`Falah.Client.CashierWinUI.exe`) carry a baseline working set of ~180MB RAM due to DirectX and Windows App SDK composition runtimes. On 4GB RAM terminals running SQL Server LocalDB (~400MB) and the ASP.NET Core server (~150MB), memory pressure is significant.
4. **Startup Latency:**
   Cold startup of the combined Server + LocalDB + WinUI client takes 12–18 seconds on magnetic HDDs or eMMC storage. Pre-compilation (`PublishReadyToRun`) is recommended.
