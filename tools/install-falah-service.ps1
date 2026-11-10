<#
.SYNOPSIS
    ينشر Falah.Server ويسجّله كخدمة ويندوز تقوم مع الجهاز وتُعاد تلقائياً لو وقعت.

.DESCRIPTION
    الكاشير مش بيقدر يبيع والسيرفر واقع، ومحدش في مطبخ هيلاحظ إن نافذة كونسول اتقفلت.
    الخدمة بتحل ده: بتقوم مع الويندوز قبل ما حد يسجّل دخول، وويندوز نفسه بيعيد تشغيلها
    لو ماتت.

    الاسكربت ده بيعمل:
      1. ينشر السيرفر Release في مجلد ثابت (مش bin بتاع التطوير).
      2. ينشئ الخدمة معتمِدة على SQL Server عشان تقوم بعده مش قبله.
      3. يضبط إعادة التشغيل التلقائي عند الفشل (بعد 5 ثوانٍ، 3 مرات، ثم كل دقيقة).
      4. يشغّل الخدمة ويتأكد إنها بتردّ.

.NOTES
    لازم يتشغّل كمسؤول (Run as Administrator).

.EXAMPLE
    .\install-falah-service.ps1
    .\install-falah-service.ps1 -Uninstall
#>

[CmdletBinding()]
param(
    [string] $ServiceName   = 'FalahServer',
    [string] $PublishPath   = 'E:\falah-restaurant\FalahServer',
    [string] $ProjectPath   = "$PSScriptRoot\..\src\Falah.Server\Falah.Server.csproj",
    [string] $SqlService    = 'MSSQL$SQLEXPRESS',
    [string] $HealthUrl     = 'http://localhost:5151/health',
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'

function Assert-Administrator {
    $identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'لازم تشغّل الاسكربت ده كمسؤول: افتح PowerShell بـ "Run as Administrator" وأعد المحاولة.'
    }
}

function Stop-AndRemoveService {
    param([string] $Name)
    $existing = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $existing) { return $false }

    if ($existing.Status -ne 'Stopped') {
        Write-Host "إيقاف الخدمة القائمة $Name ..."
        Stop-Service -Name $Name -Force
        $existing.WaitForStatus('Stopped', '00:00:30')
    }
    # sc.exe delete وليس Remove-Service: الأخير مش موجود في Windows PowerShell 5.1
    & sc.exe delete $Name | Out-Null
    Start-Sleep -Seconds 2
    return $true
}

Assert-Administrator

if ($Uninstall) {
    if (Stop-AndRemoveService -Name $ServiceName) {
        Write-Host "تم حذف الخدمة $ServiceName." -ForegroundColor Green
    } else {
        Write-Host "الخدمة $ServiceName مش موجودة أصلاً."
    }
    return
}

# ---- 1. النشر --------------------------------------------------------------
# مجلد منفصل عن bin: تحديث أثناء التطوير مينفعش يبدّل الملف اللي الخدمة ماسكاه.
Write-Host "نشر السيرفر في $PublishPath ..."
Stop-AndRemoveService -Name $ServiceName | Out-Null

dotnet publish $ProjectPath -c Release -o $PublishPath --nologo
if ($LASTEXITCODE -ne 0) { throw 'فشل النشر؛ الخدمة لم تُسجَّل.' }

$exePath = Join-Path $PublishPath 'Falah.Server.exe'
if (-not (Test-Path $exePath)) { throw "الملف التنفيذي مش موجود بعد النشر: $exePath" }

# ---- 2. إنشاء الخدمة -------------------------------------------------------
# الاعتماد على SQL Server: من غيره الخدمة بتقوم قبل قاعدة البيانات وتفشل في أول اتصال.
$dependency = ''
if (Get-Service -Name $SqlService -ErrorAction SilentlyContinue) {
    $dependency = $SqlService
    Write-Host "الخدمة هتعتمد على $SqlService."
} else {
    Write-Warning "خدمة SQL باسم '$SqlService' مش موجودة - هيتم تسجيل الخدمة من غير اعتماد. عدّل -SqlService لو الاسم مختلف."
}

Write-Host "تسجيل الخدمة $ServiceName ..."
$binPath = '"' + $exePath + '"'
if ($dependency) {
    & sc.exe create $ServiceName binPath= $binPath start= auto depend= $dependency DisplayName= "Falah Restaurant Server" | Out-Null
} else {
    & sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= "Falah Restaurant Server" | Out-Null
}
if ($LASTEXITCODE -ne 0) { throw "فشل إنشاء الخدمة (كود $LASTEXITCODE)." }

& sc.exe description $ServiceName "الخادم الخلفي لنظام فالح - نقطة البيع والتقارير والنسخ الاحتياطي التلقائي." | Out-Null

# ---- 3. إعادة التشغيل التلقائي عند الفشل -----------------------------------
# ده بيت القصيد: reset= 86400 يعني عدّاد الفشل يرجع صفر كل يوم، فالانهيارات المتفرقة
# على مدار شهور ما تستهلكش المحاولات الثلاثة.
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/60000 | Out-Null
& sc.exe failureflag $ServiceName 1 | Out-Null

# ---- 4. التشغيل والتحقق ----------------------------------------------------
Write-Host "تشغيل الخدمة ..."
Start-Service -Name $ServiceName
(Get-Service -Name $ServiceName).WaitForStatus('Running', '00:00:60')

Write-Host "التحقق من أن السيرفر بيردّ على $HealthUrl ..."
$ok = $false
foreach ($attempt in 1..20) {
    Start-Sleep -Seconds 3
    try {
        Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5 | Out-Null
        $ok = $true; break
    } catch {
        # A failed HTTP response (including 500) is not a healthy deployment.
    }
}

if ($ok) {
    Write-Host ''
    Write-Host "تمام - الخدمة $ServiceName شغالة وبتردّ." -ForegroundColor Green
    Write-Host "هتقوم تلقائياً مع الجهاز، وويندوز هيعيد تشغيلها لو وقعت."
    Write-Host "النسخ الاحتياطي التلقائي شغال جوه السيرفر - راجع شاشة النسخ الاحتياطي بعد يوم."
} else {
    Write-Warning "الخدمة اتسجّلت لكن السيرفر مردّش خلال دقيقة. راجع سجل الأحداث:"
    Write-Warning "  Get-EventLog -LogName Application -Source $ServiceName -Newest 20"
}
