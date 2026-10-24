param([Parameter(Mandatory=$true)][string]$InputPath)
$ErrorActionPreference = 'Stop'
$nugetVersionAssembly = 'C:/Program Files/dotnet/sdk/8.0.303/NuGet.Versioning.dll'
Add-Type -Path $nugetVersionAssembly
$cases = @(
 @{v='1.0.0';r='[1.0.0,2.0.0)';expected=$true},
 @{v='2.0.0';r='[1.0.0,2.0.0)';expected=$false},
 @{v='1.0.0';r='(1.0.0,2.0.0]';expected=$false},
 @{v='2.0.0';r='(1.0.0,2.0.0]';expected=$true},
 @{v='1.0.0-preview.2';r='[1.0.0-preview.1,1.0.0)';expected=$true},
 @{v='1.0.0-preview.10';r='[1.0.0-preview.2,1.0.0)';expected=$true},
 @{v='1.0.0';r='(,1.0.0)';expected=$false},
 @{v='1.0.0.1';r='(1.0.0,1.0.0.2)';expected=$true},
 @{v='1.0.0+build.2';r='[1.0.0]';expected=$true},
 @{v='1.0.0';r='[1.0,1.0.0]';expected=$true},
 @{v='1.0.0-RC.1';r='[1.0.0-rc.1]';expected=$true},
 @{v='8.0.1';r='[8.0.0-preview.1.23110.8,8.0.0]';expected=$false}
)
$testResults = foreach($case in $cases){
 $actual = [NuGet.Versioning.VersionRange]::Parse($case.r).Satisfies([NuGet.Versioning.NuGetVersion]::Parse($case.v))
 if($actual -ne $case.expected){throw "NuGet version semantics verification failed"}
 [pscustomobject]@{version=$case.v;range=$case.r;expected=$case.expected;actual=$actual}
}
$inputDoc = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json
$evaluations = foreach($pair in $inputDoc.advisory_range_pairs){
 try{
  $version = [NuGet.Versioning.NuGetVersion]::Parse($pair.version)
  $range = [NuGet.Versioning.VersionRange]::Parse($pair.advisory.versions)
  [pscustomobject]@{bom_ref=$pair.bom_ref;package=$pair.package;version=$pair.version;advisory_url=$pair.advisory.url;range=$pair.advisory.versions;severity=$pair.advisory.severity;matches=$range.Satisfies($version);status='EVALUATED_WITH_NUGET_VERSIONING'}
 }catch{
  [pscustomobject]@{bom_ref=$pair.bom_ref;package=$pair.package;version=$pair.version;advisory_url=$pair.advisory.url;range=$pair.advisory.versions;status='RANGE_EVALUATION_GAP';error=$_.Exception.Message}
 }
}
[pscustomobject]@{assembly_path=$nugetVersionAssembly;assembly_sha256=(Get-FileHash -LiteralPath $nugetVersionAssembly -Algorithm SHA256).Hash;assembly_full_name=[NuGet.Versioning.NuGetVersion].Assembly.FullName;tests=$testResults;evaluations=@($evaluations)} | ConvertTo-Json -Depth 12 -Compress
