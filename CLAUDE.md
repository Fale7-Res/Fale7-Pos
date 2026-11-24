# Falah Restaurant POS — working notes

Restaurant point of sale and back office for one restaurant (Arabic UI, Cairo time, EGP). A .NET 8
server on SQL Server Express, two WinUI 3 desktop clients and a print agent, all on the restaurant's
LAN. Owner-facing text and server error messages are Arabic; code, comments and docs are English.

## Map

| Project | What lives there |
|---|---|
| `src/Falah.Domain` | Pure rules, no I/O: `Money` (integer piastres), `BusinessClock` (Cairo), authorization policy and approvals, PIN hashing, checkout / refund / delivery / payroll / inventory / sales-report rules |
| `src/Falah.Application` | Request and DTO records shared by server and clients (the wire contract) |
| `src/Falah.Infrastructure` | EF Core `FalahDbContext`, migrations, one handler class per command, query services for reads, guards (`ApprovalPinAttemptGuard`, `LoginAttemptGuard`, `PinChoiceGuard`) |
| `src/Falah.Server` | Minimal-API host: `Program.cs` (every endpoint, DI, startup migrate + demo seed), `AuthenticationMiddleware` (session token = identity) |
| `src/Falah.Client.CashierWinUI` | Main WinUI 3 client (till, Control Center, owner dashboard, reports, back office). MVVM (CommunityToolkit), x64, unpackaged |
| `src/Falah.Client.CallCenterWinUI` | Call-center WinUI 3 client (order taking, delivery) |
| `src/Falah.Client.Cashier` | Older WPF client kept as reference; not updated by remediation work |
| `src/Falah.PrintAgent` | Per-station worker: logs in, claims print jobs for its route, renders ESC/POS (raster for Arabic), reports results |
| `tests/Falah.Domain.Tests` | xUnit, pure, ~1 s |
| `tests/Falah.Client.Tests` | xUnit view-model tests for the WinUI clients (no UI), ~1 s |
| `tests/Falah.Server.IntegrationTests` | xUnit + `WebApplicationFactory` against a real SQL Server database per test |

## Commands (PowerShell, repo root)

```powershell
dotnet build Falah.sln                                    # 0 errors expected; NU1900 warnings = NuGet feed offline
dotnet build src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj -p:Platform=x64
dotnet test tests/Falah.Domain.Tests
dotnet test tests/Falah.Client.Tests
dotnet test tests/Falah.Server.IntegrationTests --filter "FullyQualifiedName~RefundDateBasisTests"
```

- The full integration suite takes about 1.5–2.5 hours on this laptop (last full run 2026-11-22:
  1062/1066 in 2 h 9 m; the 4 failures were reprint tests whose users lacked the new `print_reprint`
  permission, green after the test fix). It needs the local
  `.\SQLEXPRESS` instance and space on `E:\SqlData\Tests`. Run it at milestones, not after every
  change, and never two runs at once in the same tree (they share bin/obj).
- This machine is a laptop that sleeps on lid/button even with a keep-awake request; a run that
  spans a sleep shows SQL timeouts in test setup. Re-run the affected class before calling it a
  code failure.
- To run a long suite while continuing to work, copy the tree without `bin`/`obj`/`.git` into the
  session scratchpad and run it there. Publish the WinUI clients from the repository itself: a
  long scratchpad path pushes Windows App SDK intermediates past the 260-character limit.
- Migrations: `dotnet ef` 8.0.11 is pinned in `.config/dotnet-tools.json`. Generate with
  `$env:ConnectionStrings__Falah` pointed at a non-existent database (e.g.
  `FalahDesignTime_DoNotConnect`); `migrations add` builds the host but never connects.

## Rules that are not optional

**Database.** Never connect to, migrate, seed or delete the real `Falah` database. Integration
tests get `FalahTest_<guid>` from `FalahApiFactory` (created in the constructor, dropped on dispose,
files under `E:\SqlData\Tests`); automatic backup is forced off in test hosts. The server runs
`Database.Migrate()` at startup, so every migration is a production deployment event: additive,
reviewed, snapshot diff checked, and applied only after a verified backup (see `docs/DEPLOYMENT.md`).
Never edit an existing migration or the snapshot by hand.

**Git.** The working tree carries a large amount of uncommitted owner work and untracked migrations.
No `reset --hard`, `clean`, `stash`, destructive checkout, commit, push or deploy unless the owner
asks. Read a file's current diff before editing it.

**Money.** Integer piastres everywhere (`long`, `Money`); 1 ج = 100. No floating point in amounts.

**Time.** Instants are stored in UTC; Cairo dates only when grouping (`BusinessClock`). Three report
bases — calendar day, business day, shift — defined in `docs/METRIC_DEFINITIONS.md`. A sale counts
in the period it was made and a refund in the period it was made; closed periods never move.

**Identity.** The session token is the identity (`AuthenticationMiddleware`). Request bodies still
carry `actorUserId`-style fields; the middleware rejects any that differ from the session. Read the
actor from `HttpContext` for anything new. Permissions are plain strings (`all` grants everything),
checked server-side in handlers (`AuthorizationPolicy`), never only in a client.

**Manager approvals.** Issued by `/api/approvals/issue` from a manager's PIN typed at the till;
single-use (concurrency token on `Consumed`), bound to one action, spent only by the user whose
session asked for it, and only within `Approvals:LifetimeMinutes`.

**PINs.** PBKDF2 per user. Login is PIN-only, so PINs are unique among active users; every place a
PIN is checked or chosen is guarded (`LoginAttemptGuard`, `ApprovalPinAttemptGuard`,
`PinChoiceGuard`). Never log or audit a PIN value.

**Commands.** Every write carries a request id and replays idempotently (replay check first, before
any validation). Clients keep an in-flight money command on disk until its outcome is known
(`RetainedRequest`, `CashierViewModel.Durability`).

**Business policy.** VAT treatment, payroll proration, refund policy, permission grants and hardware
behaviour are the owner's or the accountant's decisions. Record the question in
`docs/ASSESSMENT.md` and the pending decisions in `All Issues/REMEDIATION/state.json`; leave the behaviour unchanged.

## How work is tracked

- `All Issues/01_MASTER_ISSUE_REGISTRY.md` and its issue files hold defect status and evidence;
  `All Issues/REMEDIATION/state.json` holds the remediation state and the owner's pending questions.
  Pre-2026-11-08 documentation is in `docs/archive/`; documents removed on 2026-11-19 (old task
  prompts, takeover checkpoint, superseded verification reports) are in git history before that date.
- Defects: failing test first, watch it fail for the right reason, fix, run the class and its
  neighbours. "Done" means tests ran green and the evidence is written down; a screen is done when
  it has been run, not when it compiles; hardware behaviour is done only after an on-site test
  (`docs/DAILY_OPERATIONS_VERIFICATION.md`).
- Other references: `docs/LAN_DEPLOYMENT.md` (multi-PC, HTTPS), `docs/PRINT_AGENT.md`,
  `docs/DEPLOYMENT.md`, `docs/GO_LIVE_CLEANUP_CHECKLIST.md`.

## Current checkpoint

Release candidate `68BA663DBB1D7804` (commit `a18556c`, was `14f1726` before the 2026-11-24 history
rewrite) is certified in `docs/GO_LIVE_ACCEPTANCE_20261114.md` §6: software GO (conditional),
production NO-GO until the on-site gates and the owner's pre-go-live decisions. Latest (2026-11-24):
GitHub `main` = this project (merge `961ce29`); full suite 1140/1140 on `25eec06`; installer
`Falah-Setup-8D86E5EB054CFE52.exe` (old→new commit IDs in `docs/UI_OWNER_REQUESTS.md`). Read that section, `docs/UI_OWNER_REQUESTS.md`
and `state.json` before resuming. SQL/DPAPI capability depends on the execution account:
owner-account passes do not establish sandbox-account access. Preserve the evidence limits.

## Documentation navigation

Start with the root `README.md` to find the active references. Update the existing reference that
owns a topic (owner requests, acceptance, deployment, the issue file) rather than creating a new
progress/handoff file for every change. Generated logs and screenshots go under `artifacts/`
(ignored) and are linked from the evidence. Imported audits are historical references; do not
read the entire archive or reopen old findings without checking the current registry and code.
