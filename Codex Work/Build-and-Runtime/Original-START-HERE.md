# Fale7C# local test environment

This folder contains generated local setup artifacts, not application source changes.
Use only instance **Fale7CSharpDev**, database **Fale7CSharp_Test**, and loopback API **http://localhost:5151**.

## Start again after initialization

Open PowerShell in the repository root. In the first terminal:

```powershell
& .\artifacts\local-runtime\Start-TestServer.ps1
```

Keep that terminal open. The script verifies the known isolated database before starting the actual backend, sets configuration only for the child process, disables automatic backup and appends server output to server.log. Starting the server applies any pending migrations, so use the agreed existing source/build only. Startup has been unusually slow on this Windows machine; do not open duplicate servers while it initializes.

Check readiness from a second terminal:

```powershell
Invoke-RestMethod http://localhost:5151/health
& .\artifacts\local-runtime\Start-TestClients.ps1
```

The client script checks the backend executable, listening PID and its SQL session on the explicitly named test database before opening **Cashier WinUI only** by default. For an optional phone-order station, use `& .\artifacts\local-runtime\Start-TestClients.ps1 -IncludeCallCenter`. It sets the API URL only in the launching process. Accounts are the existing sample cashier and manager provisioned by DemoDataSeeder; credential values are deliberately excluded from reports. The account-defining code remains in src/Falah.Infrastructure/Persistence/DemoDataSeeder.cs. Do not reuse these sample accounts for production.

For live receipt-job verification with no physical printer, open another terminal:

```powershell
& .\artifacts\local-runtime\Start-TestFilePrintAgent.ps1 -Route Receipt
```

Call Center customer-ticket jobs can use another terminal with `-Route CallCenter`. Other supported routes can use the same script with their explicit route names when needed. The launcher forces file transport, clears physical transport targets and verifies the backend SQL target first. Do not run the executable with the checked-in default printer configuration for this test.

The one-time Initialize-TestDatabase.ps1 script refuses an existing instance and must **not** be rerun for normal startup. It was prepared solely for the approved first initialization.

## Stop safely

The current initial run was launched in Codex-managed execution sessions. Ask Codex to stop this run gracefully before starting a second backend. The commands below describe later runs started in your own PowerShell terminals; the server launcher also refuses an occupied port to prevent duplicate startup.

1. Resolve any pending transaction outcome through the application, then close Cashier and Call Center using their normal window close buttons. Closing the app does not itself close a business day or shift.
2. Press Ctrl+C in each file-print-agent terminal and wait for its process to exit.
3. Press Ctrl+C in the server terminal and wait for its host to stop.
4. Optionally stop **only the dedicated test instance**, after its clients are stopped:

```powershell
SqlLocalDB.exe stop Fale7CSharpDev
```

Do not stop POS2016, MSSQLLocalDB, ProjectModels, PostgreSQL or any unrelated service. No database drop/reset/restore is part of stopping. The test files and records are kept for the next run.

## Evidence and limitations

- architecture-and-runtime.md: code-derived project map, dependency/environment details, safety plan and progress.
- projects.json: every solution project, direct package versions and references.
- restore.log and build-release-final.log: dependency restore and successful final Release build (0 warnings/errors).
- build-release.log: intermediate output-copy lock failure, resolved by sequential execution; original source unchanged.
- database-initialization.log and initialized-test-database.json: dedicated new instance/database identity.
- executables.json: x64 PE evidence.
- print-offline.log and print-output/preview-*.png: actual ESC/POS raster rendering/file transport verification; no physical print.
- server.log: actual backend console output once it begins logging.
- runtime-verification.json: created only after real database/API/financial smoke verification succeeds. Its absence means those checks have not completed successfully.
- printagent-*.log and live-print-output/: created by file-only workers.
- running-processes.json: credential-free snapshot of the active test processes, instance/database and health.

Production readiness is a separate task: real account provisioning, restaurant opening balances/menu/prices, a service-based SQL/server deployment, LAN/firewall/access configuration where needed, verified backup/restore procedures and explicitly authorized physical printer tests. This single-user LocalDB environment is for development and local testing only.
