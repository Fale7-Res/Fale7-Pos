<#
.SYNOPSIS
    Builds the self-contained installation package for the restaurant: every program published for
    win-x64 with its own .NET runtime (nothing to install on the restaurant PCs), the offline
    migration script, the deploy/ installers and the Arabic guide, plus a SHA-256 manifest and a zip.

.DESCRIPTION
    Never connects to a database: the migration script is generated against a database name that
    does not exist. Run from the repository on the development machine (not from a long scratch
    path: Windows App SDK intermediates exceed 260 characters there).

.EXAMPLE
    .\tools\build-install-package.ps1
#>
[CmdletBinding()]
param([string] $OutputRoot = '')
$ErrorActionPreference = 'Stop'
# Not in the param default: Windows PowerShell 5.1 leaves $PSScriptRoot empty there under -File with [CmdletBinding()].
if (-not $OutputRoot) { $OutputRoot = Join-Path $PSScriptRoot '..\artifacts\install' }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repo

# Release id = fingerprint of every .cs file under src and tests (same method as the release candidates).
$files = Get-ChildItem "$repo\src", "$repo\tests" -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object { $_.FullName.Substring($repo.Length).ToLowerInvariant() }
$sb = New-Object Text.StringBuilder
foreach ($f in $files) { [void]$sb.Append($f.FullName.Substring($repo.Length + 1).Replace('\', '/').ToLowerInvariant() + ':' + (Get-FileHash $f.FullName).Hash + "`n") }
$fingerprint = ([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($sb.ToString())) | ForEach-Object { $_.ToString('X2') }) -join ''
$id = $fingerprint.Substring(0, 16)
$commit = (git rev-parse --short HEAD).Trim()
$dirty = (git status --porcelain -- src tests deploy | Measure-Object).Count -gt 0
$out = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) "Falah-Install-$id"
Write-Host "Package $id from $commit$(if ($dirty) { ' (+ uncommitted changes)' }) -> $out"
if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Force -Path $out | Out-Null
$log = Join-Path $out 'build.log'

function Publish([string]$project, [string]$folder, [string[]]$extra) {
    Write-Host "  publish $folder"
    # The package id and commit become every program's version, so each log line (server, station, print
    # agent) names the exact build that wrote it (runtime logging audit 2026-11-23).
    $stamp = "-p:InformationalVersion=$id-$commit$(if ($dirty) { '-dirty' })"
    & dotnet publish $project -c Release -r win-x64 --self-contained true -o (Join-Path $out $folder) -nologo $stamp @extra *>> $log
    if ($LASTEXITCODE -ne 0) { throw "publish failed for $project (see $log)" }
}
Publish 'src/Falah.Server/Falah.Server.csproj' 'Server' @()
Publish 'src/Falah.PrintAgent/Falah.PrintAgent.csproj' 'PrintAgent' @()
Publish 'src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj' 'Cashier' @('-p:Platform=x64')
Publish 'src/Falah.Client.CallCenterWinUI/Falah.Client.CallCenterWinUI.csproj' 'CallCenter' @('-p:Platform=x64')
Remove-Item (Join-Path $out 'Server\appsettings.Development.json') -ErrorAction SilentlyContinue
Get-ChildItem $out -Recurse -Filter appsettings.local.json | Remove-Item -Force

Write-Host '  migration script (offline)'
New-Item -ItemType Directory -Force -Path (Join-Path $out 'Database') | Out-Null
$env:ConnectionStrings__Falah = 'Server=.\SQLEXPRESS;Database=FalahDesignTime_DoNotConnect;Trusted_Connection=True;TrustServerCertificate=True;'
try {
    & dotnet tool restore *>> $log
    & dotnet ef migrations script --idempotent --configuration Release --project src/Falah.Infrastructure `
        --startup-project src/Falah.Server -o (Join-Path $out 'Database\migrations-idempotent.sql') *>> $log
    if ($LASTEXITCODE -ne 0) { throw "migration script generation failed (see $log)" }
} finally { Remove-Item Env:ConnectionStrings__Falah -ErrorAction SilentlyContinue }

Write-Host '  installers and guide'
$bom = New-Object Text.UTF8Encoding($true)   # Windows PowerShell 5.1 reads BOM-less scripts as ANSI
foreach ($f in (Get-ChildItem "$repo\deploy" -File) + (Get-Item "$repo\tools\install-falah-print-agent-service.ps1")) {
    $target = Join-Path $out $f.Name
    if ($f.Extension -in '.ps1', '.txt') { [IO.File]::WriteAllText($target, [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8), $bom) }
    else { Copy-Item $f.FullName $target }
}
New-Item -ItemType Directory -Force -Path (Join-Path $out 'Certificate') | Out-Null
$migrations = ([regex]::Matches((Get-Content (Join-Path $out 'Database\migrations-idempotent.sql') -Raw), "INSERT INTO \[__EFMigrationsHistory\]")).Count
@"
Falah release $id
Source commit $commit$(if ($dirty) { ' + uncommitted changes' })
Built $(Get-Date -Format 'yyyy-MM-dd HH:mm') with .NET SDK $((dotnet --version).Trim()), self-contained win-x64
Migrations in script: $migrations
Source fingerprint: $fingerprint
"@ | Set-Content (Join-Path $out 'VERSION.txt') -Encoding UTF8

Write-Host '  manifest and zip'
$manifest = Get-ChildItem $out -Recurse -File | Where-Object { $_.Name -notin 'MANIFEST.sha256.txt', 'build.log' } | Sort-Object FullName |
    ForEach-Object { "$((Get-FileHash $_.FullName).Hash)  $($_.FullName.Substring($out.Length + 1))" }
[IO.File]::WriteAllLines((Join-Path $out 'MANIFEST.sha256.txt'), $manifest)
Move-Item $log (Join-Path ([IO.Path]::GetDirectoryName($out)) "Falah-Install-$id.build.log") -Force
$zip = "$out.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -CompressionLevel Optimal
$size = (Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host ("Done: {0} files, {1:N0} MB; zip {2:N0} MB -> {3}" -f $manifest.Count, ($size / 1MB), ((Get-Item $zip).Length / 1MB), $zip)
