<#
Controlled reproduction harness for the intermittent native heap failure
(0xc0000374 in ntdll.dll) seen in both WinUI station executables.

Two things must be separated that a raw crash event cannot distinguish:

  1. WHEN: startup, graceful shutdown, or only under forced termination.
  2. WHAT: a real fault versus a clean early exit that merely produced no window.
     The process exit code settles this - 0xc0000374 is reported as the exit code
     of a heap-corruption death, while 0 is an ordinary exit.

Earlier observations were confounded because the build loop force-killed these
apps constantly, so any crash event could have belonged to the kill.

The Application log is sampled per phase, per iteration, so a fault is attributed
to the exact phase that produced it. No sign-in, no API writes.
#>
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [int]$Iterations = 8,
    [int]$SettleSeconds = 9,
    [ValidateSet('Graceful', 'Kill', 'Both')][string]$Shutdown = 'Both'
)

function Get-CrashCount {
    param([datetime]$Since, [string]$ImageName)
    $events = Get-WinEvent -FilterHashtable @{
        LogName = 'Application'; ProviderName = 'Application Error'; StartTime = $Since
    } -ErrorAction SilentlyContinue
    if ($null -eq $events) { return 0 }
    @($events | Where-Object { $_.Message -like "*$ImageName*" }).Count
}

$image = [System.IO.Path]::GetFileName($Exe)
if (-not (Test-Path $Exe)) { Write-Output "MISSING: $Exe"; exit 1 }

$modes = if ($Shutdown -eq 'Both') { @('Graceful', 'Kill') } else { @($Shutdown) }
$results = @()

foreach ($mode in $modes) {
    for ($i = 1; $i -le $Iterations; $i++) {
        $mark = Get-Date
        Start-Sleep -Milliseconds 400   # event-log timestamps are coarse

        $exitCode = $null
        $phase = 'ok'
        $reachedWindow = $false

        $p = Start-Process -FilePath $Exe -PassThru
        Start-Sleep -Seconds $SettleSeconds
        $p.Refresh()

        if ($p.HasExited) {
            $exitCode = $p.ExitCode
            $crashed = (Get-CrashCount -Since $mark -ImageName $image) -gt 0
            $phase = if ($crashed -or $exitCode -ne 0) { 'startup-crash' } else { 'startup-cleanexit' }
        }
        else {
            $reachedWindow = ($p.MainWindowHandle -ne 0)
            if ((Get-CrashCount -Since $mark -ImageName $image) -gt 0) { $phase = 'run-crash' }

            $beforeShutdown = Get-Date
            Start-Sleep -Milliseconds 400
            if ($mode -eq 'Graceful') {
                $p.CloseMainWindow() | Out-Null
                if (-not $p.WaitForExit(12000)) { $p.Kill() | Out-Null; $p.WaitForExit(5000) | Out-Null }
            }
            else {
                $p.Kill() | Out-Null; $p.WaitForExit(5000) | Out-Null
            }
            Start-Sleep -Seconds 2
            try { $exitCode = $p.ExitCode } catch { $exitCode = $null }
            if ($phase -eq 'ok' -and (Get-CrashCount -Since $beforeShutdown -ImageName $image) -gt 0) {
                $phase = 'shutdown-crash'
            }
        }

        $hex = if ($null -ne $exitCode) { '0x{0:X8}' -f $exitCode } else { 'n/a' }
        $results += [pscustomobject]@{ Mode = $mode; Run = $i; Phase = $phase; Exit = $hex; Window = $reachedWindow }
        Write-Output ("[{0} {1}/{2}] phase={3,-18} exit={4,-10} window={5}" -f $mode, $i, $Iterations, $phase, $hex, $reachedWindow)
        Start-Sleep -Seconds 1
    }
}

Write-Output ""
Write-Output "=== SUMMARY ($image) ==="
$results | Group-Object Mode, Phase | Sort-Object Name | ForEach-Object {
    Write-Output ("{0,-30} {1}" -f $_.Name, $_.Count)
}
Write-Output ""
Write-Output "exit codes observed:"
$results | Group-Object Exit | Sort-Object Count -Descending | ForEach-Object {
    Write-Output ("  {0,-12} x{1}" -f $_.Name, $_.Count)
}
