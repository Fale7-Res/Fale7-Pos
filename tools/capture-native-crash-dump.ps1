<#
Captures a full-memory dump of the intermittent heap-corruption fault (0xC0000374).

The app is launched directly - not via ProcDump's -x - so the harness holds a real Process handle and
can read the exit code, which is the only reliable way to tell a genuine fault from an ordinary exit.
ProcDump attaches to that PID and dumps on termination; runs that exited cleanly have their dump
deleted immediately, since each one costs ~250 MB.
#>
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [Parameter(Mandatory = $true)][string]$DumpDir,
    [int]$MaxAttempts = 20,
    [int]$SettleSeconds = 8
)
$procdump = Join-Path $PSScriptRoot '..\artifacts\verification\native-crash-tools\ProcDump\procdump64.exe'
if (-not (Test-Path $procdump)) { Write-Output "ProcDump missing: $procdump"; exit 1 }
New-Item -ItemType Directory -Force -Path $DumpDir | Out-Null

for ($i = 1; $i -le $MaxAttempts; $i++) {
    $p = Start-Process -FilePath $Exe -PassThru
    Start-Sleep -Milliseconds 250
    $pd = Start-Process -FilePath $procdump `
        -ArgumentList '-accepteula', '-ma', '-t', $p.Id, "`"$DumpDir`"" `
        -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds $SettleSeconds

    $p.Refresh()
    if (-not $p.HasExited) {
        $p.CloseMainWindow() | Out-Null
        if (-not $p.WaitForExit(12000)) { $p.Kill() | Out-Null; $p.WaitForExit(5000) | Out-Null }
    }
    if (-not $pd.WaitForExit(20000)) { $pd.Kill() | Out-Null }
    Start-Sleep -Seconds 1

    $exit = $p.ExitCode
    $hex = '0x{0:X8}' -f $exit
    $dumps = @(Get-ChildItem $DumpDir -Filter *.dmp -ErrorAction SilentlyContinue | Sort-Object LastWriteTime)
    Write-Output ("[{0}/{1}] exit={2} dumps={3}" -f $i, $MaxAttempts, $hex, $dumps.Count)

    if ($exit -ne 0) {
        if ($dumps.Count -gt 0) { Write-Output ("CAPTURED: {0}" -f $dumps[-1].FullName) }
        else { Write-Output "FAULTED but no dump was written." }
        break
    }
    foreach ($d in $dumps) { Remove-Item $d.FullName -Force -ErrorAction SilentlyContinue }
}
