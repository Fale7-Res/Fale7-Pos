[CmdletBinding()]
param([switch]$NoRestore,[switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repo = [IO.Path]::GetFullPath((Join-Path $workspace '..'))
$iscc = Join-Path $workspace 'Tools\InnoSetup\ISCC.exe'
$iss = Join-Path $PSScriptRoot 'Falah-Restaurant.iss'
$output = Join-Path $workspace 'Output\Falah-Restaurant-Setup.exe'
$buildLog = Join-Path $workspace 'Logs\installer-build.log'

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
foreach ($oldArtifact in @($output, (Join-Path $workspace 'Output\Falah-Restaurant-Setup.sha256.txt'))) {
    if (Test-Path -LiteralPath $oldArtifact) {
        Write-Host "Removing previous installer artifact: $oldArtifact"
        Remove-Item -LiteralPath $oldArtifact -Force
    }
}

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot 'Build-Payload.ps1') -NoRestore:$NoRestore *>&1 | Tee-Object -FilePath (Join-Path $workspace 'Logs\payload-build.log')
    if ($LASTEXITCODE -ne 0) { throw "Application publish failed. See $(Join-Path $workspace 'Logs\payload-build.log')." }
}
if (-not (Test-Path $iscc)) { throw "Inno Setup compiler is missing: $iscc. Install the signed compiler from Tools\innosetup-6.7.4.exe." }
$compilerSignature = Get-AuthenticodeSignature -LiteralPath $iscc
if ($compilerSignature.Status -ne 'Valid') { throw "Inno Setup compiler signature is not valid: $($compilerSignature.Status)." }
$packages = @(
    @{ Path=(Join-Path $workspace 'Dependencies\Redistributables\VC_redist.x64.exe'); Hash='843068991DAAA1F73AD9F6239BCE4D0F6A07A51F18C37EA2A867E9BECA71295C'; Signer='Microsoft Corporation' },
    @{ Path=(Join-Path $workspace 'Dependencies\Offline Installers\SQLEXPR_x64_ENU.exe'); Hash='2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05'; Signer='Microsoft Corporation' }
)
foreach ($package in $packages) {
    if (-not (Test-Path $package.Path)) { throw "Required offline package missing: $($package.Path)" }
    $hash = (Get-FileHash -LiteralPath $package.Path -Algorithm SHA256).Hash
    $signature = Get-AuthenticodeSignature -LiteralPath $package.Path
    if ($hash -ne $package.Hash -or $signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike "*$($package.Signer)*") {
        throw "Dependency integrity check failed for $($package.Path)."
    }
}
$required = @(
    'Application Builds\Cashier\Falah.Client.CashierWinUI.exe',
    'Application Builds\CallCenter\Falah.Client.CallCenterWinUI.exe',
    'Application Builds\Server\Falah.Server.exe',
    'Application Builds\PrintAgent\Falah.PrintAgent.exe'
)
foreach ($relative in $required) { if (-not (Test-Path (Join-Path $workspace $relative))) { throw "Published module missing: $relative" } }
$env:FALAH_INSTALLER_WORKSPACE = $workspace
& $iscc /Q $iss 2>&1 | Tee-Object -FilePath $buildLog
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE. See $buildLog." }
if (-not (Test-Path $output)) { throw "Installer compiler succeeded but did not create $output." }
$hash = Get-FileHash -LiteralPath $output -Algorithm SHA256
"Installer=$output`r`nLength=$((Get-Item $output).Length)`r`nSHA256=$($hash.Hash)`r`nBuiltAt=$(Get-Date -Format o)" | Set-Content -LiteralPath (Join-Path $workspace 'Output\Falah-Restaurant-Setup.sha256.txt') -Encoding UTF8
Write-Host "Created $output"
