# Shared helpers for the Falah installation scripts. Dot-sourced; never run on its own.
# Windows PowerShell 5.1 compatible. Messages are Arabic (they are read by whoever installs on site).

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$script:FalahRoot          = 'C:\Falah'
$script:CertificateSubject = 'falah-server'
$script:DefaultPort        = 5443

function Write-Step([string]$text) { Write-Host ''; Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Ok([string]$text)   { Write-Host "    $text" -ForegroundColor Green }
function Write-Note([string]$text) { Write-Host "    $text" -ForegroundColor Yellow }

function Test-Administrator {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Relaunch the calling script elevated (Windows asks for permission), in a window that stays open.
function Enter-Elevated([string]$scriptPath, [hashtable]$bound) {
    if (Test-Administrator) { return }
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-NoExit', '-File', "`"$scriptPath`"")
    foreach ($entry in $bound.GetEnumerator()) {
        if ($entry.Value -is [switch]) { if ($entry.Value) { $arguments += "-$($entry.Key)" } }
        else { $arguments += "-$($entry.Key)"; $arguments += "`"$($entry.Value)`"" }
    }
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList ($arguments -join ' ')
    exit
}

function Assert-Administrator {
    if (-not (Test-Administrator)) { throw 'لازم يتشغّل كمسؤول: كليك يمين على الملف واختار "Run as administrator".' }
}

# Mirror a published folder into its install location, keeping the previous version beside it for rollback.
function Install-Folder([string]$source, [string]$target) {
    # Not Test-Path -LiteralPath "$source\*": -LiteralPath takes '*' as a file name, so that check refused every
    # folder (first real install, 2026-11-26).
    $any = if (Test-Path -LiteralPath $source -PathType Container) { Get-ChildItem -LiteralPath $source -Force | Select-Object -First 1 }
    if (-not $any) { throw "مجلد البرنامج فاضي أو مش موجود: $source" }
    if (Test-Path -LiteralPath $target) {
        $previous = "$target.previous"
        if (Test-Path -LiteralPath $previous) { Remove-Item -LiteralPath $previous -Recurse -Force }
        & robocopy.exe $target $previous /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "تعذّر حفظ النسخة السابقة في $previous" }
        Write-Ok "النسخة السابقة محفوظة في $previous"
    }
    & robocopy.exe $source $target /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:2 /XF appsettings.local.json | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "تعذّر نسخ البرنامج إلى $target (robocopy $LASTEXITCODE)" }
    Write-Ok "البرنامج في $target"
}

# Trust the server's certificate on this machine (Local Machine \ Trusted Root).
function Import-FalahCertificate([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "ملف شهادة السيرفر مش موجود: $path - انسخه من جهاز السيرفر (C:\Falah\falah-server.cer)."
    }
    $cert = New-Object Security.Cryptography.X509Certificates.X509Certificate2($path)
    $existing = Get-ChildItem Cert:\LocalMachine\Root | Where-Object Thumbprint -eq $cert.Thumbprint
    if ($existing) { Write-Ok 'شهادة السيرفر موثوقة بالفعل على الجهاز ده.'; return }
    Import-Certificate -FilePath $path -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Write-Ok "تم توثيق شهادة السيرفر ($($cert.Thumbprint.Substring(0,8))...)."
}

function Test-FalahServer([string]$serverUrl, [int]$attempts = 1) {
    $url = $serverUrl.TrimEnd('/') + '/health'
    for ($i = 1; $i -le $attempts; $i++) {
        try { $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5; if ($r.StatusCode -eq 200) { return $true } } catch { }
        if ($i -lt $attempts) { Start-Sleep -Seconds 3 }
    }
    return $false
}

function Read-ServerUrl([string]$value) {
    if (-not $value) { $value = Read-Host 'عنوان السيرفر (مثال: https://SERVER-PC:5443)' }
    $uri = $null
    if (-not [Uri]::TryCreate($value.Trim(), [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https') {
        throw 'العنوان لازم يبدأ بـ https:// ويكون باسم السيرفر اللي في الشهادة، مثل https://SERVER-PC:5443'
    }
    return $uri.GetLeftPart([UriPartial]::Authority)
}

function New-DesktopShortcut([string]$name, [string]$target) {
    $desktop = [Environment]::GetFolderPath('CommonDesktopDirectory')
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut((Join-Path $desktop "$name.lnk"))
    $link.TargetPath = $target
    $link.WorkingDirectory = Split-Path $target -Parent
    $link.IconLocation = "$target,0"
    $link.Save()
    Write-Ok "اختصار على سطح المكتب: $name"
}

# ---- SQL Server (no sqlcmd needed: System.Data.SqlClient ships with Windows) ----

function New-SqlConnection([string]$instance, [string]$database) {
    $c = New-Object System.Data.SqlClient.SqlConnection("Server=$instance;Database=$database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15")
    $c.Open(); return $c
}

function Invoke-SqlScalar([string]$instance, [string]$database, [string]$sql) {
    $c = New-SqlConnection $instance $database
    try { $cmd = $c.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 0; return $cmd.ExecuteScalar() } finally { $c.Dispose() }
}

function Invoke-SqlNonQuery([string]$instance, [string]$database, [string]$sql) {
    $c = New-SqlConnection $instance $database
    try { $cmd = $c.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 0; [void]$cmd.ExecuteNonQuery() } finally { $c.Dispose() }
}

# Runs a script made of GO-separated batches (the EF idempotent migration script) in one connection.
function Invoke-SqlScriptFile([string]$instance, [string]$database, [string]$path) {
    $text = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    $batches = [regex]::Split($text, '(?im)^\s*GO\s*$') | Where-Object { $_.Trim() }
    $c = New-SqlConnection $instance $database
    try {
        foreach ($batch in $batches) { $cmd = $c.CreateCommand(); $cmd.CommandText = $batch; $cmd.CommandTimeout = 0; [void]$cmd.ExecuteNonQuery() }
    } finally { $c.Dispose() }
    return $batches.Count
}

function Get-ScriptMigrations([string]$path) {
    $text = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    [regex]::Matches($text, "INSERT INTO \[__EFMigrationsHistory\] \(\[MigrationId\], \[ProductVersion\]\)\s+VALUES \(N'([^']+)'") |
        ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
}

function Get-AppliedMigrations([string]$instance, [string]$database) {
    $exists = Invoke-SqlScalar $instance $database "SELECT OBJECT_ID(N'[__EFMigrationsHistory]')"
    if ($exists -is [DBNull] -or $null -eq $exists) { return @() }
    $c = New-SqlConnection $instance $database
    try {
        $cmd = $c.CreateCommand(); $cmd.CommandText = 'SELECT MigrationId FROM [__EFMigrationsHistory]'
        $r = $cmd.ExecuteReader(); $list = @(); while ($r.Read()) { $list += $r.GetString(0) }; $r.Dispose(); return $list
    } finally { $c.Dispose() }
}
