param([Parameter(Mandatory)][string]$InstallRoot,[string]$Role='Server')
$ErrorActionPreference = 'Stop'
$statePath = Join-Path $env:ProgramData 'Falah\install-state.json'
if (-not (Test-Path $statePath)) { exit 0 }
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ([IO.Path]::GetFullPath($state.InstallRoot).TrimEnd('\') -ne [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')) { exit 0 }
if ($Role -eq 'Workstation' -and $state.Role -eq 'Server') { throw 'This path currently hosts the Falah API service. Install as Application server or uninstall it before changing this machine to workstation role.' }
if ($Role -eq 'Workstation') { exit 0 }
$svc = Get-CimInstance Win32_Service -Filter "Name='FalahServer'" -ErrorAction SilentlyContinue
if (-not $svc) { exit 0 }
$expected = [IO.Path]::GetFullPath((Join-Path $InstallRoot 'Server\Falah.Server.exe'))
if ($svc.PathName -notlike "*$expected*") { throw 'FalahServer service points outside the verified Falah installation; it was not stopped.' }
if ($svc.State -eq 'Running') { Stop-Service -Name FalahServer -Force; (Get-Service FalahServer).WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30)) }
