# Owner task — complete verification, project explanation and all active UI

> This is the original task scope. Use CODEX_TAKEOVER_20261108.md for the current checkpoint; numeric results and draft statuses below are historical unless revalidated. Older handoffs were archived; see archive/README.md.


Repository: E:\falah-restaurant\Falah-Restaurant-CSharp
Owner request, 2026-11-07: verify that the system actually works, explain everything in the project in understandable Arabic, and improve the entire active user interface. IMPLEMENT and VERIFY; do not stop at an audit or a plan.

## 1. Read the current state and preserve it

Read CLAUDE.md, docs/CODEX_TAKEOVER_20261108.md, All Issues/REMEDIATION/state.json and relevant issue/batch files. Inspect the actual working tree and diffs; earlier summaries and test counts are historical evidence, not proof of today's tree.

The last confirmed pure Domain run was 449/449 in B37-advance-types-green.log. The handoff originally said 443 before B37; use the final B37 log. This is a TEST count, not an issue count. The draft SQL/restore changes in B34/B36 are explicitly unaccepted pending verification.

Keep all owner changes. No commit, push, reset, clean, stash, destructive checkout, deployment or service installation is authorized. Do not edit existing migrations or model snapshots manually. Avoid new dependencies unless necessary for the agreed work.

Controlling requirement: if you cannot fully inspect and meaningfully verify an issue, leave that issue unconfirmed. Do not implement further speculative fixes or mark an untested change FIXED. Existing unverified drafts must be tested and accepted, repaired, or precisely excluded without overwriting owner work.

## 2. Confirm the execution environment once

Check execution identity, .NET SDK, SQL test connectivity, DPAPI and native WinUI automation/capture capability. Previous Codex execution used codexsandboxoffline; SQL encryption initialization and CurrentUser DPAPI failed, and its policy forbade escalation. Do not assume Claude has the same restriction; test it once.

If a capability is unavailable, record the exact error, affected issue IDs, command and required access. Continue independently verifiable work. Do not repeatedly retry the same rejected permission, disable encryption, give the sandbox SQL administrator access, delete pending files, or weaken certificate validation to get green results.

Use the intended normal Windows account for DPAPI/SQL acceptance when authorized. Follow approval prompts where required. Do not change system security or account grants as an implicit part of this task.

## 3. Isolate all execution from the restaurant's real data

NEVER connect to, migrate, seed, restore, modify or delete the production Falah database. Use FalahApiFactory's unique FalahTest_<guid> databases only, with data/log files under E:\SqlData\Tests and backups under E:\SqlData\Tests\Backups.

Never start the normal server with an unchecked default connection string. A real-client UI session needs an explicitly isolated test backend and a process-scoped test FALAH_SERVER_URL. Prove the target database name and URL before allowing writes.

Isolate native clients' retained-request storage as well. Do not read, clear or replay a real station's pending operations. Existing SCREEN_LAYOUT_PROBE fixtures bypass station storage and reject network access; they prove layout only. Do not assume changing LOCALAPPDATA alone redirects Windows known-folder APIs. Use a verified test fixture/store override before exercising real UI writes.

Use seeded test roles and test data. No production credentials, customer data or secrets in logs/screenshots/reports.

## 4. Discover and explain what actually exists

Build a concise inventory from current source, routes, navigation, project references, launch/deployment scripts and executed behavior. Include every project, every active screen/tab/dialog, major API/service responsibilities, data ownership, permissions, agents and operational dependencies.

At minimum inspect these areas, and explicitly label anything absent or incomplete:

- Cashier ordering, menu/products/sizes, prices/taxes/discounts, checkout, cash/electronic/split payment, refunds, voids and retry recovery.
- Call Center, customers/addresses, pickup, delivery dispatch/results, driver custody and settlements.
- Business days, shifts, cash count, treasury, transfers, expenses, reconciliation and Control Center.
- Inventory, units, recipes/prep recipes, yield, counts, waste, costing, suppliers and purchasing.
- Employees, attendance, wages, advances, deductions, payroll and salary accrual.
- Owner dashboard, financial/operational reports, audit/security, users/roles and settings.
- Printing routes, agent startup/recovery, reprints, drawer integration, backups/restore and deployment tooling.

The explanation must answer: what it does, who uses it, its normal workflow, where it lives, its dependencies, what was tested, and what remains incomplete. Distinguish implemented, reachable, tested, broken, incomplete and blocked. File presence and a successful build do not prove a feature works.

Cover both active WinUI clients fully. Classify the older WPF project accurately: inspect whether anything launches/deploys it; do not silently count a reference-only client as a verified shipped UI or spend the active-client redesign budget on archived screens.

## 5. Establish real baselines, then fix documented failures

Start with Domain, client and SQL integration tests by project path. Prioritize B34 employee locks, B36 backup/maintenance, the DPAPI durability failures, and existing known high-priority defects. Read the handoff for exact files and commands.

Do not weaken assertions, skip failing tests, replace real SQL with an in-memory substitute, or rewrite financial rules merely to make the suite green. A fixture correction must be justified against the session/role contract, never by weakening the server.

For a defect: reproduce the failure, fix the smallest complete cause, run its meaningful tests and relevant neighboring cases, then record evidence. A blocked test is not a product failure or a pass. Preserve logs and TRX results.

One writer and one build/test chain per output tree. Reuse existing briefs; no analyst/implementer/reviewer agent loops, repeated whole-repository audits or excessive status chatter. Run targeted tests during work and the complete current-source suite at the appropriate final milestone, not after every cosmetic edit.

## 6. Verify end-to-end business workflows

Against disposable test data, cover the actual paths from each client through the server and persisted result. Include retries after lost responses, double clicks, app restart recovery, refused permissions, used/expired approvals, relevant concurrency, UTC/Cairo date boundaries and current known regressions.

The workflow matrix must include at least:

1. Login and each relevant role, session expiry/deactivation and unauthorized reads/writes.
2. Open business day/shift, place and pay orders using supported payment modes, confirm/cancel pending electronic payments, refund/void eligible orders, and close/reconcile.
3. Call Center customer lookup/selection, pickup, delivery assignment/result, custody collection and settlement.
4. Purchase receipt, stock deduction, waste, physical count and recipe/cost effects.
5. Attendance and overnight check-out, employee ledger writes, salary accrual and payroll lifecycle/payment.
6. Printing claim/send/report/restart recovery and safe reprint authorization.
7. Backup creation/verification, restore gates and maintenance drain; actual restore only on a positively identified disposable FalahTest database.
8. Daily/period reports matching persisted financial facts and permissions.

If a business decision is unresolved (including COD void permissions, overlap between standalone settlements and payroll, tax/proration or visibility policy), preserve behavior and ask only the necessary decision. Continue other work. Do not invent restaurant policy.

Software test transports can verify submission and recovery logic. Paper output, drawer movement, real device/service installation, power-cut behavior and production HTTPS trust require separate on-site evidence; keep these explicitly pending.

## 7. Audit and improve the whole active UI in the real native apps

First enumerate every reachable screen, tab and modal in both clients, including permission-specific routes and recovered-operation views. Capture baseline screenshots and record concrete visual/usability defects, then implement the fixes screen by screen. Use existing native probes and populated fixtures where suitable; browser screenshots or view-model tests alone do not verify WinUI desktop layout.

Keep the existing restaurant identity and workflows. Produce one cohesive visual system: typography, spacing, headers, surfaces, tables, inputs, buttons, dialogs, status indicators, empty/loading/error states, selection and focus behavior. A clear, quick restaurant workflow matters more than decorative effects. Keep technical implementation details out of operator-facing UI.

Check and fix:

- Arabic RTL layout, readable Arabic fonts, mixed phone/order-number text, Cairo dates, EGP/piastre display and long Arabic names.
- Light and Dark themes, live theme changes, semantic colors and text contrast of at least 4.5:1 for normal text; selected, focused, disabled and error states.
- Resizing, scrolling, clipping/overlap, content density, stable column widths and reachable primary actions.
- Keyboard/touch operation, focus order, shortcuts blocked beneath modals, no accidental default destructive action.
- Validation next to the right field, useful Arabic messages, busy/double-submit protection, and understandable permission-disabled or unresolved-request states.
- Empty, populated, loading, failed-load, long-list and long-content states; all functional tabs and nested dialogs.
- No placeholder success, dead button, untranslated enum, stale detail, incorrect totals or status label.

Minimum display coverage: every active screen in Light and Dark at 1366x768 and 1920x1080. Check the shared shell, cashier/checkout, Call Center, dense tables and complex dialogs at 125% and 150% scaling too; expand coverage where a defect occurs. Use meaningful populated data. Keep a coverage matrix rather than silently omitting a hard screen.

After each UI group, rebuild the affected native app and actually reopen/interact with the changed views. Keep before/after screenshots for changed screens and evidence for the final coverage matrix. Verify essential actions still reach the correct backend and cannot bypass permissions or approval rules.

If native execution/capture is unavailable, do not redesign layouts from guesswork or report the UI complete. Record the blocked screens/capability and continue only independently verifiable work.

## 8. Deliverables and completion criteria

Use existing issue/state tracking rather than creating another sprawling backlog. Add these concise reports under docs/:

1. PROJECT_OVERVIEW_AR.md — understandable Arabic explanation and module/screen inventory with source paths and real status.
2. UI_VERIFICATION_AND_CHANGES.md — complete screen coverage matrix, concrete changes, before/after screenshot paths, themes/resolutions/states checked, and blocked UI checks.
3. FINAL_VERIFICATION_REPORT.md — exact commands, current test totals/results, logs/TRX, native builds, workflow evidence, known failures, unaccepted drafts, permission blockers, policy decisions and on-site tasks.

Keep reports synchronized with All Issues/REMEDIATION/state.json and specific issue evidence. Do not turn a successful test count into a claimed count of fixed defects.

Complete only when the current-source verification is green for the accepted scope, changed UI has actually run, and every remaining gap has an explicit honest status. If blockers prevent full completion, say exactly what works, what does not, what was not tested, and what is needed next. Never say 'everything works' while a required check is blocked.

Begin now with the handoff and actual environment check, move promptly into execution, and report concise Arabic progress. The final Arabic response should lead with delivered behavior and the remaining limitations, then link the three reports and representative UI screenshots. No plan-only conclusion.