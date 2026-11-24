<#
.SYNOPSIS
    ينشئ قاعدة بيانات فالح لو مش موجودة، أو يحدّثها بأمان لو موجودة.

.DESCRIPTION
    - قاعدة مش موجودة: ينشئها ويطبّق كل الترحيلات.
    - قاعدة موجودة ومحتاجة تحديث: ياخد نسخة احتياطية كاملة (COPY_ONLY) ويتحقق منها قبل أي تغيير،
      وبعدين يطبّق السكربت (آمن للتكرار) ويقارن عدد الطلبات والورديات قبل وبعد.
    - قاعدة أحدث من الحزمة: يرفض (لا يرجّع قاعدة لإصدار أقدم).
    شغّله والبرنامج متوقف وما فيش وردية مفتوحة. Install-Server.ps1 بيستدعيه تلقائيًا.
#>
[CmdletBinding()]
param(
    [string] $SqlInstance = '.\SQLEXPRESS',
    [string] $Database    = 'Falah',
    [string] $ScriptPath  = '',
    [string] $BackupDir   = 'C:\Falah\Backups',
    [switch] $Yes
)
. (Join-Path $PSScriptRoot 'Common.ps1')
# Not in the param default: Windows PowerShell 5.1 leaves $PSScriptRoot empty there under -File with [CmdletBinding()].
if (-not $ScriptPath) { $ScriptPath = Join-Path $PSScriptRoot 'Database\migrations-idempotent.sql' }
Enter-Elevated $PSCommandPath $PSBoundParameters
Assert-Administrator

if ($Database -notmatch '^[A-Za-z0-9_]+$') { throw 'اسم قاعدة البيانات غير صالح.' }
if (-not (Test-Path -LiteralPath $ScriptPath)) { throw "سكربت قاعدة البيانات مش موجود: $ScriptPath" }
$inScript = @(Get-ScriptMigrations $ScriptPath)
Write-Step "قاعدة البيانات $Database على $SqlInstance (الحزمة فيها $($inScript.Count) ترحيل)"

try { [void](Invoke-SqlScalar $SqlInstance 'master' 'SELECT 1') }
catch { throw "مش قادر أوصل لـ SQL Server على $SqlInstance. اتأكد إن SQL Server Express متثبت وشغال. ($($_.Exception.Message))" }

$exists = Invoke-SqlScalar $SqlInstance 'master' "SELECT DB_ID(N'$Database')"
if ($exists -is [DBNull] -or $null -eq $exists) {
    Write-Note 'القاعدة مش موجودة - هيتم إنشاؤها جديدة.'
    Invoke-SqlNonQuery $SqlInstance 'master' "CREATE DATABASE [$Database]"
    $n = Invoke-SqlScriptFile $SqlInstance $Database $ScriptPath
    $applied = @(Get-AppliedMigrations $SqlInstance $Database)
    Write-Ok "تم إنشاء القاعدة وتطبيق $($applied.Count) ترحيل ($n دفعة)."
    return
}

$applied = @(Get-AppliedMigrations $SqlInstance $Database)
$unknown = @($applied | Where-Object { $inScript -notcontains $_ })
if ($unknown.Count -gt 0) {
    throw "القاعدة أحدث من الحزمة دي (فيها ترحيل مش موجود في السكربت: $($unknown[-1])). استخدم الحزمة الأحدث - لا يتم الرجوع بالقاعدة لإصدار أقدم."
}
$pending = @($inScript | Where-Object { $applied -notcontains $_ })
if ($pending.Count -eq 0) { Write-Ok "القاعدة محدّثة بالفعل ($($applied.Count) ترحيل). مفيش تغيير."; return }

Write-Note "محتاجة $($pending.Count) تحديث: $($pending -join '، ')"
if (-not $Yes) {
    $answer = Read-Host 'هيتم أخذ نسخة احتياطية ثم تحديث القاعدة. البرنامج لازم يكون متوقف ومفيش وردية مفتوحة. اكتب "نعم" أو yes للمتابعة'
    if (@('نعم','yes','y') -notcontains $answer.Trim().ToLower()) { throw 'تم الإلغاء - لم يتغير شيء.' }
}

# The backup is written by the SQL Server service account, so it needs write access to the folder.
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$instanceName = if ($SqlInstance -match '\\(.+)$') { $Matches[1] } else { 'MSSQLSERVER' }
$sqlAccount = if ($instanceName -eq 'MSSQLSERVER') { 'NT SERVICE\MSSQLSERVER' } else { "NT SERVICE\MSSQL`$$instanceName" }
& icacls.exe $BackupDir /grant "${sqlAccount}:(OI)(CI)M" | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $BackupDir "${Database}_before-update_$stamp.bak"
Write-Step "نسخة احتياطية: $backup"
Invoke-SqlNonQuery $SqlInstance 'master' "BACKUP DATABASE [$Database] TO DISK = N'$backup' WITH COPY_ONLY, CHECKSUM, INIT"
Invoke-SqlNonQuery $SqlInstance 'master' "RESTORE VERIFYONLY FROM DISK = N'$backup' WITH CHECKSUM"
Write-Ok 'النسخة الاحتياطية اتعملت واتأكدت سليمة.'

$books = "SELECT CONCAT((SELECT COUNT(*) FROM Orders), '/', (SELECT ISNULL(SUM(TotalPiastres),0) FROM Orders), ' طلب، ', (SELECT COUNT(*) FROM Shifts), ' وردية، ', (SELECT COUNT(*) FROM TreasuryMovements), ' حركة خزينة')"
$before = Invoke-SqlScalar $SqlInstance $Database $books
Write-Step 'تطبيق التحديث'
[void](Invoke-SqlScriptFile $SqlInstance $Database $ScriptPath)
$after = Invoke-SqlScalar $SqlInstance $Database $books
$nowApplied = @(Get-AppliedMigrations $SqlInstance $Database)
Write-Ok "تم: $($nowApplied.Count) ترحيل."
Write-Ok "قبل: $before"
Write-Ok "بعد: $after"
if ($before -ne $after) { Write-Warning 'الأرقام اتغيرت بعد التحديث! لا تشغّل البرنامج. ارجع للنسخة الاحتياطية واتصل بالمطوّر.' }
else { Write-Ok "البيانات زي ما هي. النسخة الاحتياطية للرجوع: $backup" }
