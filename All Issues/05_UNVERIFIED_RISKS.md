# 05 — Unverified Risks

Two kinds of entry follow. Both need evidence that static review cannot give.

1. **17 canonical issues with status UNVERIFIED.** Their existence is not established.
2. **NOT_FIXED_YET issues whose trigger needs runtime confirmation.** The code path is confirmed, but how often it
   happens or how it actually behaves is not.

## 1. UNVERIFIED issues

| Issue | What is unknown | What would resolve it |
|---|---|---|
| [TEST-0001](ISSUES/TEST-0001.md) | Current state of the integration suite (last run 19/760 red; later edits unbuilt) | Rebuild and run the suite in a scratch copy against `FalahTest_*` |
| [UI-0006](ISSUES/UI-0006.md) | Whether a recovered electronic confirm/cancel can be retried at the till | Trace `CashierViewModel` recovery; desktop run |
| [UI-0005](ISSUES/UI-0005.md) | Live theme switching for converter brushes | Desktop run (UI5) |
| [DB-0010](ISSUES/DB-0010.md) | Deadlocks under two tills (range locks, inverse transfers) | Concurrency load test |
| [FIN-0043](ISSUES/FIN-0043.md) | Month/year/YTD totals = sums of days | Property-style report tests |
| [FIN-0045](ISSUES/FIN-0045.md) | Approval provenance in shift close / driver variance | Code trace |
| [FIN-0046](ISSUES/FIN-0046.md) | Packaging cost in profit completeness | Code trace |
| [INV-0013](ISSUES/INV-0013.md) | Recipe ↔ product dual pointers after retarget | Code trace of `SaveRecipeHandler` |
| [MENU-0003](ISSUES/MENU-0003.md) | Till behaviour when a price changes mid-cart | Client trace and test |
| [PRT-0009](ISSUES/PRT-0009.md) | Bitmap memory for long tickets | Memory measurement |
| [PRT-0018](ISSUES/PRT-0018.md) | Ticket layout and Arabic encoding on real printers | On-site print test |
| [RPT-0002](ISSUES/RPT-0002.md) | Inconsistent multi-statement reports and snapshots | Concurrency test during snapshot composition |
| [RPT-0005](ISSUES/RPT-0005.md) | Dashboard reopened-day omission and exception counts | Code trace |
| [SEC-0030](ISSUES/SEC-0030.md) | Reprint authorization rules | Code trace and owner policy |
| [TEST-0003](ISSUES/TEST-0003.md) | Thread safety of test doubles | Only if tests run concurrently on them |
| [OPS-0001](ISSUES/OPS-0001.md) | Dependency CVEs; .NET 8 end of support | `dotnet list package --vulnerable` (feed online); check Microsoft's lifecycle page |
| [OPS-0003](ISSUES/OPS-0003.md) | Whether production is a sleeping laptop; disk write-cache | Site inspection |

## 2. Confirmed code paths whose real-world trigger is unverified

| Issue | Trigger needing evidence | Test |
|---|---|---|
| [FIN-0010](ISSUES/FIN-0010.md), [FIN-0011](ISSUES/FIN-0011.md), [FIN-0012](ISSUES/FIN-0012.md), [FIN-0013](ISSUES/FIN-0013.md), [FIN-0014](ISSUES/FIN-0014.md), [PAYR-0003](ISSUES/PAYR-0003.md), [SEC-0010](ISSUES/SEC-0010.md), [INV-0001](ISSUES/INV-0001.md), [INV-0002](ISSUES/INV-0002.md), [FIN-0021](ISSUES/FIN-0021.md), [FIN-0022](ISSUES/FIN-0022.md) | Race windows (no interleaving was executed) | Concurrency tests with barriers (06) |
| [PRT-0003](ISSUES/PRT-0003.md), [HW-0001](ISSUES/HW-0001.md) | Real drain times against the 120 s lease and the 30 s kick expiry | On-site timing with a backlog |
| [PRT-0004](ISSUES/PRT-0004.md) | Printer firmware behaviour on paper-out | H-01, H-02 |
| [PRT-0010](ISSUES/PRT-0010.md) | Whether agent install folders are writable | Inspect the install path and the account |
| [API-0004](ISSUES/API-0004.md), [PERF-0001](ISSUES/PERF-0001.md), [PERF-0003](ISSUES/PERF-0003.md) | Latency on production-sized data | Load test on a seeded `FalahTest_*` database |
| [DB-0004](ISSUES/DB-0004.md) | Two backups in the same second | Integration test |
| [DB-0005](ISSUES/DB-0005.md), [DB-0006](ISSUES/DB-0006.md) | Restore behaviour under load or failure | Restore rehearsal on a test database |
| [SEC-0006](ISSUES/SEC-0006.md), [SEC-0022](ISSUES/SEC-0022.md) | Production configuration and data | Site inspection; security endpoint |
| [FIN-0028](ISSUES/FIN-0028.md) | Executed only through the .NET Framework BCL, not the .NET 8 app | T-TIME-01 |
| [MENU-0002](ISSUES/MENU-0002.md) | Exact UI blocking (the code chain is complete) | Desktop run |

## 3. Hypotheses from the resilience audit (all mapped)
- **UH-01** → [OPS-0003](ISSUES/OPS-0003.md) · **UH-02** → [PRT-0003](ISSUES/PRT-0003.md) · **UH-03** → [PRT-0010](ISSUES/PRT-0010.md) · **UH-04** → [UI-0006](ISSUES/UI-0006.md)
- **UH-05** → [DB-0010](ISSUES/DB-0010.md) · **UH-06** → [FIN-0028](ISSUES/FIN-0028.md) · **UH-07** → [OPS-0006](ISSUES/OPS-0006.md) · **UH-08** → [PRT-0005](ISSUES/PRT-0005.md)
