<#
.SYNOPSIS
    يثبّت برنامج الطباعة لمسار واحد (طابعة واحدة) كخدمة ويندوز على الجهاز اللي الطابعة متوصلة بيه.

.DESCRIPTION
    قبل التشغيل: من شاشة المستخدمين في برنامج الكاشير اعمل مستخدم للطباعة بصلاحية print_agent بس،
    وعرّف الطابعة في ويندوز (Printers & scanners) واعرف اسمها.

    1. يوثّق شهادة السيرفر.
    2. ينسخ البرنامج إلى C:\Falah\PrintAgent-<المسار>.
    3. يكتب الإعدادات في C:\ProgramData\Falah\PrintAgent\<المسار>، والرمز في ملف منفصل مقفول
       على حساب النظام والمسؤولين بس (مش في ملف الإعدادات).
    4. يسجّل الخدمة FalahPrintAgent-<المسار> (تقوم مع الجهاز وتتعاد لو وقعت)، أو يحدّثها لو موجودة.
    درج النقدية مقفول افتراضيًا لحد ما يتجرّب في المطعم.

.EXAMPLE
    .\Install-PrintAgent.ps1 -Route Receipt -PrinterName "XP-80C" -ServerUrl https://SERVER-PC:5443
#>
[CmdletBinding()]
param(
    [ValidateSet('Receipt', 'CallCenter', 'Charcoal', 'Grill', 'DeliveryOrder', 'DriverDispatch')]
    [string] $Route,
    [string] $PrinterName,
    [string] $ServerUrl,
    [string] $CertificatePath = ''
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Enter-Elevated $PSCommandPath $PSBoundParameters
Assert-Administrator
$package = $PSScriptRoot
$routes = 'Receipt', 'CallCenter', 'Charcoal', 'Grill', 'DeliveryOrder', 'DriverDispatch'
$labels = @{ Receipt = 'إيصال الكاشير'; CallCenter = 'الكول سنتر'; Charcoal = 'الفحم'; Grill = 'الشواية'; DeliveryOrder = 'طلب التوصيل'; DriverDispatch = 'خروج الطيار' }

if (-not $Route) {
    for ($i = 0; $i -lt $routes.Count; $i++) { Write-Host "  $($i + 1) = $($labels[$routes[$i]]) ($($routes[$i]))" }
    $n = [int](Read-Host 'المسار') - 1
    if ($n -lt 0 -or $n -ge $routes.Count) { throw 'رقم مسار غير صحيح.' }
    $Route = $routes[$n]
}
if (-not $PrinterName) {
    Write-Host '  الطابعات على الجهاز ده:'; Get-Printer | ForEach-Object { Write-Host "    - $($_.Name)" }
    $PrinterName = Read-Host 'اسم الطابعة بالظبط'
}
if (-not (Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue)) { throw "الطابعة '$PrinterName' مش متعرّفة في ويندوز على الجهاز ده." }
$ServerUrl = Read-ServerUrl $ServerUrl
$serviceName = "FalahPrintAgent-$Route"
Write-Host "تثبيت طباعة $($labels[$Route]) على $PrinterName" -ForegroundColor White

Write-Step 'شهادة السيرفر'
if (-not $CertificatePath) {
    $CertificatePath = @((Join-Path $package 'Certificate\falah-server.cer'), (Join-Path $FalahRoot 'falah-server.cer')) |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
Import-FalahCertificate $CertificatePath
if (-not (Test-FalahServer $ServerUrl 3)) { Write-Warning "الجهاز مش شايف السيرفر على $ServerUrl دلوقتي - الخدمة هتفضل تحاول لوحدها." }

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped') {
    Write-Step 'إيقاف خدمة الطباعة الحالية'
    Stop-Service -Name $serviceName; (Get-Service $serviceName).WaitForStatus('Stopped', '00:00:30')
}

Write-Step 'نسخ البرنامج'
$target = Join-Path $FalahRoot "PrintAgent-$Route"
Install-Folder (Join-Path $package 'PrintAgent') $target

Write-Step 'الإعدادات'
$configDir = Join-Path $env:ProgramData "Falah\PrintAgent\$Route"
New-Item -ItemType Directory -Force -Path $configDir | Out-Null
$configFile = Join-Path $configDir 'appsettings.json'
$source = if (Test-Path -LiteralPath $configFile) { $configFile } else { Join-Path $target 'appsettings.json' }
$config = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($p in @($config.PSObject.Properties | Where-Object Name -like '//*')) { $config.PSObject.Properties.Remove($p.Name) }
$config.ServerBaseAddress = $ServerUrl
$config.Route = $Route
$config.Transport = 'windows'
$config.WindowsPrinterName = $PrinterName
$config | Add-Member -NotePropertyName DataDirectory -NotePropertyValue $configDir -Force
$config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configFile -Encoding UTF8
Write-Ok "الإعدادات: $configFile"

$secrets = Join-Path $configDir 'appsettings.local.json'
# Setup.exe passes the PIN it asked for in its own window (the console cannot show Arabic on Windows 10).
$setupPin = $env:FALAH_SETUP_PRINT_PIN
if ($setupPin -or -not (Test-Path -LiteralPath $secrets) -or @('نعم','yes','y') -contains (Read-Host 'فيه رمز طباعة محفوظ. تغيّره؟ (نعم/yes أو لا)').Trim().ToLower()) {
    if ($setupPin) { $pin = $setupPin }
    else {
        $s = Read-Host 'رمز مستخدم الطباعة (صلاحية print_agent بس)' -AsSecureString
        $pin = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
    }
    if ($pin -notmatch '^\d{4,8}$') { throw 'الرمز لازم يكون من 4 لـ 8 أرقام.' }
    [IO.File]::WriteAllText($secrets, (@{ Pin = $pin } | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
    & icacls.exe $secrets /inheritance:r /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' | Out-Null
    Write-Ok 'الرمز اتحفظ في ملف مقفول على النظام والمسؤولين بس.'
}

Write-Step "خدمة $serviceName"
if ($existing) {
    $bin = '"{0}" "{1}" --service "--service-name={2}"' -f (Join-Path $target 'Falah.PrintAgent.exe'), $configFile, $serviceName
    & sc.exe config $serviceName binPath= $bin start= auto | Out-Null
    Start-Service -Name $serviceName; (Get-Service $serviceName).WaitForStatus('Running', '00:00:30')
    Write-Ok 'اتحدّثت واشتغلت.'
} else {
    & (Join-Path $package 'install-falah-print-agent-service.ps1') -Route $Route -PublishPath $target -ConfigPath $configDir -ServiceName $serviceName
}
Write-Host ''
Write-Host "تمام - طباعة $($labels[$Route]) شغالة كخدمة. السجل في $configDir" -ForegroundColor Green
Write-Host '  جرّب: اعمل طلب من الكاشير واتأكد إن الورقة طلعت على الطابعة دي بس.'
