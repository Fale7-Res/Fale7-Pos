# Falah Abu El Enab — Restaurant POS

Point of sale and back office for one restaurant, running entirely on the restaurant's LAN.
Arabic interface, Cairo time, Egyptian pounds held as integer piastres.

> **للمالك:** النظام فيه سيرفر واحد وبرنامج كاشير وإدارة وبرنامج كول سنتر وبرنامج طباعة لكل طابعة.
> حالة التشغيل والقرار النهائي في [قبول التشغيل §٦](docs/GO_LIVE_ACCEPTANCE_20261114.md)، والتركيب في
> [دليل التركيب](docs/DEPLOYMENT.md)، وطلباتك وما تم فيها في [طلبات المالك](docs/UI_OWNER_REQUESTS.md).

| | |
|---|---|
| **Release candidate** | `68BA663DBB1D7804` (commit `14f1726`, 2026-11-19), not yet installed |
| **Verdict** | Software GO (conditional). Production NO-GO until the on-site acceptance tests and the owner's pre-go-live decisions are done ([acceptance §6](docs/GO_LIVE_ACCEPTANCE_20261114.md)) |
| **Tests** | Domain 564 · Client 441 · Server integration 1053 (last full run 1051/1053, both failures environmental and green on re-run) |

## What it does

- **Till:** takeaway, pickup and delivery orders; cash, card, InstaPay, wallet and split payments; discounts and comps with manager approval above a limit; refunds and voids; reprints; open and blind-close shifts; expenses, cash in/out and safe drops from the drawer.
- **Call center:** customer lookup, delivery orders with zone fees, driver dispatch.
- **Delivery:** driver custody, cash-on-delivery settlement, failed and returned deliveries, collection shortfalls classified with evidence.
- **Back office:** menu and recipes, inventory and stocktakes, purchasing and suppliers, employees, attendance, payroll and the employee ledger, treasury accounts (drawer, main safe, bank, InstaPay).
- **Owner reporting:** daily statement, business-day close and reconciliation, cash flow per account, owner dashboard and financial report. Every figure is defined in [METRIC_DEFINITIONS](docs/METRIC_DEFINITIONS.md).
- **Printing:** a print agent per station claims the jobs for its route (receipt, kitchen, grill, charcoal, delivery, dispatch) and prints Arabic tickets as ESC/POS raster.

## Architecture

```
  Cashier PC(s)          Call-center PC          Printer stations
  CashierWinUI           CallCenterWinUI         PrintAgent (one per route)
        \                      |                      /
         \_____________ HTTP(S) on the LAN ___________/
                               |
                         Falah.Server  (ASP.NET Core minimal API, Windows service)
                               |
                     SQL Server Express  (.\SQLEXPRESS, database "Falah")
```

- The server owns every rule. Clients show its Arabic messages and never decide policy on their own.
- The session token is the identity. Permissions are checked on the server, and manager approvals are single-use and tied to one action.
- Every write carries a request id and replays idempotently. The tills keep an unanswered money command on disk until its outcome is known, so a power cut cannot double a sale.
- Instants are stored in UTC. Reports group by calendar day, business day or shift (Cairo), and closed periods never move.

## Repository layout

| Path | Contents |
|---|---|
| [src/Falah.Domain](src/Falah.Domain) | Pure business rules: `Money`, `BusinessClock`, authorization and approvals, PIN hashing, checkout, refund, delivery, payroll, inventory and reporting rules |
| [src/Falah.Application](src/Falah.Application) | Request and DTO records shared by the server and the clients (the wire contract) |
| [src/Falah.Infrastructure](src/Falah.Infrastructure) | EF Core `FalahDbContext` and migrations, one handler per command, query services, login, approval and PIN guards, backup |
| [src/Falah.Server](src/Falah.Server) | Host: every endpoint in `Program.cs`, `AuthenticationMiddleware`, startup checks |
| [src/Falah.Client.CashierWinUI](src/Falah.Client.CashierWinUI) | Main WinUI 3 client: till, control center, owner dashboard, reports, back office |
| [src/Falah.Client.CallCenterWinUI](src/Falah.Client.CallCenterWinUI) | WinUI 3 call-center client |
| [src/Falah.Client.Shared](src/Falah.Client.Shared) | Code shared by both WinUI clients |
| [src/Falah.PrintAgent](src/Falah.PrintAgent) | Per-station print worker (console or Windows service) |
| [src/Falah.Client.Cashier](src/Falah.Client.Cashier) | Earlier WPF client, kept for reference only and not maintained |
| [tests/](tests) | `Falah.Domain.Tests` (pure, about 1 s), `Falah.Client.Tests` (view models, about 2 s), `Falah.Server.IntegrationTests` (real SQL Server database per test) |
| [tools/](tools) | Service installers, printer transport check, receipt logo and preview generators, crash-dump helpers, XAML audit |
| [docs/](docs) | Operating, deployment and reference documents (below) |
| [All Issues/](All%20Issues) | Defect registry, one file per issue, remediation state and owner questions |
| `artifacts/` | Generated output, ignored by git: release candidates, logs, screenshots |

## Requirements

- Windows 10 1904x or later, x64
- .NET SDK 8.0.4xx (runtime 8.0.30 tested)
- SQL Server 2022 Express as `.\SQLEXPRESS` (tested on 16.0.1000.6; apply the latest CU before go-live)
- For the integration tests: write access to `E:\SqlData\Tests`

## Build and test

```powershell
dotnet build Falah.sln
dotnet build src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj -p:Platform=x64
dotnet build src/Falah.Client.CallCenterWinUI/Falah.Client.CallCenterWinUI.csproj -p:Platform=x64

dotnet test tests/Falah.Domain.Tests
dotnet test tests/Falah.Client.Tests
dotnet test tests/Falah.Server.IntegrationTests --filter "FullyQualifiedName~FullTradingDayScenarioTests"
```

Each integration test creates its own `FalahTest_<guid>` database and drops it afterwards. The full integration suite takes 1.5 to 2.5 hours, so run it at milestones, never twice at once in the same tree.

## Run locally

> **Warning:** with no connection string, the server uses the restaurant's real `Falah` database. Always pass your own.

```powershell
# Server on a throwaway database, with demo users (cashier PIN 1234, manager PIN 4321)
dotnet run --project src/Falah.Server -- `
  "--ConnectionStrings:Falah=Server=.\SQLEXPRESS;Database=FalahDev;Trusted_Connection=True;TrustServerCertificate=True;" `
  --urls http://127.0.0.1:5199 --Seed:DemoUsers=true --Database:MigrateOnStartup=true --Backup:Automatic:Enabled=false

# Clients (build them first with the x64 commands above)
$env:FALAH_SERVER_URL = 'http://127.0.0.1:5199'
& src/Falah.Client.CashierWinUI/bin/x64/Debug/net8.0-windows10.0.19041.0/Falah.Client.CashierWinUI.exe
& src/Falah.Client.CallCenterWinUI/bin/x64/Debug/net8.0-windows10.0.19041.0/Falah.Client.CallCenterWinUI.exe
```

Demo users are never created in Production, and the production server refuses to start while a migration is unapplied.

## Installation package

```powershell
.\tools\build-install-package.ps1     # -> artifacts\install\Falah-Install-<id>\ and .zip
```

Builds every program self-contained for win-x64, so the restaurant PCs need no .NET installed. The package also contains the offline migration script, the installers from [deploy/](deploy) and an Arabic guide (`اقرأني أولاً.txt`). On site, double-click `1-Install-Server.cmd` on the server PC, then `2-Install-Cashier.cmd`, `3-Install-CallCenter.cmd` or `4-Install-PrintAgent.cmd` on each station. The installers:

- create the server's HTTPS certificate and trust it on each station;
- back up and verify an existing database before updating it;
- register the server and the print agents as Windows services;
- limit the firewall to the local private network;
- keep the previous version of each program for rollback.

## Database changes

Migrations live in `src/Falah.Infrastructure/Persistence/Migrations` and are generated with the pinned `dotnet ef` 8.0.11 (`.config/dotnet-tools.json`) against a database name that does not exist. They are additive and reviewed. In production they are applied from `migrations-idempotent.sql` after a verified backup and a rehearsal on a restored copy ([DEPLOYMENT](docs/DEPLOYMENT.md), [migration review](docs/DATABASE_MIGRATION_REVIEW_20261107.md)).

## Documentation

| Topic | Document |
|---|---|
| Go-live status, on-site acceptance tests, decisions | [GO_LIVE_ACCEPTANCE_20261114](docs/GO_LIVE_ACCEPTANCE_20261114.md) |
| Installing, updating and rolling back | [DEPLOYMENT](docs/DEPLOYMENT.md) |
| Multi-PC network and HTTPS | [LAN_DEPLOYMENT](docs/LAN_DEPLOYMENT.md) |
| Print agent, routes, printers, paper design | [PRINT_AGENT](docs/PRINT_AGENT.md), [PAPER_DESIGN](docs/PAPER_DESIGN.md) |
| The trading day step by step and how each step is proven | [DAILY_OPERATIONS_VERIFICATION](docs/DAILY_OPERATIONS_VERIFICATION.md) |
| Before the first real day | [GO_LIVE_CLEANUP_CHECKLIST](docs/GO_LIVE_CLEANUP_CHECKLIST.md), [OPENING_STOCK_CHECKLIST](docs/OPENING_STOCK_CHECKLIST.md) |
| What every report figure means | [METRIC_DEFINITIONS](docs/METRIC_DEFINITIONS.md) |
| Open business-policy questions | [ASSESSMENT](docs/ASSESSMENT.md) |
| Owner requests and what was done | [UI_OWNER_REQUESTS](docs/UI_OWNER_REQUESTS.md) |
| Defects and their status | [Issue registry](All%20Issues/01_MASTER_ISSUE_REGISTRY.md), [latest review](All%20Issues/REVIEW_20261117_WHOLE_PROJECT.md) |
| Working rules for contributors and agents | [CLAUDE.md](CLAUDE.md) |
| Documentation from before 2026-11-08 | [archive](docs/archive/README.md) |

## Rules that are not optional

- Never connect a test, a migration or a development server to the real `Falah` database.
- Money is integer piastres everywhere, with no floating point.
- VAT, payroll, refund policy, permissions and hardware behaviour are the owner's or the accountant's decisions. Record the question, don't change the behaviour.
- A defect is fixed by a failing test first. A screen is done when it has been run, and hardware behaviour only after an on-site test.

The full set is in [CLAUDE.md](CLAUDE.md).
