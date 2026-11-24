# Falah deployment dependency inventory

Audit date: 2026-11-19. Source: repository project files, deployment documentation, published outputs, server startup code and configuration. No source files were changed for this inventory.

## Application modules

| Module | Project | Type / target | Runtime strategy | Role |
|---|---|---|---|---|
| Cashier | `src/Falah.Client.CashierWinUI` | Unpackaged WinUI 3, `net8.0-windows10.0.19041.0`, x64 | Self-contained .NET and Windows App SDK | Till, control center, owner reports and back office |
| Call Center | `src/Falah.Client.CallCenterWinUI` | Unpackaged WinUI 3, `net8.0-windows10.0.19041.0`, x64 | Self-contained .NET and Windows App SDK | Call-center ordering client |
| Server | `src/Falah.Server` | ASP.NET Core Web app, `net8.0`, win-x64 | Self-contained .NET | HTTP(S) API, business services and SQL data access; Windows Service capable |
| Print Agent | `src/Falah.PrintAgent` | Console / Windows Service worker, `net8.0`, win-x64 | Self-contained .NET | Authenticates to Server and dispatches route-specific jobs to Windows spooler/network/serial printer transports |

The solution also contains `src/Falah.Client.Cashier`, a WPF client. `README.md` calls it an earlier client kept for reference and not maintained; it is excluded from the production installer. Domain, Application and Infrastructure are libraries, not independently launched modules.

## Runtime and native dependencies

- All four production modules are published `win-x64` and self-contained. The target does not need a separately installed .NET 8 Runtime or Desktop Runtime.
- The two WinUI projects set `WindowsPackageType=None` and `WindowsAppSDKSelfContained=true`; publish output contains the app-local Windows App SDK runtime and XAML resources. The installer refuses Windows builds below 17763 (Windows 10 1809).
- The clients require Windows UI/XAML platform components and a Windows x64 process. They are not portable to Linux, macOS, or ARM Windows by these builds.
- Print Agent uses `System.Drawing.Common`, `System.IO.Ports`, code pages, QRCoder and the Windows spooler; package/runtime code is app-local. A physical printer queue/driver, network reachability, printer port and drawer hardware are site dependencies and cannot be inferred or safely installed by a generic installer.
- Server uses Microsoft.EntityFrameworkCore.SqlServer 8.0.11 / Microsoft.Data.SqlClient. Published Server includes its managed and SNI dependencies. Database engine is not bundled with the app itself.
- Current publish folders do not contain `vcruntime140*.dll` or `msvcp140*.dll`; therefore the installer checks and provisions the Microsoft Visual C++ 2015-2022 x64 runtime, version 14.40.33810 or newer. Search of Visual Studio's package cache found the signed offline installer at version 14.51.36247.0; it was copied into the deployment workspace without downloading another copy. SHA-256: `843068991DAAA1F73AD9F6239BCE4D0F6A07A51F18C37EA2A867E9BECA71295C`.
- No SQL Server engine service is present on the build machine. `sqlcmd.exe` is a client utility only. A full SQL Server 2022 Express Core x64 offline installer was not found in local downloads/package caches, so the Microsoft-hosted offline media was staged and verified (Authenticode: Microsoft Corporation; SHA-256: `2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05`). The much smaller web bootstrapper is also staged but is not used for offline installation. The signed Inno Setup 6.7.4 compiler was staged in `Tools`.

## Database and startup

- Provider: SQL Server via `UseSqlServer`; default connection in `Program.cs` is `Server=.\\SQLEXPRESS;Database=Falah;Trusted_Connection=True;TrustServerCertificate=True;`.
- LocalDB is insufficient for the intended central API used by multiple client stations. A full SQL Server Database Engine is required on the server host; SQL Server Express is the smallest bundled local option. Existing SQL Server instances must be reused after connectivity/version validation.
- Local clients communicate with Server over HTTP loopback by default (`localhost:5151`). For remote stations, the clients require `FALAH_SERVER_URL`; code rejects non-loopback HTTP. Server remote bindings also require HTTPS and a valid certificate. Configure a machine-trusted certificate and HTTPS name for LAN deployment; do not install an insecure HTTP LAN binding.
- The tracked server `appsettings.json` uses a development-specific `E:/falah-restaurant/Backups` default. Deployment overrides it with the protected `%ProgramData%\Falah\Backups` directory so installed backups do not depend on a developer drive letter.
- Print Agent polls Server and requires a dedicated least-privilege `print_agent` account/PIN and per-route printer settings. The committed `appsettings.json` intentionally contains no PIN. Do not auto-create a demo account or enable demo PIN seeding in production.
- Production has `Database:MigrateOnStartup=false`. Existing databases with pending migrations are deliberately refused by Server. Never upgrade an existing restaurant database automatically from setup. For a new, empty application database, initialization may be offered only as an explicit user action; for existing databases require a verified backup and the reviewed idempotent migration script.
- Server creates an initial owner only when `Bootstrap:OwnerPin` is provided. The wizard collects a 4–6 digit non-repeating owner PIN, and the configuration helper removes it from the protected production config after the initial successful health check.
- The server is Windows-Service capable (`FalahServer`). Existing service scripts stop/delete an existing service, so they must not be reused for safe in-place upgrades. Installer service management must check installation identity and preserve existing config/data.

## Build packages and app-local NuGet dependencies

| Project | Direct packages |
|---|---|
| Cashier WinUI / Call Center WinUI | Microsoft.WindowsAppSDK 1.7.260224002; Microsoft.Windows.SDK.BuildTools 10.0.22621.3233; CommunityToolkit.Mvvm 8.3.2 |
| Print Agent | QRCoder 1.8.0; System.IO.Ports 8.0.0; System.ServiceProcess.ServiceController 8.0.1; System.Text.Encoding.CodePages 8.0.0; System.Drawing.Common 8.0.0 |
| Server | Microsoft.AspNetCore.OpenApi 8.0.30; EF Core Design 8.0.11; WindowsServices 8.0.1; Swashbuckle 6.6.2 |
| Infrastructure | EF Core SQL Server 8.0.11; EF Core Design 8.0.11; Hosting Abstractions 8.0.1; System.Drawing.Common 8.0.0 |

NuGet packages are build inputs. Publish output is the runtime deliverable; SDKs, test frameworks, analyzers, Visual Studio and SQL management tools are not target prerequisites.

## SQL prerequisite package

- Planned: Microsoft SQL Server 2022 Express x64, Database Engine only (`SQLEXPR_x64_ENU.exe`), official Microsoft-hosted and Authenticode signed.
- Online bootstrapper is not suitable as the offline payload. Complete Core media is 279,293,816 bytes based on Microsoft CDN response. The signed download is being staged under `Dependencies/Offline Installers` and must be signature-checked before use.
- SQL Server Express has edition limits and is not a substitute for a licensed Standard/Enterprise server where workload or capacity requires one. Never upgrade or replace an existing engine.
- A SQL Engine installer is not a SQL management client. SSMS/sqlcmd are not required for ordinary application runtime.

## Installation states and safety model

Application installation state is recorded only after server health verification under `%ProgramData%\Falah\install-state.json` and the installer-owned `HKLM\Software\Falah\Restaurant\Installer` key. Dependency discovery is separate and based on SQL Engine registry/services/version, OS build, the VC runtime registry and a live SQL connection; repository files, SDKs, NuGet cache, and publish folders never count as an installed application.

Mode A reuses SQL Server 2016+ if present and can install SQL Express Core only when a local engine is absent and the selected connection is local. It never upgrades or replaces an engine. An old engine is left untouched and reported for side-by-side remediation. Mode B never invokes SQL setup. Both modes validate SQL connectivity and check the target database. New database creation/migrations require the explicit new-database option and a database-absent check; existing database use requires a separate confirmation and migrations remain disabled. The application's own startup can change the RCSI database option on an explicitly authorized existing database; the installer displays that behavior before authorization. Uninstall preserves SQL and all databases.

## Verification status

The four app-local publish folders from the preceding task were built on this developer Windows x64 machine. No clean Windows VM is available in this task. SQL setup, runtime detection, printer hardware, HTTPS certificate provisioning, existing database upgrades and all clean-machine scenarios remain unverified until tested on isolated VMs/hardware.

Build outputs are staged under `Application Builds`. The application payload includes all four modules. The package's SQL and VC++ installers were searched for in local Downloads, Visual Studio/Windows installer caches, Program Files and relevant package directories before using any downloaded package. VC++ was reused from the local Visual Studio redistributable cache. SQL Server 2022 Express Core offline media was not present locally and the Microsoft-signed media was previously staged. No .NET runtime installer is required because each app is self-contained. Windows App SDK runtime files are app-local in each WinUI publish folder; no additional Windows App SDK installer is required. Printer drivers, print routing credentials, SQL TLS configuration, and HTTPS PKI are environment-specific and are not bundled.

The source repository itself has no installer-specific source edits; all installer code, config, build output, tools and package artifacts reside in this `Installer Creation` workspace. The prior WPF cashier remains excluded as unmaintained legacy code.
