<#
.SYNOPSIS
    يثبّت سيرفر فالح (أو يحدّثه) كخدمة ويندوز على جهاز السيرفر، بـ HTTPS.

.DESCRIPTION
    بالترتيب:
      1. شهادة HTTPS للسيرفر في مخزن الجهاز (تُنشأ أول مرة فقط، ومن غير كلمة سر في أي ملف)،
         وتتصدّر falah-server.cer عشان باقي الأجهزة تثق فيها.
      2. قاعدة البيانات: تُنشأ لو مش موجودة، أو تتحدّث بعد نسخة احتياطية متحقَّق منها (Update-Database.ps1).
      3. نسخ البرنامج إلى C:\Falah\Server (والنسخة السابقة تتحفظ في Server.previous).
      4. خدمة FalahServer: تقوم مع الجهاز بعد SQL Server، وتتعاد لو وقعت.
      5. الإعدادات في بيئة الخدمة (مش في ملفات البرنامج، فالتحديث ما يمسحهاش).
      6. جدار الحماية: المنفذ للشبكة الخاصة المحلية بس.
      7. أول تثبيت: حساب المالك برمز تختاره، والرمز يتمسح من الإعدادات بعد إنشاء الحساب.

.EXAMPLE
    .\Install-Server.ps1
    .\Install-Server.ps1 -ServerName SERVER-PC -BackupCopyDir D:\FalahBackups
#>
[CmdletBinding()]
param(
    [string] $ServerName    = $env:COMPUTERNAME,
    [int]    $Port          = 5443,
    [string] $InstallDir    = 'C:\Falah\Server',
    [string] $BackupDir     = 'C:\Falah\Backups',
    [string] $BackupCopyDir = '',
    [string] $SqlInstance   = '.\SQLEXPRESS',
    [string] $SqlService    = 'MSSQL$SQLEXPRESS',
    [string] $Database      = 'Falah'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Enter-Elevated $PSCommandPath $PSBoundParameters
Assert-Administrator
$serviceName = 'FalahServer'
$package = $PSScriptRoot

Write-Host 'تثبيت سيرفر فالح' -ForegroundColor White
if (-not (Get-Service -Name $SqlService -ErrorAction SilentlyContinue)) {
    throw "خدمة SQL Server ($SqlService) مش موجودة على الجهاز ده. ثبّت SQL Server 2022 Express الأول (اسم المثيل SQLEXPRESS)."
}
if ((Get-Service $SqlService).Status -ne 'Running') { Start-Service $SqlService }

# ---- 1. Certificate --------------------------------------------------------------------------------
Write-Step 'شهادة HTTPS'
$cert = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.Subject -eq "CN=$CertificateSubject" -and $_.NotAfter -gt (Get-Date).AddDays(30) -and $_.HasPrivateKey } |
    Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $cert) {
    $ips = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notlike '169.254.*' } | ForEach-Object IPAddress)
    $san = (@("DNS=$ServerName", 'DNS=localhost') + ($ips | ForEach-Object { "IPAddress=$_" })) -join '&'
    $cert = New-SelfSignedCertificate -Subject "CN=$CertificateSubject" -FriendlyName 'Falah Server' `
        -TextExtension @("2.5.29.17={text}$san", '2.5.29.37={text}1.3.6.1.5.5.7.3.1') `
        -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(10) `
        -CertStoreLocation Cert:\LocalMachine\My
    Write-Ok "شهادة جديدة لـ $ServerName ($($ips -join '، ')) - صالحة 10 سنين."
} else { Write-Ok "الشهادة الحالية صالحة حتى $($cert.NotAfter.ToString('yyyy-MM-dd'))." }
New-Item -ItemType Directory -Force -Path $FalahRoot | Out-Null
$cerFile = Join-Path $FalahRoot 'falah-server.cer'
Export-Certificate -Cert $cert -FilePath $cerFile -Force | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $package 'Certificate') -ErrorAction SilentlyContinue | Out-Null
try { Copy-Item $cerFile (Join-Path $package 'Certificate\falah-server.cer') -Force } catch { }
Import-FalahCertificate $cerFile
Write-Ok "ملف الشهادة لباقي الأجهزة: $cerFile (واتنسخ في مجلد Certificate جوه الحزمة)."

# ---- 2. Stop the running service, then the database -------------------------------------------------
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped') {
    Write-Step 'إيقاف السيرفر الحالي'
    Stop-Service -Name $serviceName -Force; (Get-Service $serviceName).WaitForStatus('Stopped', '00:00:45')
    Write-Ok 'اتوقف.'
}
& (Join-Path $package 'Update-Database.ps1') -SqlInstance $SqlInstance -Database $Database -BackupDir $BackupDir `
    -ScriptPath (Join-Path $package 'Database\migrations-idempotent.sql')

# The service runs as LocalSystem; give that account the database rights the server uses
# (read/write, its own backups, and restore from the Backup screen).
Write-Step 'صلاحيات الخدمة على قاعدة البيانات'
Invoke-SqlNonQuery $SqlInstance 'master' @"
IF SUSER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;
IF IS_SRVROLEMEMBER(N'dbcreator', N'NT AUTHORITY\SYSTEM') = 0 ALTER SERVER ROLE [dbcreator] ADD MEMBER [NT AUTHORITY\SYSTEM];
"@
Invoke-SqlNonQuery $SqlInstance $Database @"
IF USER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
IF IS_ROLEMEMBER(N'db_owner', N'NT AUTHORITY\SYSTEM') = 0 ALTER ROLE [db_owner] ADD MEMBER [NT AUTHORITY\SYSTEM];
"@
$instanceName = if ($SqlInstance -match '\\(.+)$') { $Matches[1] } else { 'MSSQLSERVER' }
$sqlAccount = if ($instanceName -eq 'MSSQLSERVER') { 'NT SERVICE\MSSQLSERVER' } else { "NT SERVICE\MSSQL`$$instanceName" }
foreach ($dir in @($BackupDir, $BackupCopyDir) | Where-Object { $_ }) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    & icacls.exe $dir /grant "${sqlAccount}:(OI)(CI)M" | Out-Null
}
Write-Ok 'تمام.'
$firstInstall = [int](Invoke-SqlScalar $SqlInstance $Database 'SELECT COUNT(*) FROM Users') -eq 0

# ---- 3. Program files ----------------------------------------------------------------------------
Write-Step 'نسخ البرنامج'
Install-Folder (Join-Path $package 'Server') $InstallDir
$exe = Join-Path $InstallDir 'Falah.Server.exe'

# ---- 4. Service ----------------------------------------------------------------------------------
Write-Step 'خدمة FalahServer'
if (-not (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    & sc.exe create $serviceName binPath= "`"$exe`"" start= auto depend= $SqlService DisplayName= 'Falah Restaurant Server' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "فشل إنشاء الخدمة (كود $LASTEXITCODE)." }
    & sc.exe description $serviceName 'خادم نظام فالح: نقطة البيع والتقارير والنسخ الاحتياطي التلقائي.' | Out-Null
    Write-Ok 'اتسجلت.'
} else {
    & sc.exe config $serviceName binPath= "`"$exe`"" start= auto | Out-Null
    Write-Ok 'موجودة - اتحدّث مسارها.'
}
& sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/5000/restart/60000 | Out-Null
& sc.exe failureflag $serviceName 1 | Out-Null

# ---- 5. Settings (service environment survives program updates) ----------------------------------
$settings = [ordered]@{
    'ASPNETCORE_ENVIRONMENT'                             = 'Production'
    'ConnectionStrings__Falah'                           = "Server=$SqlInstance;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
    'Kestrel__Endpoints__Https__Url'                     = "https://0.0.0.0:$Port"
    'Kestrel__Endpoints__Https__Certificate__Subject'    = $CertificateSubject
    'Kestrel__Endpoints__Https__Certificate__Store'      = 'My'
    'Kestrel__Endpoints__Https__Certificate__Location'   = 'LocalMachine'
    'Kestrel__Endpoints__Https__Certificate__AllowInvalid' = 'false'   # trusted in LocalMachine\Root above
    'Backup__DefaultDirectory'                           = $BackupDir
}
if ($BackupCopyDir) { $settings['Backup__CopyToDirectory'] = $BackupCopyDir }
function Set-ServiceEnvironment([hashtable]$values, [string]$extra) {
    $lines = @($values.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
    if ($extra) { $lines += $extra }
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName" -Name Environment -Type MultiString -Value $lines
}

$bootstrap = ''
if ($firstInstall) {
    Write-Step 'أول تثبيت: حساب المالك'
    Write-Host '    اختار رمز دخول للمالك (4 أرقام أو أكتر، مش متكرر ولا متسلسل). هتغيّره وتضيف باقي المستخدمين من شاشة المستخدمين.'
    $p1 = Read-Host '    رمز المالك' -AsSecureString
    $p2 = Read-Host '    اكتبه تاني' -AsSecureString
    $plain = { param($s) [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)) }
    $pin = & $plain $p1
    if ($pin -ne (& $plain $p2)) { throw 'الرمزين مش زي بعض.' }
    if ($pin -notmatch '^\d{4,8}$') { throw 'الرمز لازم يكون من 4 لـ 8 أرقام.' }
    $bootstrap = "Bootstrap__OwnerPin=$pin"
}
Set-ServiceEnvironment $settings $bootstrap

# ---- 6. Firewall ---------------------------------------------------------------------------------
Write-Step 'جدار الحماية'
Get-NetFirewallRule -DisplayName 'Falah Server' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName 'Falah Server' -Direction Inbound -Protocol TCP -LocalPort $Port `
    -Profile Private -RemoteAddress LocalSubnet -Action Allow | Out-Null
Write-Ok "المنفذ $Port مفتوح للشبكة المحلية الخاصة بس. اتأكد إن شبكة المطعم متصنّفة Private في ويندوز."

# ---- 7. Start and verify -------------------------------------------------------------------------
Write-Step 'تشغيل السيرفر'
Start-Service -Name $serviceName
$url = "https://${ServerName}:$Port"
if (-not (Test-FalahServer "https://localhost:$Port" 25)) {
    if ($bootstrap) { Set-ServiceEnvironment $settings '' }
    throw "السيرفر مردّش. راجع: Get-EventLog -LogName Application -Source $serviceName -Newest 20"
}
if ($bootstrap) {
    Set-ServiceEnvironment $settings ''   # the owner's PIN leaves the configuration as soon as the account exists
    Restart-Service -Name $serviceName
    if (-not (Test-FalahServer "https://localhost:$Port" 25)) { throw 'السيرفر مردّش بعد إعادة التشغيل.' }
    if ([int](Invoke-SqlScalar $SqlInstance $Database 'SELECT COUNT(*) FROM Users') -eq 0) {
        throw 'السيرفر رفض رمز المالك (غالبًا سهل التخمين: متكرر أو متسلسل). شغّل 1-Install-Server.cmd تاني واختار رمز تاني.'
    }
    Write-Ok 'حساب المالك اتعمل، والرمز اتمسح من الإعدادات.'
}
Write-Host ''
Write-Host "تمام - السيرفر شغال: $url" -ForegroundColor Green
Write-Host "  على باقي الأجهزة استخدم العنوان ده بالظبط: $url"
Write-Host "  وانسخ معاك ملف الشهادة: $cerFile"
Write-Host "  النسخ الاحتياطي التلقائي في: $BackupDir$(if ($BackupCopyDir) { " ونسخة في $BackupCopyDir" })"
