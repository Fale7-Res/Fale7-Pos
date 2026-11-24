<#
.SYNOPSIS
    Runs one installation step for Setup.exe and reports the outcome in a result file that Setup
    shows in its own window. The Windows 10 console cannot display Arabic, so Setup runs this hidden
    and asks every question (owner PIN, printers, print PINs) in its wizard instead.

.DESCRIPTION
    Writes a full log to C:\Falah\Logs and, with -ResultFile, a UTF-8 file whose first line is OK or
    FAILED and whose second line is the message. PINs arrive in environment variables set by Setup
    (FALAH_SETUP_OWNER_PIN, FALAH_SETUP_PRINT_PIN), never on the command line, and are never written
    to the log. When a server certificate file is given it is kept in C:\Falah\falah-server.cer, where
    later runs find it.
#>
param(
    [ValidateSet('Server', 'Cashier', 'CallCenter', 'PrintAgent')]
    [string] $Step,
    [string] $ServerUrl = '',
    [string] $CertificatePath = '',
    [string] $BackupDir = '',
    [string] $Route = '',
    [string] $PrinterName = '',
    [string] $ResultFile = '',
    [string] $LogDir = 'C:\Falah\Logs',
    [switch] $Yes
)
$ErrorActionPreference = 'Stop'
$package = $PSScriptRoot
$succeeded = $false
$message = ''
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$log = Join-Path $LogDir ("setup-{0}-{1}{2}.log" -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $Step, $(if ($Route) { "-$Route" } else { '' }))
Start-Transcript -LiteralPath $log | Out-Null
if ($ResultFile) { $env:FALAH_SETUP_RUN = '1' }   # the steps must never wait for keyboard input in this mode
try {
    . (Join-Path $package 'Common.ps1')
    if (-not $Step) { throw 'الخطوة مش محددة.' }

    if ($CertificatePath) {
        if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "ملف شهادة السيرفر مش موجود: $CertificatePath" }
        $kept = Join-Path $FalahRoot 'falah-server.cer'
        New-Item -ItemType Directory -Force -Path $FalahRoot | Out-Null
        if ((Resolve-Path -LiteralPath $CertificatePath).Path -ne $kept) { Copy-Item -LiteralPath $CertificatePath $kept -Force }
        $CertificatePath = $kept
    }
    if (-not $ServerUrl -and $Step -eq 'PrintAgent') {
        $ServerUrl = [Environment]::GetEnvironmentVariable('FALAH_SERVER_URL', 'Machine')
    }

    $station = @{}
    if ($ServerUrl) { $station.ServerUrl = $ServerUrl }
    if ($CertificatePath) { $station.CertificatePath = $CertificatePath }
    switch ($Step) {
        'Server' {
            $server = @{ Yes = $Yes }
            if ($BackupDir) { $server.BackupDir = $BackupDir }
            & (Join-Path $package 'Install-Server.ps1') @server
        }
        'Cashier'    { & (Join-Path $package 'Install-Station.ps1') -Role Cashier @station }
        'CallCenter' { & (Join-Path $package 'Install-Station.ps1') -Role CallCenter @station }
        'PrintAgent' {
            if ($Route) { $station.Route = $Route }
            if ($PrinterName) { $station.PrinterName = $PrinterName }
            & (Join-Path $package 'Install-PrintAgent.ps1') @station
        }
    }
    $succeeded = $true
}
catch {
    $message = $_.Exception.Message
    if ($message -match 'NonInteractive') {
        $message = 'خطوة من خطوات التثبيت طلبت إدخال من الكيبورد بدل ما تاخده من Setup. ابعت ملف السجل للدعم.'
    }
    Write-Host "FAILED: $($_.Exception.Message)"
}
finally {
    Stop-Transcript | Out-Null
}
if ($ResultFile) {
    $lines = @($(if ($succeeded) { 'OK' } else { 'FAILED' }), $message, $log)
    [IO.File]::WriteAllLines($ResultFile, $lines, (New-Object Text.UTF8Encoding($true)))
} else {
    if (-not $succeeded) { Write-Host "حصلت مشكلة: $message" -ForegroundColor Red }
    [void](Read-Host 'Enter')
}
if ($succeeded) { exit 0 } else { exit 1 }
