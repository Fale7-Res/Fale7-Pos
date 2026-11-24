param([Parameter(Mandatory)][string]$InstallRoot)
$ErrorActionPreference = 'Stop'

function Get-NormalizedRoot([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
}

function Test-PathUnderInstallRoot([string]$Path, [string]$Root) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    try { $fullPath = [IO.Path]::GetFullPath($Path) } catch { return $false }
    return $fullPath.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)
}

$root = Get-NormalizedRoot $InstallRoot

# Stop every Windows service whose executable is inside this exact installation.
# This includes the API and any future app-owned service without touching SQL Server.
$services = Get-CimInstance Win32_Service -ErrorAction Stop
foreach ($service in $services) {
    $binaryPath = $null
    if ($service.PathName -match '^\s*"([^"]+\.exe)"' -or $service.PathName -match '^\s*(.+?\.exe)(?:\s|$)') {
        $binaryPath = $Matches[1]
    }
    if (-not (Test-PathUnderInstallRoot $binaryPath $root)) { continue }

    if ($service.State -eq 'Running') {
        Stop-Service -Name $service.Name -Force -ErrorAction Stop
        (Get-Service -Name $service.Name).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    }
    & sc.exe delete $service.Name | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not remove Falah service '$($service.Name)' (sc.exe exit $LASTEXITCODE)." }
}

# Close app windows first, then force-stop only processes launched from this install.
$processes = Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
    Test-PathUnderInstallRoot $_.ExecutablePath $root
}
foreach ($process in $processes) {
    try {
        $proc = Get-Process -Id $process.ProcessId -ErrorAction Stop
        if ($proc.MainWindowHandle -ne 0) {
            [void]$proc.CloseMainWindow()
            if (-not $proc.WaitForExit(5000)) { Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop }
        } else {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        }
    } catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
        # Process exited during shutdown; continue uninstalling.
    } catch {
        if (Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue) { throw }
    }
}

# Remove machine-wide deployment settings only when the independent install marker
# confirms that this exact directory owns them. Never touch unrelated installs.
$statePath = Join-Path $env:ProgramData 'Falah\install-state.json'
if (-not (Test-Path -LiteralPath $statePath)) { exit 0 }
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ((Get-NormalizedRoot $state.InstallRoot) -ne $root) { exit 0 }

$currentUrl = [Environment]::GetEnvironmentVariable('FALAH_SERVER_URL', 'Machine')
if ($currentUrl -eq $state.ServerUrl) {
    [Environment]::SetEnvironmentVariable('FALAH_SERVER_URL', $null, 'Machine')
}
Get-NetFirewallRule -Group 'Falah Restaurant Deployment' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Remove-Item 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue

# SQL Server, databases, TLS material, installation logs and application data are preserved.
