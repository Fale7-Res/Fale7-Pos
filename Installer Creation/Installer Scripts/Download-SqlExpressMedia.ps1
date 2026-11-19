$ErrorActionPreference = 'Stop'
$mediaDir = Join-Path $PSScriptRoot '..\Dependencies\Offline Installers'
$mediaDir = [IO.Path]::GetFullPath($mediaDir)
$finalPath = Join-Path $mediaDir 'SQLEXPR_x64_ENU.exe'
$url = 'https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe'
$total = 279293816L
$expectedHash = '2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05'
$chunkSize = 32MB
$partDir = Join-Path $mediaDir 'sql-express-download-parts'
if (Test-Path $finalPath) {
    $existing = Get-Item $finalPath
    if ($existing.Length -eq $total -and (Get-FileHash $finalPath -Algorithm SHA256).Hash -eq $expectedHash -and (Get-AuthenticodeSignature $finalPath).Status -eq 'Valid') {
        Write-Output "Already staged and verified: $finalPath"
        exit 0
    }
}
New-Item -ItemType Directory -Force -Path $partDir | Out-Null
$jobs = @()
$parts = @()
for ($start = 0L; $start -lt $total; $start += $chunkSize) {
    $end = [Math]::Min($total - 1, $start + $chunkSize - 1)
    $index = $parts.Count
    $part = Join-Path $partDir ('part-{0:D2}.bin' -f $index)
    $status = Join-Path $partDir ('part-{0:D2}.status' -f $index)
    $errorLog = Join-Path $partDir ('part-{0:D2}.error' -f $index)
    $expected = $end - $start + 1
    $parts += [pscustomobject]@{Start=$start;End=$end;Part=$part;Status=$status;Error=$errorLog}
    if ((Test-Path $part) -and (Get-Item $part).Length -eq $expected -and (Test-Path $status) -and (Get-Content $status -Raw).Trim() -eq '206') { continue }
    Remove-Item $part,$status,$errorLog -Force -ErrorAction SilentlyContinue
    $args = '--fail --location --retry 4 --retry-delay 2 --range {0}-{1} --output "{2}" --write-out "%{{http_code}}" "{3}"' -f $start,$end,$part,$url
    $proc = Start-Process -FilePath 'curl.exe' -ArgumentList $args -PassThru -WindowStyle Hidden -RedirectStandardOutput $status -RedirectStandardError $errorLog
    $jobs += [pscustomobject]@{Process=$proc;Start=$start;End=$end;Part=$part;Status=$status;Error=$errorLog}
}
while (@($jobs | Where-Object {-not $_.Process.HasExited}).Count -gt 0) {
    $completed = @($jobs | Where-Object {$_.Process.HasExited}).Count
    $bytes = 0L
    foreach ($job in $jobs) { if (Test-Path $job.Part) { $bytes += (Get-Item $job.Part).Length } }
    Write-Output ("SQL Express media download: {0}/{1} parts, {2:N1} MiB" -f $completed,$jobs.Count,($bytes/1MB))
    Start-Sleep -Seconds 10
}
foreach ($job in $jobs) {
    if ($job.Process.ExitCode -ne 0) { throw "curl chunk failed ($($job.Start)-$($job.End)): $((Get-Content $job.Error -Raw -ErrorAction SilentlyContinue))" }
    $expected = $job.End - $job.Start + 1
    if (-not (Test-Path $job.Part) -or (Get-Item $job.Part).Length -ne $expected) { throw "Wrong chunk length at offset $($job.Start)." }
    if ((Get-Content $job.Status -Raw).Trim() -ne '206') { throw "Unexpected HTTP range status for offset $($job.Start): $((Get-Content $job.Status -Raw))" }
}
foreach ($partInfo in $parts) {
    $expected = $partInfo.End - $partInfo.Start + 1
    if (-not (Test-Path $partInfo.Part) -or (Get-Item $partInfo.Part).Length -ne $expected) { throw "Wrong or missing chunk length at offset $($partInfo.Start)." }
    if ((Get-Content $partInfo.Status -Raw).Trim() -ne '206') { throw "Unexpected HTTP range status for offset $($partInfo.Start)." }
}
$stream = [IO.File]::Open($finalPath,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
try { foreach ($partInfo in $parts) { $input=[IO.File]::OpenRead($partInfo.Part); try {$input.CopyTo($stream)} finally {$input.Dispose()} } } finally {$stream.Dispose()}
if ((Get-Item $finalPath).Length -ne $total) { throw 'Assembled SQL Express media size does not match Microsoft Content-Length.' }
$sig = Get-AuthenticodeSignature $finalPath
if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw "Microsoft signature verification failed: $($sig.Status) $($sig.SignerCertificate.Subject)" }
$hash = (Get-FileHash $finalPath -Algorithm SHA256).Hash
if ($hash -ne $expectedHash) { throw "SQL Express media SHA-256 does not match: $hash" }
Remove-Item -LiteralPath $partDir -Recurse -Force
Get-Item $finalPath | Select-Object FullName,Length
$sig | Select-Object Status,@{n='Signer';e={$_.SignerCertificate.Subject}}
Get-FileHash $finalPath -Algorithm SHA256 | Select-Object Algorithm,Hash
