<#
.SYNOPSIS
    يثبّت برنامج الكاشير أو الكول سنتر على جهاز في المطعم ويوصّله بالسيرفر.

.DESCRIPTION
    1. يوثّق شهادة السيرفر على الجهاز (falah-server.cer من جهاز السيرفر).
    2. يضبط عنوان السيرفر للجهاز كله (FALAH_SERVER_URL).
    3. ينسخ البرنامج إلى C:\Falah\Cashier أو C:\Falah\CallCenter ويعمل اختصار على سطح المكتب.
    4. يتأكد إن الجهاز شايف السيرفر.
    ينفع يتشغّل على جهاز السيرفر نفسه كمان.

.EXAMPLE
    .\Install-Station.ps1 -Role Cashier -ServerUrl https://SERVER-PC:5443
#>
[CmdletBinding()]
param(
    [ValidateSet('Cashier', 'CallCenter')]
    [string] $Role,
    [string] $ServerUrl,
    [string] $CertificatePath = ''
)
. (Join-Path $PSScriptRoot 'Common.ps1')
Enter-Elevated $PSCommandPath $PSBoundParameters
Assert-Administrator
$package = $PSScriptRoot

if (-not $Role) {
    $choice = Read-Host 'البرنامج: 1 = الكاشير والإدارة، 2 = الكول سنتر'
    $Role = switch ($choice.Trim()) { '1' { 'Cashier' } '2' { 'CallCenter' } default { throw 'اختار 1 أو 2.' } }
}
$ServerUrl = Read-ServerUrl $ServerUrl
$app = @{
    Cashier    = @{ Folder = 'Cashier';    Exe = 'Falah.Client.CashierWinUI.exe';    Name = 'فالح - الكاشير' }
    CallCenter = @{ Folder = 'CallCenter'; Exe = 'Falah.Client.CallCenterWinUI.exe'; Name = 'فالح - الكول سنتر' }
}[$Role]
Write-Host "تثبيت $($app.Name)" -ForegroundColor White

Write-Step 'شهادة السيرفر'
if (-not $CertificatePath) {
    $CertificatePath = @((Join-Path $package 'Certificate\falah-server.cer'), (Join-Path $FalahRoot 'falah-server.cer')) |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
Import-FalahCertificate $CertificatePath

Write-Step 'عنوان السيرفر'
[Environment]::SetEnvironmentVariable('FALAH_SERVER_URL', $ServerUrl, 'Machine')
Write-Ok "FALAH_SERVER_URL = $ServerUrl"

Write-Step 'نسخ البرنامج'
$target = Join-Path $FalahRoot $app.Folder
$running = Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($app.Exe)) -ErrorAction SilentlyContinue
if ($running) { throw "$($app.Name) مفتوح دلوقتي. اقفله (بعد ما تتأكد إن مفيش عملية دفع شغالة) وأعد التشغيل." }
Install-Folder (Join-Path $package $app.Folder) $target
New-DesktopShortcut $app.Name (Join-Path $target $app.Exe)

Write-Step 'الاتصال بالسيرفر'
if (Test-FalahServer $ServerUrl 3) { Write-Ok "الجهاز شايف السيرفر على $ServerUrl." }
else {
    Write-Warning "الجهاز مش شايف السيرفر على $ServerUrl. اتأكد إن السيرفر شغال، وإن الاسم مكتوب زي ما ظهر في آخر تثبيت السيرفر، وإن الجهازين على نفس الشبكة."
}
Write-Host ''
Write-Host "تمام - افتح $($app.Name) من سطح المكتب." -ForegroundColor Green
