<#
.SYNOPSIS
Registers an already-published print agent as an automatic Windows service. Run on the station as administrator.
.NOTES
Preserves existing configuration, secrets and journals. Never publishes, prints a ticket or modifies the server database.
#>
[CmdletBinding()]
param(
    [ValidateSet('Receipt','CallCenter','Charcoal','Grill','DeliveryOrder','DriverDispatch')]
    [string] $Route = 'Receipt',
    [string] $PublishPath,
    [string] $ServiceName,
    [string] $ConfigPath,
    [switch] $Uninstall
)
$ErrorActionPreference = 'Stop'
$Route = @('Receipt','CallCenter','Charcoal','Grill','DeliveryOrder','DriverDispatch') | Where-Object { $_ -ieq $Route } | Select-Object -First 1
if (-not $ServiceName) { $ServiceName = 'FalahPrintAgent-' + $Route }
if ($ServiceName -notmatch '^[A-Za-z0-9_.-]+$') { throw 'ServiceName must contain letters, numbers, dots, underscores or hyphens.' }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run PowerShell as administrator.' }
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($Uninstall) {
    if ($existing) {
        if ($existing.Status -ne 'Stopped') { Stop-Service -Name $ServiceName; $existing.WaitForStatus('Stopped', '00:00:30') }
        & sc.exe delete $ServiceName | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Service removal failed.' }
    }
    Write-Host 'Service removed; configuration and pending print journals were preserved.'
    return
}
if ($existing) { throw 'Service already exists. Stop and uninstall it explicitly before changing its registration; keep the configuration directory.' }
if (-not $PublishPath) { throw 'Specify -PublishPath containing the published Falah.PrintAgent.exe.' }
$PublishPath = [IO.Path]::GetFullPath($PublishPath)
$exePath = Join-Path $PublishPath 'Falah.PrintAgent.exe'
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) { throw 'Published Falah.PrintAgent.exe was not found.' }

$mutex = New-Object Threading.Mutex($false, ('Global\FalahPrintAgent-' + $Route))
$owns = $false
try {
    try { $owns = $mutex.WaitOne([TimeSpan]::Zero) } catch [Threading.AbandonedMutexException] { $owns = $true }
    if (-not $owns) { throw 'Another agent is running for this route. Stop it before installing the service.' }
} finally { if ($owns) { $mutex.ReleaseMutex() }; $mutex.Dispose() }

if (-not $ConfigPath) { $ConfigPath = Join-Path $env:ProgramData ('Falah\PrintAgent\' + $Route) }
$ConfigPath = [IO.Path]::GetFullPath($ConfigPath)
New-Item -ItemType Directory -Path $ConfigPath -Force | Out-Null
$configurationFile = Join-Path $ConfigPath 'appsettings.json'
if (-not (Test-Path -LiteralPath $configurationFile)) {
    $template = Join-Path $PublishPath 'appsettings.json'
    if (-not (Test-Path -LiteralPath $template)) { throw 'A base appsettings.json is required.' }
    $configuration = Get-Content -LiteralPath $template -Raw | ConvertFrom-Json
    if ($configuration.Pin) { throw 'Remove the PIN from the base configuration and use appsettings.local.json instead.' }
    $configuration.Route = $Route
    $configuration | Add-Member -NotePropertyName DataDirectory -NotePropertyValue $ConfigPath -Force
    $configuration | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configurationFile -Encoding UTF8
}
$configuration = Get-Content -LiteralPath $configurationFile -Raw | ConvertFrom-Json
if ($configuration.Route -ine $Route) { throw 'Existing configuration has a different route; it was not overwritten.' }
if ($configuration.Pin) { throw 'Move the PIN from appsettings.json to appsettings.local.json before installing.' }
$secrets = Join-Path $ConfigPath 'appsettings.local.json'
$machineEnvironment = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey('SYSTEM\CurrentControlSet\Control\Session Manager\Environment')
try { $hasMachinePin = $machineEnvironment.GetValueNames() -contains 'FALAH_PRINT_AGENT_PIN' }
finally { $machineEnvironment.Dispose() }
if (-not (Test-Path -LiteralPath $secrets -PathType Leaf) -and -not $hasMachinePin) {
    throw 'Provide appsettings.local.json in the config directory or the machine-level FALAH_PRINT_AGENT_PIN. Its value was not read.'
}
if (Test-Path -LiteralPath $secrets -PathType Leaf) {
    & icacls.exe $secrets /inheritance:r /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the secrets file to SYSTEM and Administrators.' }
}
$binaryPath = '"{0}" "{1}" --service "--service-name={2}"' -f $exePath, $configurationFile, $ServiceName
$parameters = @{ Name = $ServiceName; BinaryPathName = $binaryPath; DisplayName = ('Falah Print Agent - ' + $Route); StartupType = 'Automatic'; Description = 'Falah print worker; one route, durable attempts, automatic recovery.' }
if ($configuration.Transport -ieq 'windows') { $parameters.DependsOn = @('Spooler') }
# With no Credential, New-Service uses LocalSystem. The on-site printer queue/certificate must be visible to that account.
New-Service @parameters | Out-Null
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/60000 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not configure service recovery.' }
& sc.exe failureflag $ServiceName 1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not enable recovery for service-reported failures.' }
Start-Service -Name $ServiceName
(Get-Service -Name $ServiceName).WaitForStatus('Running', '00:00:30')
Write-Host ('Service registered. Inspect logs under the configured DataDirectory and perform the on-site paper/power-cut checks: ' + $ServiceName)
