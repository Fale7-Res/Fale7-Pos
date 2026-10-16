# Demo readiness — Falah Restaurant OS (management demo)

Audited state as of 2026-09-19. Scope: a live demonstration to management on **this** development
PC (single device, everything on `localhost`). **This is not a go-live sign-off.** Go-live items are
in `GO_LIVE_CLEANUP_CHECKLIST.md` (section 12 covers system and configuration).

Nothing in the database has been deleted, reset, closed or edited to prepare for the demo. Every
data item below is classified for a human decision.

---

## 1. Morning of the demo — 10-minute checklist

1. **Close heavy apps.** Measured on this PC: Claude desktop ~1.2 GB, VS Code ~1.1 GB, ChatGPT
   ~0.95 GB, Chrome ~0.6 GB. With them open, free memory drops to ~1 GB, which is the condition
   that starves SQL Server (§5).
2. Start the Falah server (§2, step 2).
3. Run the read-only check:
   `powershell -ExecutionPolicy Bypass -File tools\pre-demo-check.ps1 -Pin <manager PIN>`
   → it must print `OVERALL: PASS`.
4. If the SQL memory rows FAIL even with apps closed, restart the `MSSQL$SQLEXPRESS` service,
   restart the Falah server, and run the check again.
5. Start the cashier app and log in once (§2, step 3), before anyone arrives.

---

## 2. Startup procedure for this PC

| # | Component | Executable | Config source | URL / port | Process name | Logs | Healthy when |
|---|-----------|------------|---------------|------------|--------------|------|--------------|
| 1 | SQL Server Express | Windows service `MSSQL$SQLEXPRESS` (starts automatically) | Instance `.\SQLEXPRESS`, database `Falah` (the server's built-in default connection; no connection string is configured) | local instance | `sqlservr.exe` | SQL Server error log | `Get-Service 'MSSQL$SQLEXPRESS'` → Running |
| 2 | Falah server | `src\Falah.Server\bin\Release\net8.0\Falah.Server.exe`, started from that folder | `appsettings.json` beside the exe (Production environment: no Swagger) | `http://localhost:5151` (localhost only) | `Falah.Server.exe` | Console window only when run by hand (redirect it: `Falah.Server.exe > server.log`). Windows Event Log when installed as the `FalahServer` service, which it is **not** on this PC | Console shows `Now listening on: http://localhost:5151`; `http://localhost:5151/health` → 200. Applies pending migrations at start. Automatic backup runs if the last one is older than 20 h |
| 3 | Cashier app | `src\Falah.Client.CashierWinUI\bin\x64\Release\net8.0-windows10.0.19041.0\Falah.Client.CashierWinUI.exe` | Server address: environment variable `FALAH_SERVER_URL`, default `http://localhost:5151` | client of step 2 | `Falah.Client.CashierWinUI.exe` | Pending operations: `%LOCALAPPDATA%\Falah\pending-operations\cashier` (empty on 2026-09-19) | Window "فالح أبو العنبه - نقطة البيع" with the PIN pad; login succeeds |
| 4 | Call Center app (only if shown) | `src\Falah.Client.CallCenterWinUI\bin\x64\Release\net8.0-windows10.0.19041.0\Falah.Client.CallCenterWinUI.exe` | same as step 3 | client of step 2 | `Falah.Client.CallCenterWinUI.exe` | `%LOCALAPPDATA%\Falah\pending-operations\callcenter` | Window "فالح أبو العنبه - مركز الاتصال" |
| 5 | Print agent (only if printing is shown) | `src\Falah.PrintAgent\bin\Release\net8.0\Falah.PrintAgent.exe` | `appsettings.json` beside the exe (identical to the source file) | talks to step 2; prints RAW to the Windows queue `XP-80C` | `Falah.PrintAgent.exe` | Its console window | Log line `print agent started - route Receipt`. **Only after step 2**: if the server is down, the agent exits (code 3) and does not retry. A second agent for the same route refuses to start (code 4) |

Verified on 2026-09-19: steps 1–4 each started and showed the expected window, and closed cleanly.
Step 5 was not started (no printing today).

Only the current Release build of each app exists now. The old audit and probe builds that used to
sit beside it (some closed themselves after drawing every screen) were removed, so there is no
wrong executable to launch.

Shutdown: close the apps → stop the agent (Ctrl+C) → stop the server (Ctrl+C).

---

## 3. Demo data — every suspicious record (nothing changed)

Legend: **KEEP**: fine to show. **HIDE**: leave it, but steer the demo away from it.
**CLOSE**: finish it through the app's own flow. **CLEAN BEFORE DEMO**: needs a decision and an
action before management sees it. **MANUAL REVIEW**: the owner must decide; accounting history is
involved.

| # | Record | Why it exists | Visible in the demo | Verdict |
|---|--------|---------------|---------------------|---------|
| 1 | Business day `day-e5225ab8…`, dated **2026-09-03**, open for 16 days | Opened during early testing, never closed; every order since has been stamped onto it | Dashboard **red** banner "يوم العمل المفتوح تاريخه متأخر 16 يوم"; Control Center | **CLEAN BEFORE DEMO** (close it: §4) |
| 2 | Shift `shift-a08d61e5…` "صباحية", opened 2026-09-05 by the demo cashier | Test shift. All test orders, including #0183/#0184, were taken against it | Shifts screen; close-blocker in Control Center | **CLOSE** (blind close, §4) |
| 3 | Employee **"على"**, ledger balance **−5,300 ج** | Test advances and payments during employee-ledger testing | Employees, Payroll, employee financial report | **MANUAL REVIEW.** Do not open his statement in the demo (**HIDE**) |
| 4 | Supplier **"مورد التدقيق"** with invoices `INV-AUDIT-1` (150 ج, 50 paid) and `INV-TYPO-TEST` (1.5 ج, unpaid) | Created by the purchasing audit | Suppliers, Purchasing, payables aging; dashboard "overdue supplier invoice" banner | **HIDE**, or **MANUAL REVIEW** (settle or deactivate) |
| 5 | Supplier "كرم", invoice 2000158 (1,500 ج, 100 paid, 1,400 owed) | Realistic test purchase | Purchasing | **KEEP** (useful to show payables) |
| 6 | Customers **"عميل التدقيق"** (01000000002) and "على" (0100000000) | Created by the delivery/customer tests | Customers list, delivery lookup | **HIDE** |
| 7 | Pending electronic payments **#0142, #0143** (InstaPay) and **#0149, #0152** (split), 45 ج each, since 2026-09-10 | Test orders whose transfer was never confirmed | Dashboard banner "مدفوعات إلكترونية لم تُؤكَّد: 4"; close-blockers; Orders | **CLEAN BEFORE DEMO.** Cancel each pending payment in the app (reason: "بيانات اختبار"). It is idempotent and audited, and does not restore stock |
| 8 | Delivered cash-on-delivery orders **#0135 (115 ج), #0145 (60 ج), #0173 (185 ج)** in open custody with driver **سيد عبد الله** (360 ج) | Test deliveries that were never settled | Dashboard banner "عهدة سائقين غير مسواة: 3"; Delivery; close-blockers | **MANUAL REVIEW.** They can only be cleared by a driver settlement recording the cash actually received. If no cash was really collected, settling with 0 records a 360 ج driver shortage. Either way it is permanent accounting history |
| 9 | Delivery order **#0148** (60 ج, cash on delivery), never assigned | Test delivery | Delivery board "ready/unassigned" | **HIDE**, or use it live as the "assign driver" step (§7 step 6) |
| 10 | Orders **#0174–#0182** (2026-09-18, 95–340 ج, takeaway cash). #0174–#0179 were created accidentally by UI automation; the origin of #0180–#0182 is unconfirmed | Testing | Order history, 2026-09-18 figures | **HIDE** (don't open 2026-09-18). Deleting them would break the order sequence and accounting |
| 11 | Orders **#0183, #0184** (10 ج each) | Controlled physical print tests | Order history, today's figures | **KEEP** (harmless; they are what printed) |
| 12 | Older orders (57 in total, 2026-09-02 → 2026-09-19) | Development and testing | Reports, history | **KEEP** (they give the reports something to show) |
| 13 | Stock: 51 of 52 inventory items below their minimum | Opening stock was never entered (`OPENING_STOCK_CHECKLIST.md`) | Dashboard banner "51 صنف تحت الحد الأدنى"; Inventory | **HIDE** for the demo; enter opening stock before go-live |
| 14 | One sold product with no known cost | Recipe or cost not entered | Dashboard banner "صنف مباع بلا تكلفة معروفة" | **HIDE** (or enter the cost) |
| 15 | Print routes CallCenter, DeliveryOrder and DriverDispatch never printed on paper | No hardware on those routes yet | Dashboard info "3 مسار طباعة لم يُثبت" | **KEEP** (truthful). Say so if asked |
| 16 | Print jobs: 44, all Succeeded, **0 pending, 0 failed** | — | Printing screen | **KEEP** |
| 17 | Users `user-demo-manager` (permission `all`, PIN 4321) and `user-demo-cashier` (PIN 1234) | Created by `DemoDataSeeder` | Login | **KEEP for the demo**; must be replaced before go-live (§6) |
| 18 | 69 unrevoked login sessions since 2026-09-10 | Every login creates one; they expire after idle time and are never pruned | Not visible | **KEEP** (inert) |
| 19 | Treasury: 77 posted movements (main 10,710 / drawer 11,800 / bank 1,000 / InstaPay 45 ج, gross inflows) | Driven by the test data above | Treasury screen | **KEEP.** Do not "fix" balances by posting fake movements |
| 20 | Audit log: 244 events (3 critical, 11 warning) | Genuine history of the test period | Audit screen | **KEEP** |
| 21 | Backups: 5 verified, latest 2026-09-18 18:58 | Automatic and manual backups | Backup screen; dashboard banner once the latest is older than 24 h | **KEEP.** The server takes a fresh automatic backup within ~90 s of start when the last one is older than 20 h |
| 22 | Databases `FalahIndependentAudit_20260910`, `FalahSecondAudit_20260911`, `FalahRestoreAudit_20260910` on the SQL instance | Earlier independent audits | Not visible in the app | **MANUAL REVIEW** (drop when no longer needed) |

Integrity is clean (read-only checks, 2026-09-19): totals reconcile, there are no duplicate order
numbers, checkout ids or treasury keys, refunds match treasury, there are no orphan rows, employee
running balances are unbroken, and there is no negative stock.

---

## 4. Recommended demo database state

Target: no stale shift, no stale business day, no pending or failed print jobs, no alarm-looking
banners caused by test data, and **no faked balances**.

**Recommended route: rehearse on a copy first.** Restore the latest verified backup into a new
database (for example `FalahDemo`), point the server at it for the demo (environment variable
`ConnectionStrings__Falah="Server=.\SQLEXPRESS;Database=FalahDemo;Trusted_Connection=True;TrustServerCertificate=True;"`),
and run the close-out there. The real `Falah` database stays exactly as it is. This needs your
approval before anyone does it.

Close-out sequence (in the app, in this order). Each step writes real, audited history:

| Step | Action | Removes |
|------|--------|---------|
| a | Cancel the 4 pending electronic payments (#0142, #0143, #0149, #0152), reason "بيانات اختبار" | "مدفوعات إلكترونية لم تُؤكَّد" banner, 4 close-blockers |
| b | Settle سيد عبد الله's custody for #0135, #0145, #0173 with the cash actually held. **Owner decides** whether that is 360 ج or a recorded shortage | "عهدة سائقين" banner, 3 close-blockers |
| c | Blind-close shift `صباحية` (2026-09-05) with the counted drawer cash. Expect a discrepancy against the test-era expected cash, which needs a manager approval | Open shift, 1 close-blocker |
| d | Complete the 3 checklist items and close business day 2026-09-03 | Red "يوم العمل المفتوح … 16 يوم" banner |
| e | Open a new business day and a new shift for today | Fresh start for the live order |

What stays visible and why:
- **Stock below minimum (51 items):** only real opening stock fixes this. Do not show Inventory's
  low-stock view or dwell on that banner.
- **"على" −5,300 ج and the audit supplier:** removing them means posting adjusting entries. That is
  an owner decision, not demo preparation. Steer away (HIDE).
- **Accidental orders #0174–#0182:** part of the numbered order sequence and the cash history.
  Show dates other than 2026-09-18.

If the close-out is not done, the demo still works. Just open the Dashboard on "هذا الشهر" and
explain that the red banner is test data.

---

## 5. SQL Server stability (the only known cause of slow screens)

Cause (measured): when Windows runs short of memory it squeezes SQL Server Express (seen: 337 MB
committed, only 8 MB left for query memory grants). Any query that sorts wide text rows then waits
out SQL Server's ~25 s memory-grant timeout (wait type `RESOURCE_SEMAPHORE`). The menu has been
fixed to need the 1 MB minimum; the audit log and some reports can still stall while the squeeze
lasts. **Raising the client timeout would only hide this, so it has not been done.**

Demo-day actions (no configuration change):
- Close heavy apps (§1). The pre-demo check fails below 1.5 GB free.
- Run `tools\pre-demo-check.ps1`. It checks the SQL service, free memory, the query-memory pool
  (≥ 64 MB), `RESOURCE_SEMAPHORE` waiters (must be 0), and times `/api/menu` (< 2 s),
  `/api/audit/events`, owner dashboard, daily statement, sales, orders and treasury (< 3 s each).
- Restart `MSSQL$SQLEXPRESS` **only if** the memory rows fail with apps already closed. A
  restart clears the squeeze; the first queries afterwards are slightly slower while the cache
  warms.

Measured 2026-09-19 with the Release server: every timed endpoint was 0.02–0.19 s, the query pool
had 546 MB available with 0 waiters, and free memory was ~1 GB with the heavy apps open.

Recommended configuration (go-live, owner/administrator decision, **not applied**):
`min server memory (MB)` = **1024** (currently 16), `max server memory (MB)` = **2048** (currently
unlimited; on this 8 GB PC it leaves room for Windows and the apps; Express caps its data cache at
~1.4 GB anyway), plus the latest SQL Server 2022 cumulative update (the instance is RTM 16.0.1000.6).

---

## 6. Credentials and security

| Item | Safe for a local demo | Must change before go-live |
|------|-----------------------|----------------------------|
| Demo users `user-demo-manager` / PIN **4321** (permission `all`) and `user-demo-cashier` / PIN **1234** | Yes | Yes: create real users with their own PINs, deactivate both demo users |
| PINs hardcoded in `DemoDataSeeder.cs`, which seeds them on any **empty** database at server start | Yes (the database is not empty) | Yes: decide how a fresh install is seeded (`GO_LIVE_CLEANUP_CHECKLIST.md` §12.3) |
| Print agent config (tracked in git) holds `ReportedByUserId: user-demo-manager` and `Pin: 4321` in plain text | Yes | Yes: dedicated low-permission print account, with its PIN only on each station (§12.2) |
| Demo cashier holds `orders_refund` (refunds without a manager) and `orders_comp` (comps still require a real manager approval since 2026-09-19) | Yes | Review role permissions with the owner |
| Server listens on `http://localhost:5151` only, plain HTTP | Yes (nothing leaves this PC) | Multi-device phase: bind to the LAN deliberately and decide on TLS |
| `tools\pre-demo-check.ps1` takes the PIN as a parameter (not stored in a file; it may remain in PowerShell history) | Yes | Use a real account when running it |
| This document names the demo PINs | Yes | Remove once the demo users are replaced |

Already sound: every request's user must match its session (403 otherwise); sessions expire after
idle time; approvals are persisted and single-use; every money/treasury/employee/delivery/inventory/
settings action checks a named permission; login is rate-limited; Swagger is off in Production.

---

## 7. Rehearsal (≈15 minutes, before the manager arrives)

Log in as the manager.

| # | Step | Expected visible result | Avoid (test data) | If slow or empty |
|---|------|-------------------------|-------------------|------------------|
| 1 | **Login**: PIN pad → PIN | Shell with نقطة البيع, الطلبات والتوصيل, الخزينة, التقارير…; header shows "فالح أبو العنبه" | Typing the PIN where it is filmed | Server not running → §2 step 2 |
| 2 | **Dashboard**: لوحة المتابعة | Sales, cash position, attention list, printing health | The red "open day 16 days" banner unless the close-out (§4) was done; low-stock banner | Switch the period to "هذا الشهر" |
| 3 | **Cashier order**: نقطة البيع → a product → الدفع → cash | New order number, change due, cart cleared; sizes listed small → large | Discounts and comps (they need a manager PIN live) | Menu slow → run the pre-demo check (§5) |
| 4 | **Order history**: الطلبات والتوصيل → الطلبات | New order at the top; Arabic status and payment method | Opening #0174–#0182 or 2026-09-18 | Refresh |
| 5 | **Delivery**: new delivery order at the till → التوصيل والسائقون | Order on the board with its amount to collect | Old delivered orders in custody (#0135, #0145, #0173) | Use #0148 (unassigned) |
| 6 | **Driver assignment**: assign the order to محمود السائق | Status changes to "with driver"; custody opened | Reassignment; failure/return flows | — |
| 7 | **Treasury**: الخزينة | Balances per account; movements with Arabic types | Withdrawals/transfers (they post real money movements) | — |
| 8 | **Employee**: الموظفون | Employee list; statement with Arabic entry types and a running balance | "على"'s statement (−5,300 ج) | Show the list only |
| 9 | **Purchasing / inventory**: المشتريات, المخزون | Invoice from "كرم", stock movements | "مورد التدقيق", `INV-TYPO-TEST`, the low-stock filter | — |
| 10 | **Reports**: التقارير | Sales, categories, top items, important actions (with names, not ids) | — | Change the period |
| 11 | **Daily statement**: كشف اليوم | Sales, payments, expenses, cash position for a date | 2026-09-18 | Pick 2026-09-14 |
| 12 | **Audit**: سجل المراجعة | Events by category and severity | — | Filter to one category (a smaller sort) |
| 13 | **Settings / system health**: المزيد → الإعدادات, الطباعة | Restaurant details, menu link `https://fale7-res.vercel.app/`, print queues empty | Editing settings live | — |

Not in the rehearsal: receipt printing (hardware banding unresolved), business-day close/reopen,
backup restore, user permission editing.

---

## 8. Could embarrass us in front of management

1. **Resolved in the final second pass:** both WinUI login headings and window titles now use
   **فالح أبو العنبه**. See [FINAL_CLOSEOUT_REVIEW.md](FINAL_CLOSEOUT_REVIEW.md) for the current
   receipt previews, verification evidence and physical-print approval gate.
2. The red dashboard banner for a business day that is 16 days behind, unless §4 is done.
3. "51 items below minimum" on the dashboard (opening stock never entered).
4. Test names on screen: "مورد التدقيق", "INV-TYPO-TEST", "عميل التدقيق", and "على" at −5,300 ج.
5. A slow screen if heavy apps are left open (§1, §5).
6. The Call Center app was verified to start and construct its screens, but was not rehearsed.
