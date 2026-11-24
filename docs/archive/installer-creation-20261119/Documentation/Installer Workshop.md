# Falah Installer Workshop

## Start the GUI

Double-click `Installer Scripts\Run-Falah-Installer-Workshop.bat`. It needs Python 3 for Windows with Tkinter/Tcl-Tk enabled. No Python packages are required. Alternatively, run `Falah-Installer-Workshop.py` with Python 3.

The cleanup tab generates a PowerShell script for review, copying, or saving. It never executes the script during generation. Run it in **64-bit Windows PowerShell opened with Run as administrator** on the computer being cleaned. This elevation requirement is checked at runtime too, because PowerShell's `#requires` directive is bypassed when code is pasted into a console. The script only targets that computer's local SQL Server instances; it does not target a remote server.

## Cleanup choices

- **Falah application:** uses the registered Falah uninstaller after verifying the install identity and files. It stops the owned Falah server service as needed. It preserves SQL Server and databases.
- **Falah ProgramData:** permanently removes `C:\ProgramData\Falah`, including installer state, logs, TLS files, and any backups kept there.
- **One named database:** removes only the exact database name entered, if found on a reachable local SQL Server instance.
- **All user databases:** removes every non-system database found on every discovered local SQL Server Engine instance. This can delete databases for unrelated applications. `master`, `model`, `msdb`, and `tempdb` are excluded.
- **SQL Server Database Engine instances:** invokes the matching Microsoft-signed SQL Setup executable to uninstall each local Engine instance. It does not remove SSMS. Database files may remain if databases were not dropped first.

Before deleting data or uninstalling SQL Engine instances, the script checks instance access and Setup media and displays the plan. It proceeds only after the exact phrase `WIPE SELECTED DATA` is typed. Keep a verified backup outside `C:\ProgramData\Falah` before choosing data-removal options. If matching Microsoft Setup media is unavailable or SQL connections cannot be checked, it stops before cleanup. Cleanup transcripts and the separate Falah uninstaller log are saved under `C:\ProgramData\Falah-Cleanup\Logs`. A nonzero Inno uninstaller code is accepted only if its log has both successful-completion markers and all application executables are confirmed absent; then only the verified Falah installation directory's residual files are removed. If a previous successful uninstall already removed its registration and uninstaller but left the known deployment-generated production config, the cleanup script can recognize and remove that narrow residual state. On a runtime failure, the script prints the failing command and line, keeps an interactive PowerShell window open for review, and does not exit the PowerShell host.

The SQL **all local user databases** option is intentionally broad. The script does not provide a remote-server wipe option. It cannot guarantee deletion of database files that SQL Server has already detached or placed outside its normal database catalog.

## Build a new installer

The build tab calls the repository's `Build-Payload.ps1`, which publishes the current Cashier, Call Center, Server, and Print Agent source projects as Release, self-contained `win-x64` outputs. It clears each module's publish output and build-output directory first, and clears that module's intermediate files too unless `-NoRestore` was explicitly requested. This prevents files left by earlier builds from being included by the installer's wildcard staging. It writes `Logs\payload-build-manifest.json` with the Git commit, working-tree changes, and SHA-256 of each output EXE. Once publishing succeeds, the GUI asks whether to continue. Choosing **Yes** calls `Build-Installer.ps1 -SkipPublish`, which verifies the bundled redistributables and Inno Setup compiler before producing:

`Output\Falah-Restaurant-Setup.exe`

The build expects the project's .NET SDK and its NuGet package cache, the staged signed dependencies, and the staged Inno Setup compiler. It does not download dependencies. Build logs are written under `Logs`.

## Scope

The GUI and its scripts do not themselves uninstall or modify the current machine's SQL Server or application. The generated cleanup script is a separate, explicit, destructive action. The build tab does run the project's publishing and installer compiler scripts and writes outputs inside `Installer Creation`.
