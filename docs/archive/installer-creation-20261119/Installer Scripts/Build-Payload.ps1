[CmdletBinding()]
param([switch]$NoRestore)

$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repo = [IO.Path]::GetFullPath((Join-Path $workspace '..'))
$props = Join-Path $workspace 'Configuration\Installer.Build.props'
$packageCache = Join-Path $env:USERPROFILE '.nuget\packages'
$applicationBuilds = Join-Path $workspace 'Application Builds'
$intermediateRoot = Join-Path $applicationBuilds '.obj'
$buildOutputRoot = Join-Path $applicationBuilds '.build-output'
$logs = Join-Path $workspace 'Logs'
$modules = @(
    @{ Name='Cashier'; Project='src\Falah.Client.CashierWinUI\Falah.Client.CashierWinUI.csproj'; Platform='x64' },
    @{ Name='CallCenter'; Project='src\Falah.Client.CallCenterWinUI\Falah.Client.CallCenterWinUI.csproj'; Platform='x64' },
    @{ Name='Server'; Project='src\Falah.Server\Falah.Server.csproj'; Platform=$null },
    @{ Name='PrintAgent'; Project='src\Falah.PrintAgent\Falah.PrintAgent.csproj'; Platform=$null }
)

function Remove-GeneratedBuildDirectory([string]$Path, [string]$AllowedRoot) {
    $rootFull = [IO.Path]::GetFullPath($AllowedRoot).TrimEnd('\') + '\'
    $pathFull = [IO.Path]::GetFullPath($Path)
    if (-not $pathFull.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a build directory outside '$rootFull': '$pathFull'."
    }
    if (Test-Path -LiteralPath $pathFull) {
        Write-Host "Removing previous generated output: $pathFull"
        Remove-Item -LiteralPath $pathFull -Recurse -Force
    }
}

$builtModules = @()
foreach ($module in $modules) {
    $project = Join-Path $repo $module.Project
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Project source file not found: $project" }
    $projectName = [IO.Path]::GetFileNameWithoutExtension($project)
    $publish = Join-Path $applicationBuilds $module.Name
    $moduleOutput = Join-Path $buildOutputRoot $projectName
    $moduleIntermediate = Join-Path $intermediateRoot $projectName

    # Publish folders are always cleared so removed/renamed files from older builds
    # cannot be swept into the installer by the Inno Setup wildcard entries.
    Remove-GeneratedBuildDirectory $publish $applicationBuilds
    Remove-GeneratedBuildDirectory $moduleOutput $buildOutputRoot
    # A default build clears compiler intermediates too. -NoRestore retains obj/assets
    # so an intentionally offline no-restore build can still use its existing restore graph.
    if (-not $NoRestore) { Remove-GeneratedBuildDirectory $moduleIntermediate $intermediateRoot }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null
    $arguments = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', $publish,
        ('-p:DirectoryBuildPropsPath=' + $props),
        ('-p:RestorePackagesPath=' + $packageCache),
        '-p:PublishSingleFile=false', '-p:PublishReadyToRun=false', '-p:DebugSymbols=false', '-p:DebugType=None')
    if ($module.Platform) { $arguments += ('-p:Platform=' + $module.Platform) }
    if ($NoRestore) { $arguments += '--no-restore' }
    Write-Host "Publishing $($module.Name) to $publish"
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $($module.Name) (exit $LASTEXITCODE)." }
    $exe = Get-ChildItem -LiteralPath $publish -Filter '*.exe' -File | Where-Object { $_.Name -notin @('createdump.exe','RestartAgent.exe') } | Select-Object -First 1
    if (-not $exe) { throw "Expected application EXE was not produced for $($module.Name)." }
    $builtModules += [pscustomobject]@{
        Name = $module.Name
        Project = $module.Project
        PublishDirectory = $publish
        Executable = $exe.Name
        ExecutableSha256 = (Get-FileHash -LiteralPath $exe.FullName -Algorithm SHA256).Hash
    }
}
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$gitCommit = $null
$gitChanges = @()
if (Get-Command git -ErrorAction SilentlyContinue) {
    $gitCommit = (& git -C $repo rev-parse HEAD 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0) { $gitCommit = $null }
    $gitChanges = @(& git -C $repo status --short 2>$null)
    if ($LASTEXITCODE -ne 0) { $gitChanges = @() }
}
$manifest = [ordered]@{
    BuiltAt = (Get-Date).ToUniversalTime().ToString('o')
    Repository = $repo
    GitCommit = $gitCommit
    WorkingTreeChanges = $gitChanges
    Configuration = 'Release / win-x64 / self-contained'
    CleanIntermediates = (-not $NoRestore)
    Modules = $builtModules
}
$manifestPath = Join-Path $logs 'payload-build-manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host 'All application modules published. SQL Express and installer compiler are prepared separately.'
Write-Host "Build manifest: $manifestPath"
