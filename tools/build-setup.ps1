<#
.SYNOPSIS
    Builds Falah-Setup-<id>.exe: one installer for every restaurant PC, holding the installation
    package (tools\build-install-package.ps1) and the offline SQL Server 2022 Express installer.

.DESCRIPTION
    Uses the newest artifacts\install\Falah-Install-<id> folder unless -PackageDir is given; build the
    package first. Checks that the SQL Server installer is Microsoft-signed before bundling it.
    -DryRun builds a test variant that needs no administrator rights and installs nothing: it extracts
    the files and writes the steps it would run to dryrun-steps.txt.

.EXAMPLE
    .\tools\build-install-package.ps1
    .\tools\build-setup.ps1
#>
[CmdletBinding()]
param(
    [string] $PackageDir = '',
    [string] $SqlInstaller = 'E:\offline installer dependencies\Dependencies\Offline Installers\SQLEXPR_x64_ENU.exe',
    [string] $Compiler = '',
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outDir = Join-Path $repo 'artifacts\install'

if (-not $PackageDir) {
    $PackageDir = Get-ChildItem $outDir -Directory -Filter 'Falah-Install-*' | Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $PackageDir -or -not (Test-Path (Join-Path $PackageDir 'Run-Step.ps1'))) {
    throw "No installation package with Run-Step.ps1 found. Run tools\build-install-package.ps1 first. ($PackageDir)"
}
$id = (Split-Path $PackageDir -Leaf) -replace '^Falah-Install-', ''

$signature = Get-AuthenticodeSignature $SqlInstaller
$version = (Get-Item $SqlInstaller).VersionInfo.ProductVersion
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'CN=Microsoft Corporation' -or $version -notlike '16.*') {
    throw "SQL Server installer is not a Microsoft-signed SQL Server 2022 file: $SqlInstaller ($($signature.Status), $version)"
}

if (-not $Compiler) {
    $Compiler = @('E:\offline installer dependencies\Tools\InnoSetup\ISCC.exe',
                  "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $Compiler) { throw 'Inno Setup compiler (ISCC.exe) not found; pass -Compiler.' }

# Inno Setup reads a script as UTF-8 only with a BOM; compile a BOM copy so the Arabic text survives.
$script = Join-Path $outDir 'FalahSetup.build.iss'
[IO.File]::WriteAllText($script, [IO.File]::ReadAllText((Join-Path $repo 'tools\setup\FalahSetup.iss'), [Text.Encoding]::UTF8),
    (New-Object Text.UTF8Encoding($true)))
$defines = @("/DPkgId=$id", "/DPkgDir=$PackageDir", "/DSqlInstaller=$SqlInstaller", "/DOutDir=$outDir")
if ($DryRun) { $defines += '/DDryRun' }
Write-Host "Setup for $id$(if ($DryRun) { ' (dry run)' }) from $PackageDir"
& $Compiler /Q @defines $script
$code = $LASTEXITCODE
Remove-Item $script -ErrorAction SilentlyContinue
if ($code -ne 0) { throw "ISCC failed ($code)" }

$exe = Join-Path $outDir ("Falah-Setup-$id" + $(if ($DryRun) { '-DRYRUN' } else { '' }) + '.exe')
$hash = (Get-FileHash $exe).Hash
"{0}  {1}" -f $hash, (Split-Path $exe -Leaf) | Set-Content "$exe.sha256.txt" -Encoding ASCII
Write-Host ("Done: {0} ({1:N0} MB), SHA-256 {2}" -f $exe, ((Get-Item $exe).Length / 1MB), $hash)
