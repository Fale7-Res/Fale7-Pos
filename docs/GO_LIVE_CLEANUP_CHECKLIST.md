# Go-live data cleanup checklist — Falah Restaurant OS

**Nothing in this list has been deleted, reset or modified automatically.** Every item below is a
decision for the owner to make and execute deliberately, through the app's own screens, before the
first real trading day. This document only records what is currently in the `Falah` database so the
decisions can be made from facts rather than memory.

Counts were read (read-only) from `.\SQLEXPRESS` / `Falah` on **2026-09-18**.

---

## 1. Users — ACTION REQUIRED

| Id | Name | Role |
|---|---|---|
| `user-demo-cashier` | كاشير التجربة | cashier |
| `user-demo-manager` | مدير المحل | manager |

These two demo accounts are the **only** users in the system, so they cannot simply be deleted —
the app would have nobody to sign in as, and every historical order, audit line and treasury
movement references them as its actor.

**Do this in order:**
1. Create the real staff accounts (owner, managers, cashiers) with their own PINs.
2. Sign in as a real manager and confirm the permissions are right.
3. Deactivate — do **not** delete — `user-demo-cashier` and `user-demo-manager`, so their historical
   references stay intact and their PINs stop working.
4. Change any PIN that was shared during testing (the demo manager PIN `4321` is known).

## 2. Stale business day — ACTION REQUIRED

One business day is open: `2026-09-03`, opened 2026-09-03 19:01 UTC — roughly **15 days stale**.

Trading cannot start cleanly on top of it: today's figures would be posted into a September-3
business day, and the daily statement, shift reconciliation and treasury all key off it.

**Close this business day through the Control Centre before the first real day**, working through
whatever blockers it reports. Do not delete the row.

## 3. Open shift — ACTION REQUIRED

One shift is open, opened 2026-09-05 16:34 UTC, on business date `2026-09-05`.

It must be **closed through the normal blind close** (count the drawer, enter the counted figure,
give a reason for any difference). Closing it is a prerequisite for closing the stale business day
above. Do not delete the row or force the status in SQL — the close is what writes the handover and
reconciliation treasury movements.

## 4. Test employee balance — DECISION REQUIRED

One employee exists: **على** (`emp-emp-0b556eee...`), active, with **7 ledger entries** and a
current running balance of **−5300.00 ج**.

The negative balance means the test payments and withdrawals ran ahead of the recorded earnings. It
is test data, not a real debt. Because the Employee Ledger is append-only and authoritative, the
correct way to bring it to a true starting position is an **explicit adjusting entry with a reason**
("تسوية رصيد افتتاحي قبل التشغيل") — not a deletion. Decide the real opening balance first, then
post the adjustment.

## 5. Test suppliers and invoices — DECISION REQUIRED

- Suppliers: **2** (`ماجد الجزارة`, `خضار`)
- Purchases: **3**
- Supplier ledger entries follow those purchases.

If these are real suppliers, keep them and simply verify their opening balances. If they were
invented for testing, deactivate the supplier rather than deleting it — the purchases behind them
have already moved ingredient cost bases, and deleting the supplier would orphan that history.

## 6. Pending print jobs — ACTION REQUIRED

**12 of 24 print jobs are still `Pending`.** These will be picked up and printed the moment a print
agent connects to a live printer, producing a burst of stale tickets for orders from testing.

**Before connecting the real printers**, clear the queue through the print queue screen (cancel the
pending jobs), or connect the agent only after the queue is drained. Do not delete the rows
directly — cancelling through the screen keeps the audit trail.

## 7. Historical test orders — DECISION REQUIRED

- Orders: **46** (order numbers up to `#0173`), plus **1** refund
- Deliveries: **4**, driver custodies: **4**, customers: **3**

These are test orders. They are also the source of every figure the reports currently show.

**Recommended: keep them and let the order sequence continue.** Reports are all business-day scoped,
so once the stale day is closed these orders simply sit behind the go-live date and stop appearing in
day-to-day figures. Deleting them would break the treasury, inventory and employee ledgers that
reference them.

If the owner specifically wants order numbering to restart at `#0001`, that is a separate, deliberate
decision that requires resetting `OrderSequence` **and** accepting that the historical orders'
numbers will collide with new ones. Not recommended.

## 8. Inventory (stock) movements — DECISION REQUIRED

- Stock movements: **162**
- Inventory items: **52**

The on-hand quantities these produce are test quantities. Rather than deleting movements, **do a
full stocktake (جرد) on the day before go-live**: the count writes the correcting movements and
leaves an auditable trail of what the opening stock actually was. See
[OPENING_STOCK_CHECKLIST.md](OPENING_STOCK_CHECKLIST.md).

## 9. Treasury movements — DECISION REQUIRED

**66 movements.** Current balances: Main 1660.00 ج · Drawer 6370.00 ج · Bank 1000.00 ج ·
InstaPay 40.00 ج · Wallet 0.00 ج. Also outstanding: **360.00 ج** cash with drivers (1 driver,
unsettled) and **135.00 ج** in unconfirmed electronic transfers across 4 orders.

**Do this in order:**
1. Settle the driver custody so no cash is recorded as being with a driver who is not holding it.
2. Confirm or cancel the 4 pending electronic payments.
3. Close the open shift (item 3) — this moves the drawer through its handover.
4. Count the real cash in each account and post a **deposit or withdrawal with a reason** to bring
   each account to its true opening figure. Do not edit balances in SQL; there is no such operation,
   and the balance is derived from the movements.

## 10. Audit log and backups — NO ACTION

- Audit events: **240**
- Backup records: **4** (automatic backups are running, most recent verified valid)

The audit log is deliberately append-only and must not be trimmed. Leave both alone.

## 11. Products and menu — VERIFY ONLY

**129 products**, 52 inventory items, recipes and price rules attached.

Nothing to clean, but before go-live verify: prices are current, availability flags are right, and
every product that should deplete stock has a recipe (the dashboard's food-cost card reports how
many sold lines had a known cost).

---

## Summary

| # | Item | Verdict |
|---|---|---|
| 1 | Demo users | **Action** — create real users, then deactivate demo ones |
| 2 | Stale business day (2026-09-03) | **Action** — close it |
| 3 | Open shift (2026-09-05) | **Action** — blind close it |
| 4 | Test employee balance (−5300.00 ج) | **Decision** — post an adjusting entry |
| 5 | Test suppliers / invoices | **Decision** — keep or deactivate |
| 6 | Pending print jobs | **Done** — drained; 0 pending on every route (2026-09-19) |
| 7 | 46 historical test orders | **Decision** — recommended: keep |
| 8 | 162 stock movements | **Decision** — recommended: stocktake, don't delete |
| 9 | 66 treasury movements | **Decision** — settle, confirm, then adjust to true balances |
| 10 | Audit log, backups | **None** — leave alone |
| 11 | Products / menu | **Verify only** |
| 12 | System / configuration (below) | **Action** — see section 12 |

---

## 12. System and configuration — found in the 2026-09-19 stabilization audit

Counts in sections 1–11 above were taken on an earlier date. On 2026-09-19 the database held 57
orders, 236 stock movements and 77 treasury movements; the integrity checks all passed (see
`DEMO_READINESS.md` §2).

| # | Item | Why it matters | Action before go-live |
|---|------|----------------|-----------------------|
| 12.1 | **SQL Server memory floor** | SQL Server Express has `min server memory = 16 MB`. Under Windows memory pressure it shrank to 337 MB committed with only 8 MB for query grants, and queries that sort wide rows stalled for the ~25 s grant timeout (the "/api/menu 25 s" fault). | On the restaurant PC set `min server memory (MB)` to 1024 and `max server memory (MB)` to about 2048 on an 8 GB machine (via `sp_configure`, by whoever administers that PC), and install the latest SQL Server 2022 cumulative update (the instance is RTM 16.0.1000.6). Afterwards run `tools/pre-demo-check.ps1`; it must PASS. |
| 12.2 | **Print agent credentials** | `src/Falah.PrintAgent/appsettings.json` (tracked in git) logs every station in as `user-demo-manager` with PIN `4321` — a full-permission account whose PIN sits in plain text on each print station. | Create a dedicated low-permission print account, put its PIN only in each station's deployed config, and remove the demo PIN from source. |
| 12.3 | **Demo data seeder runs in every environment** | `DemoDataSeeder.SeedIfEmptyAsync` runs at every server start. On an **empty** database (a fresh install) it creates demo products and the two demo users with PINs 1234/4321. | Decide: install from a restored backup / pre-seeded DB, or gate the seeder behind an explicit setting. Either way, change both demo PINs on day one. |
| 12.4 | **Print agent start order** | If the server is not up when an agent starts, the agent exits (code 3) and does not retry. | Start agents after the server, or run them as services with restart-on-failure. |
| 12.5 | **Printer USB selective suspend** | The XP-80C drops to "offline" after idle. | Disable USB selective suspend for that port or in the power plan on the restaurant PC. |
| 12.6 | **Backup path is machine-specific** | `Backup:DefaultDirectory = E:/falah-restaurant/Backups` in `src/Falah.Server/appsettings.json`. | Set the restaurant PC's real backup folder (ideally a second disk) in the deployed config. |
| 12.7 | **Image-mode print settings** | Were only in the deployed agent config; any clean rebuild silently reverted to character mode (unreadable Arabic). **Fixed 2026-09-19:** now in the source config. | Verify each station's deployed config has `RenderAsImage: true`, `ImageWidthDots: 576`, `ImageMarginDots: 24`. |
| 12.8 | **Uncommitted source** | About 190 modified and 43 new files are not in git. | Commit before any deployment build. |
| 12.9 | **Session table growth** | Expired sessions are never pruned (harmless; they cannot be used). | Optional periodic clean-up. |
| 12.10 | **Two agents, one route, two machines** | The single-instance guard is per machine. Two stations configured for the same route on different PCs would both print every ticket. | Configure exactly one station per route. |

