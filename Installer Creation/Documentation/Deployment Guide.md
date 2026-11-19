# Falah Restaurant Deployment

## Package contents

The offline setup package contains four self-contained x64 applications: Cashier (WinUI 3), Call Center (WinUI 3), the ASP.NET Core API/Windows service, and Print Agent. The older WPF cashier project is legacy and is not part of the maintained deployment. The package also contains the SQL Server 2022 Express Core offline installer and Microsoft Visual C++ x64 redistributable.

The applications are published self-contained, so the target does not need a separately installed .NET runtime or SDK. The WinUI clients need Windows 10 build 17763 or newer and the Visual C++ 2015–2022 x64 runtime. The deployment helper checks the OS and the VC runtime registry, then installs the bundled VC package only when it is missing or below the selected minimum.

## Installation modes

**Full Installation** on an Application server reuses a compatible local SQL Engine if one is detected. If there is no SQL Engine, it installs the bundled SQL Server Express 2022 Database Engine as the `FALAHRESTAURANT` named instance. It never upgrades or replaces an existing SQL instance. An older engine is left untouched and produces a diagnostic for side-by-side remediation. A reachable remote SQL server can also be selected by entering its connection string.

**Install Application (SQL Server Already Available)** never launches a SQL Server installer and never changes SQL Server services. It validates the supplied SQL connection and database before configuring the application. A remote SQL server is supported; use credentials and network policy appropriate for the API service identity.

The separate computer-role page distinguishes **Application server** from **Workstation**. A workstation installs all four module files and the VC++ runtime if needed, then verifies the central API `/health` endpoint and sets `FALAH_SERVER_URL`. It does not install SQL Server, configure a local database, or create/start a local API service, regardless of the SQL mode selection. This supports installing the single packaged EXE on client PCs without spawning duplicate API services.

Server-role installations require an initial owner PIN and explicit database authorization. Select **Initialize only if this database does not already exist** only for a new database that the operator has authorized. The helper checks the name in `master`, refuses initialization if the database exists, creates an empty database, starts the API with migrations enabled for that first start, creates the initial owner from the PIN, waits for health, removes the PIN from configuration, then disables startup migrations. For an existing database, select **Authorize use of an existing database**. Startup migrations remain disabled. The application itself can set `READ_COMMITTED_SNAPSHOT` when it starts; this behavior is in the application code and must be accepted by the database owner.

Neither mode drops databases, seeds demo PINs, or removes SQL Server during uninstall. Uninstall removes only the `FalahServer` service if its executable path matches the verified Falah installation. SQL databases, SQL engine, TLS files, and installer logs are retained.

## Network and TLS

For a single computer, enter a loopback URL such as `http://localhost:5151`. For network clients, enter an HTTPS URL whose hostname appears in a trusted PFX certificate; select that PFX and its password. The installer binds the API to all interfaces on the selected port and creates a Private/Domain Windows Firewall rule. Ensure client machines trust the certificate issuing authority and that DNS resolves the API hostname. Remote cleartext HTTP is rejected by the server transport guard.

## Print Agent

Print Agent is included and deployed but is not started as a service automatically because it needs site-specific route, printer, and credentials. Configure an independent service identity and one route per instance using the template in `Configuration/print-agent.template.json`. Do not put a production PIN in a broadly readable config file.

## Logs and installed state

- Setup log: `{tmp}\Setup Log ...` while running; SQL setup log and helper transcript are stored under `%ProgramData%\Falah\Logs\Installer`.
- Application installed state: `%ProgramData%\Falah\install-state.json` and `HKLM\Software\Falah\Restaurant\Installer` (written after health verification).
- Dependency state is checked independently using Windows OS build, Visual C++ runtime registry, SQL instance registry/services, and an actual SQL connection attempt.

## Rebuild

Run PowerShell as Administrator from this directory:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Installer Scripts\Build-Installer.ps1
```

The script republishes the four modules into `Application Builds`, verifies dependency signatures and pinned SHA-256 hashes, compiles `Installer Scripts/Falah-Restaurant.iss`, and writes `Output/Falah-Restaurant-Setup.exe` with a hash sidecar. No SDK, source file, or publish output outside this workspace is used as evidence that the product is installed.

## Important limits

The application service and client apps are installed together. For a multi-computer restaurant, install/configure the API service and database on the designated server, then configure each workstation's `FALAH_SERVER_URL` to that server's HTTPS URL. The installer has not been exercised on a clean VM; SQL Express setup, Windows firewall behavior, SQL authentication under the service identity, certificate trust on a second PC, printer drivers, upgrades, and uninstall remain unverified until tested on isolated machines. Back up production data before any planned application migration.
