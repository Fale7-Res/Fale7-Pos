<#
Runs only repository tests. SQL resources are created/dropped by FalahApiFactory as
FalahTest_<guid>; this script never launches the normal server or targets production.
Run in the owner's ordinary interactive Windows PowerShell, without elevation.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath (Join-Path $taskRoot 'Falah.sln'))) {
    throw 'Run this script from its original tools directory inside Falah-Restaurant-CSharp.'
}
$taskIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
if ($taskIdentity -match 'codexsandbox') {
    throw 'This verification needs the ordinary Windows account with its loaded user profile, not the Codex sandbox account.'
}
if (Get-Process -Name testhost -ErrorAction SilentlyContinue) {
    throw 'Another test host is running. Collect its result before starting this verification.'
}
# Windows PowerShell 5.1 does not load System.Security on its own; without this the probe below
# reports "DPAPI unavailable" on a machine where DPAPI works (seen 2026-11-08, owner account).
Add-Type -AssemblyName System.Security
try {
    $taskProtectedProbe = [System.Security.Cryptography.ProtectedData]::Protect(
        [byte[]](1, 2, 3), $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    if ($taskProtectedProbe.Length -eq 0) { throw 'Empty DPAPI probe result.' }
} catch {
    throw "CurrentUser data protection is unavailable in this Windows context. No tests were started. $($_.Exception.Message)"
}

$taskRunName = 'codex-owner-shift-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$taskResults = Join-Path $taskRoot ('artifacts\' + $taskRunName)
New-Item -ItemType Directory -Path $taskResults -Force | Out-Null
$taskIdentity | Set-Content -LiteralPath (Join-Path $taskResults 'execution-identity.txt') -Encoding UTF8
$env:NUGET_PACKAGES = 'E:\DevCaches\NuGet\packages'
$env:TEMP = 'E:\DevCaches\Temp'
$env:TMP = 'E:\DevCaches\Temp'

function Invoke-TaskTests {
    param([string]$Name, [string]$Project, [string]$Filter)
    $taskArgs = @('test', $Project, '--no-restore', '--nologo', '-v:minimal',
        '--results-directory', $taskResults, '--logger', "trx;LogFileName=$Name.trx")
    if ($Filter) { $taskArgs += @('--filter', $Filter) }
    Write-Host "Running $Name as $taskIdentity"
    & dotnet @taskArgs 2>&1 | Tee-Object -FilePath (Join-Path $taskResults ($Name + '.log'))
    $taskExit = $LASTEXITCODE
    if ($taskExit -ne 0) { throw "$Name failed (exit $taskExit). Preserve and review the evidence in $taskResults." }
}

Push-Location -LiteralPath $taskRoot
try {
    # One small SQL test first: fail promptly if SQL permissions/encryption are still unavailable.
    Invoke-TaskTests 'sql-preflight' 'tests/Falah.Server.IntegrationTests' `
        'FullyQualifiedName~Closing_with_no_orders_and_matching_cash_needs_no_approval_and_posts_only_a_handover'
    Invoke-TaskTests 'domain' 'tests/Falah.Domain.Tests' ''
    Invoke-TaskTests 'client' 'tests/Falah.Client.Tests' ''
    Invoke-TaskTests 'shift-printing-sql' 'tests/Falah.Server.IntegrationTests' `
        'FullyQualifiedName~ShiftCloseVerticalSliceTests|FullyQualifiedName~SlipPrintingAndReprintTests'
    Write-Host "Verification completed. Results: $taskResults"
    Write-Host 'This proves the selected software tests only, not physical printing or complete project readiness.'
} finally {
    Pop-Location
}
