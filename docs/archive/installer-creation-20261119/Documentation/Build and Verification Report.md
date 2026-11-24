# Build and verification report

Build date: 2026-11-19 (Africa/Cairo)

## Result

- Final distributable: `Output/Falah-Restaurant-Setup.exe` (installer version 1.0.6; supersedes 1.0.5, 1.0.4, 1.0.3, 1.0.2, 1.0.1, and 1.0.0)
- Size: 429,418,177 bytes (about 410 MiB)
- SHA-256: `8033565ACF3935FA613F6C05B022F00179EA76A37BD148625A9B71411F039A3F`
- Inno Setup: 6.7.4, compiler exit code 0
- Final installer is not Authenticode-signed; no publisher signing certificate was available. The two bundled Microsoft prerequisites are signed and are checked by the build script before packaging.

- Version 1.0.6 retains the SQL Server `/LOG` correction and safe uninstall behavior. Its wizard validates required fields before installation starts, normalizes the `InitialCatalog` token, records the failing deployment phase and script location without logging the SQL connection string, and uses the accepted `Initial Catalog` / `Connect Timeout` indexer keys when changing the SQL connection builder. SQL Server, databases, TLS material, logs, and application data remain preserved.

## Verified here

- Four x64 self-contained Release publish outputs were produced: Cashier WinUI, Call Center WinUI, Server, and Print Agent. The older WPF cashier was excluded as unmaintained legacy code.
- `Falah-Restaurant.iss` compiled successfully into the final EXE.
- Target-machine evidence: installer 1.0.0 failed SQL Express setup with exit code `-2068578301` because `/ERRORREPORTING=0` was unsupported and `/SUPPRESSPRIVACYSTATEMENTNOTICE` was missing. Version 1.0.3 corrected those flags. The 1.0.5 target log at 20:39 records `Unspaced InitialCatalog key was present: False`, then fails during `Validate SQL Server connection` at line 122 with `Keyword not supported: 'InitialCatalog'`. Line 122 assigned `$masterBuilder.InitialCatalog` in PowerShell 5.1. That exact assignment reproduced the exception locally; setting `$masterBuilder['Initial Catalog']` and `$masterBuilder['Connect Timeout']` succeeded. Version 1.0.6 contains this correction and has not yet been tested on the target.
- All PowerShell deployment/build/uninstall scripts passed Windows PowerShell syntax parsing. The `InitialCatalog` normalization and SQL builder indexer assignments passed local checks, and the 1.0.6 installer compiled successfully. Uninstall behavior itself was not executed on the development machine. The ACL constructor used for the protected backup directory also passed a local construction check.
- SQL Server Express 2022 Core offline media: 279,293,816 bytes, Microsoft Authenticode signature valid, SHA-256 `2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05`.
- Microsoft Visual C++ x64 redistributable was reused from the local Visual Studio offline package cache (version 14.51.36247.0), Microsoft Authenticode signature valid, SHA-256 `843068991DAAA1F73AD9F6239BCE4D0F6A07A51F18C37EA2A867E9BECA71295C`.
- The build script verified those hashes/signatures and all four published application executables before compiling the installer.
- `7-Zip 21.04` could not inspect the generated Inno Setup archive format. Inno compilation itself succeeded; no installation was run on the development PC.
- Publish output displayed NU1900 warnings because the NuGet vulnerability feed at `api.nuget.org` was unreachable. Cached packages were sufficient for all four publish operations.

## Not verified

Scenarios A–F from the deployment request have **not** been run end-to-end. The 1.0.6 installer is waiting for target retest. Preexisting SQL, partial prerequisites, developer machine, upgrade, and bad/unavailable SQL scenarios remain unverified. Database creation/migration, SQL service authentication, HTTPS certificate/firewall setup, WinUI launch on a clean computer, printer hardware, and uninstall/upgrade behavior also remain unverified. Do not treat these cases as passed until the 1.0.6 target test completes.

Build warnings and limitations are documented in `Dependency Inventory.md` and `Deployment Guide.md`. No tracked application source files were modified for this deployment workspace.





