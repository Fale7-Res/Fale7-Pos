<#
.SYNOPSIS
    يشيل نظام فالح من الجهاز ده: الخدمات والبرامج والاختصارات والإعدادات. مع -RemoveDatabase بياخد نسخة
    احتياطية أخيرة ويتأكد منها، وبعدين يمسح قاعدة البيانات ويشيل SQL Server Express.

.DESCRIPTION
    Owner request 2026-11-26. Setup's uninstaller runs this hidden (Windows 10's console shows Arabic as boxes) and
    shows the result in its own window.

    Always removed: the FalahServer and FalahPrintAgent-* services, the programs under C:\Falah (server, cashier,
    call center, print agents, their .previous copies, the installation packages except the one uninstalling,
    logs, the certificate file), %ProgramData%\Falah (print agent settings and PINs, server logs), each user's
    %LocalAppData%\Falah, the desktop shortcuts that point into C:\Falah, the firewall rule, FALAH_SERVER_URL and
    the server certificate.

    With -RemoveDatabase: a final COPY_ONLY backup, verified, in the server's backup folder; then the database is
    dropped; then SQL Server Express is removed, unless the instance holds another database (that is someone
    else's data, so it stays and the result says so). If the final backup fails nothing is dropped.

    Never removed: the backup folders (the server's and its copy) and anything else under C:\Falah that Setup did
    not put there. The log of this run is written into the backup folder.

    The path, name and instance parameters exist so the script can be exercised on scratch folders and a test
    database; Setup passes only -RemoveDatabase, -KeepDir and -ResultFile.
#>
param(
    [switch] $RemoveDatabase,
    [string] $KeepDir = '',
    [string] $ResultFile = '',
    [switch] $DryRun,
    [string] $SqlInstance = '.\SQLEXPRESS',
    [string] $Database = 'Falah',
    [string] $FalahRoot = 'C:\Falah',
    [string] $ProgramDataRoot = (Join-Path $env:ProgramData 'Falah'),
    [string[]] $DesktopDirs = @(),
    [string[]] $UserDataDirs = @(),
    [string] $ServerService = 'FalahServer',
    [string] $PrintServicePrefix = 'FalahPrintAgent-',
    [string] $FirewallRule = 'Falah Server',
    [string] $ServerUrlVariable = 'FALAH_SERVER_URL',
    [string] $CertificateSubject = 'CN=falah-server',
    [string] $SqlSetupExe = ''
)
$ErrorActionPreference = 'Stop'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $env:TEMP "falah-uninstall-$stamp.log"
$problems = New-Object System.Collections.Generic.List[string]
$succeeded = $false
$message = ''
Start-Transcript -LiteralPath $log | Out-Null

function Write-Step([string]$text) { Write-Host ''; Write-Host "==> $text" }
function Write-Ok([string]$text) { Write-Host "    $text" }
function Remove-Path([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return }
    if ($DryRun) { Write-Ok "(تجربة) هيتمسح: $path"; return }
    try { Remove-Item -LiteralPath $path -Recurse -Force; Write-Ok "اتمسح: $path" }
    catch { $problems.Add("ما اتمسحش $path ($($_.Exception.Message))"); Write-Ok "ما اتمسحش: $path - $($_.Exception.Message)" }
}
function New-SqlConnection([string]$database) {
    $c = New-Object System.Data.SqlClient.SqlConnection("Server=$SqlInstance;Database=$database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15")
    $c.Open(); return $c
}
function Invoke-Sql([string]$sql, [switch]$Scalar) {
    $c = New-SqlConnection 'master'
    try { $cmd = $c.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 0; if ($Scalar) { return $cmd.ExecuteScalar() } else { [void]$cmd.ExecuteNonQuery() } }
    finally { $c.Dispose() }
}
function Get-ServiceEnvironment([string]$name) {
    $key = "HKLM:\SYSTEM\CurrentControlSet\Services\$name"
    $values = @{}
    try { foreach ($line in @((Get-ItemProperty -LiteralPath $key -Name Environment -ErrorAction Stop).Environment)) {
        $i = $line.IndexOf('='); if ($i -gt 0) { $values[$line.Substring(0, $i)] = $line.Substring($i + 1) } } } catch { }
    return $values
}

try {
    if ($Database -notmatch '^[A-Za-z0-9_]+$') { throw 'اسم قاعدة البيانات غير صالح.' }
    if (-not $DesktopDirs) {
        $DesktopDirs = @([Environment]::GetFolderPath('CommonDesktopDirectory')) +
            @(Get-ChildItem 'C:\Users' -Directory -ErrorAction SilentlyContinue | ForEach-Object { Join-Path $_.FullName 'Desktop' })
    }
    if (-not $UserDataDirs) {
        $UserDataDirs = @(Get-ChildItem 'C:\Users' -Directory -ErrorAction SilentlyContinue | ForEach-Object { Join-Path $_.FullName 'AppData\Local\Falah' }) +
            @("$env:SystemRoot\System32\config\systemprofile\AppData\Local\Falah")
    }
    Write-Host "إزالة فالح$(if ($RemoveDatabase) { ' مع قاعدة البيانات وSQL Server' })$(if ($DryRun) { ' (تجربة - مفيش حاجة هتتمسح)' })"

    # ---- The backup folders: read from the server's own settings before the service goes. ---------------------
    $environment = Get-ServiceEnvironment $ServerService
    $backupDirs = @($environment['Backup__DefaultDirectory'], $environment['Backup__CopyToDirectory'], (Join-Path $FalahRoot 'Backups')) |
        Where-Object { $_ } | Select-Object -Unique
    $backupDir = @($backupDirs | Where-Object { Test-Path -LiteralPath $_ }) | Select-Object -First 1
    if (-not $backupDir) { $backupDir = $backupDirs[0] }
    Write-Step 'مجلدات النسخ الاحتياطية (مش هتتمسح)'
    $backupDirs | ForEach-Object { Write-Ok $_ }

    # ---- Programs that are running. ------------------------------------------------------------------------------
    Write-Step 'قفل البرامج المفتوحة'
    foreach ($name in 'Falah.Client.CashierWinUI', 'Falah.Client.CallCenterWinUI') {
        foreach ($p in @(Get-Process -Name $name -ErrorAction SilentlyContinue)) {
            if ($DryRun) { Write-Ok "(تجربة) هيتقفل: $name" } else { $p | Stop-Process -Force; Write-Ok "اتقفل: $name" }
        }
    }

    # ---- Services. ---------------------------------------------------------------------------------------------
    Write-Step 'الخدمات'
    $services = @(Get-Service -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $ServerService -or $_.Name -like "$PrintServicePrefix*" })
    if (-not $services) { Write-Ok 'مفيش خدمات فالح على الجهاز ده.' }
    foreach ($s in $services) {
        if ($DryRun) { Write-Ok "(تجربة) هتتشال: $($s.Name)"; continue }
        if ($s.Status -ne 'Stopped') { Stop-Service -Name $s.Name -Force; (Get-Service $s.Name).WaitForStatus('Stopped', '00:00:45') }
        & sc.exe delete $s.Name | Out-Null
        if ($LASTEXITCODE -ne 0) { $problems.Add("الخدمة $($s.Name) ما اتشالتش (sc $LASTEXITCODE)") } else { Write-Ok "اتشالت: $($s.Name)" }
    }

    # ---- Database and SQL Server (only when asked). ----------------------------------------------------------------
    if ($RemoveDatabase) {
        Write-Step "قاعدة البيانات $Database"
        $reachable = $true
        try { [void](Invoke-Sql 'SELECT 1' -Scalar) } catch { $reachable = $false; Write-Ok "SQL Server ($SqlInstance) مش موجود أو مش شغال - مفيش قاعدة تتمسح." }
        $removeSql = $reachable
        if ($reachable) {
            $exists = Invoke-Sql "SELECT DB_ID(N'$Database')" -Scalar
            if ($exists -is [DBNull] -or $null -eq $exists) { Write-Ok 'القاعدة مش موجودة.' }
            else {
                if (-not $backupDir) { throw 'مفيش مجلد للنسخ الاحتياطية - القاعدة ما اتمسحتش.' }
                $backup = Join-Path $backupDir "${Database}_before-uninstall_$($stamp -replace '-', '_').bak"
                if ($DryRun) { Write-Ok "(تجربة) نسخة أخيرة: $backup ثم مسح القاعدة" }
                else {
                    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
                    $instanceName = if ($SqlInstance -match '\\(.+)$') { $Matches[1] } else { 'MSSQLSERVER' }
                    $sqlAccount = if ($instanceName -eq 'MSSQLSERVER') { 'NT SERVICE\MSSQLSERVER' } else { "NT SERVICE\MSSQL`$$instanceName" }
                    & icacls.exe $backupDir /grant "${sqlAccount}:(OI)(CI)M" | Out-Null
                    try {
                        Invoke-Sql "BACKUP DATABASE [$Database] TO DISK = N'$backup' WITH COPY_ONLY, CHECKSUM, INIT"
                        Invoke-Sql "RESTORE VERIFYONLY FROM DISK = N'$backup' WITH CHECKSUM"
                    } catch {
                        throw "النسخة الاحتياطية الأخيرة ما نجحتش، فالقاعدة ما اتمسحتش: $($_.Exception.Message)"
                    }
                    Write-Ok "نسخة أخيرة اتعملت واتأكدت سليمة: $backup"
                    Invoke-Sql "ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database];"
                    Write-Ok 'القاعدة اتمسحت.'
                }
            }

            # Another application's database in the same instance keeps SQL Server installed.
            $others = @()
            $c = New-SqlConnection 'master'
            try {
                $cmd = $c.CreateCommand()
                $cmd.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4 AND name <> N'$Database'"
                $r = $cmd.ExecuteReader(); while ($r.Read()) { $others += $r.GetString(0) }; $r.Dispose()
            } finally { $c.Dispose() }
            if ($others.Count -gt 0) {
                $removeSql = $false
                $problems.Add("SQL Server فضل متثبت: فيه قواعد تانية مش بتاعة فالح ($($others -join '، ')).")
                Write-Ok "SQL Server هيفضل: فيه قواعد تانية ($($others -join '، '))."
            }
        }

        if ($removeSql) {
            Write-Step 'إزالة SQL Server Express'
            $instanceName = if ($SqlInstance -match '\\(.+)$') { $Matches[1] } else { 'MSSQLSERVER' }
            if (-not $SqlSetupExe) {
                $SqlSetupExe = Get-ChildItem 'C:\Program Files\Microsoft SQL Server\*\Setup Bootstrap\SQL*\setup.exe' -ErrorAction SilentlyContinue |
                    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
            }
            if (-not $SqlSetupExe -or -not (Test-Path -LiteralPath $SqlSetupExe)) {
                $problems.Add('برنامج إزالة SQL Server مش موجود. شيله من Apps في ويندوز (Microsoft SQL Server 2022).')
            } elseif ($DryRun) { Write-Ok "(تجربة) هيتشال: $SqlSetupExe /ACTION=Uninstall /INSTANCENAME=$instanceName" }
            else {
                $p = Start-Process -FilePath $SqlSetupExe -ArgumentList "/Q /ACTION=Uninstall /FEATURES=SQLENGINE /INSTANCENAME=$instanceName" -Wait -PassThru
                if ($p.ExitCode -in 0, 3010) { Write-Ok "SQL Server اتشال$(if ($p.ExitCode -eq 3010) { ' (محتاج إعادة تشغيل)' })." }
                else { $problems.Add("إزالة SQL Server رجعت كود $($p.ExitCode). التفاصيل في C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log") }
            }
        }
    }

    # ---- Firewall, server address, certificate. ------------------------------------------------------------------
    Write-Step 'جدار الحماية والإعدادات والشهادة'
    $rules = @(Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue)
    foreach ($rule in $rules) { if ($DryRun) { Write-Ok "(تجربة) هتتشال قاعدة الجدار: $FirewallRule" } else { $rule | Remove-NetFirewallRule; Write-Ok "قاعدة الجدار اتشالت: $FirewallRule" } }
    if ([Environment]::GetEnvironmentVariable($ServerUrlVariable, 'Machine')) {
        if ($DryRun) { Write-Ok "(تجربة) هيتشال $ServerUrlVariable" } else { [Environment]::SetEnvironmentVariable($ServerUrlVariable, $null, 'Machine'); Write-Ok "اتشال $ServerUrlVariable" }
    }
    foreach ($store in 'Cert:\LocalMachine\My', 'Cert:\LocalMachine\Root') {
        foreach ($cert in @(Get-ChildItem $store -ErrorAction SilentlyContinue | Where-Object Subject -eq $CertificateSubject)) {
            if ($DryRun) { Write-Ok "(تجربة) هتتشال الشهادة من $store" } else { Remove-Item -LiteralPath $cert.PSPath -Force; Write-Ok "الشهادة اتشالت من $store" }
        }
    }

    # ---- Desktop shortcuts that point into C:\Falah. WshShortcut reads a file name through the ANSI code page, so
    # an Arabic-named shortcut is read through an ASCII-named copy. ---------------------------------------------------
    Write-Step 'الاختصارات'
    $shell = New-Object -ComObject WScript.Shell
    $root = [IO.Path]::GetFullPath($FalahRoot).TrimEnd('\') + '\'
    foreach ($dir in $DesktopDirs | Where-Object { $_ -and (Test-Path -LiteralPath $_) }) {
        foreach ($lnk in @(Get-ChildItem -LiteralPath $dir -Filter *.lnk -File -Force -ErrorAction SilentlyContinue)) {
            $copy = Join-Path $env:TEMP ('falah-read-' + [guid]::NewGuid().ToString('N') + '.lnk')
            try {
                Copy-Item -LiteralPath $lnk.FullName $copy -Force
                $target = $shell.CreateShortcut($copy).TargetPath
            } catch { $target = '' } finally { Remove-Item -LiteralPath $copy -Force -ErrorAction SilentlyContinue }
            if ($target -and $target.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { Remove-Path $lnk.FullName }
        }
    }

    # ---- Files. --------------------------------------------------------------------------------------------------
    Write-Step 'الملفات'
    $keep = @($backupDirs | ForEach-Object { [IO.Path]::GetFullPath($_).TrimEnd('\') })
    if ($KeepDir) { $keep += [IO.Path]::GetFullPath($KeepDir).TrimEnd('\') }
    $ours = @('Server', 'Cashier', 'CallCenter', 'Logs', 'Certificate', 'falah-server.cer')
    if (Test-Path -LiteralPath $FalahRoot) {
        foreach ($item in @(Get-ChildItem -LiteralPath $FalahRoot -Force)) {
            $full = $item.FullName.TrimEnd('\')
            if ($keep -contains $full) { continue }
            $base = $item.Name -replace '\.previous$', ''
            if ($ours -contains $base -or $base -like 'PrintAgent-*') { Remove-Path $full }
            elseif ($item.Name -eq 'Package') {
                foreach ($pkg in @(Get-ChildItem -LiteralPath $full -Directory -Force)) {
                    if ($keep -notcontains $pkg.FullName.TrimEnd('\')) { Remove-Path $pkg.FullName }
                }
            }
        }
    }
    Remove-Path $ProgramDataRoot
    foreach ($dir in $UserDataDirs) { Remove-Path $dir }

    $succeeded = $problems.Count -eq 0
    $message = if ($succeeded) { 'فالح اتشال من الجهاز ده.' } else { 'فالح اتشال، مع ملاحظات: ' + ($problems -join ' | ') }
    Write-Host ''
    Write-Host $message
}
catch {
    $message = $_.Exception.Message
    if ($problems.Count) { $message += ' | ' + ($problems -join ' | ') }
    Write-Host "FAILED: $message"
}
finally {
    Stop-Transcript | Out-Null
}

# The log goes where it survives: the backup folder (kept), else beside the uninstaller's temp files.
$kept = $log
if ($backupDir -and -not $DryRun) {
    try { New-Item -ItemType Directory -Force -Path $backupDir | Out-Null; $kept = Join-Path $backupDir (Split-Path $log -Leaf); Move-Item -LiteralPath $log $kept -Force } catch { $kept = $log }
}
if ($ResultFile) {
    [IO.File]::WriteAllLines($ResultFile, @($(if ($succeeded) { 'OK' } else { 'FAILED' }), $message, $kept), (New-Object Text.UTF8Encoding($true)))
}
if ($succeeded) { exit 0 } else { exit 1 }
