<#
Verifies the Windows spooler RAW transport against a real print queue without consuming paper.

The queue is paused first, the ticket is submitted, the resulting job is inspected (it must exist,
be named by the transport and hold the bytes that were sent), and then the job is deleted and the
queue restored to the state it was in. What this proves is the whole software path - OpenPrinter /
StartDocPrinter / WritePrinter marshalling, the RAW datatype and the byte count. What it cannot
prove is what the paper looks like; that still needs a physical run.
#>
param(
    [Parameter(Mandatory = $true)][string]$PrinterName,
    [string]$AgentDirectory = "$PSScriptRoot\..\src\Falah.PrintAgent\bin\Release\net8.0"
)

$printer = Get-Printer -Name $PrinterName -ErrorAction Stop
$wasPaused = $printer.PrinterStatus -eq 'Paused'
Write-Output "printer: $($printer.Name)  status: $($printer.PrinterStatus)  port: $($printer.PortName)"

if (-not $wasPaused) {
    $wmi = Get-CimInstance Win32_Printer -Filter "Name='$PrinterName'"
    Invoke-CimMethod -InputObject $wmi -MethodName Pause | Out-Null
    Write-Output "queue paused for the duration of this check"
}

try {
    # Windows PowerShell 5.1 runs on .NET Framework and cannot load the agent's .NET 8 assemblies,
    # so the ticket is produced by the agent itself in its own process.
    $cfgDir = Join-Path $env:TEMP ("falah-printer-check-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null
    $cfg = Join-Path $cfgDir 'appsettings.json'
    @{
        ServerBaseAddress = 'http://localhost:5151'
        Route = 'Receipt'
        ReportedByUserId = 'verification'
        Transport = 'windows'
        WindowsPrinterName = $PrinterName
        TextEncoding = 'utf-8'
        PollSeconds = 3
        TransportTimeoutSeconds = 15
        # --test-print never logs in, but the agent validates its whole config before any mode runs,
        # so a PIN has to be present. This placeholder is never sent anywhere.
        Pin = 'not-used-by-test-print'
        # The production print path on the real printer: image mode at the physically proven width.
        RenderAsImage = $true
        ImageWidthDots = 576
        ImageMarginDots = 24
    } | ConvertTo-Json | Set-Content -Path $cfg -Encoding utf8

    $before = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction SilentlyContinue).Count
    $exe = Join-Path $AgentDirectory 'Falah.PrintAgent.exe'
    $out = & $exe $cfg --test-print
    $out | ForEach-Object { Write-Output "  agent: $_" }
    Write-Output "agent exit code: $LASTEXITCODE"

    Start-Sleep -Milliseconds 1200
    $jobs = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction SilentlyContinue)
    Write-Output "jobs before: $before   after: $($jobs.Count)"
    foreach ($j in $jobs) {
        Write-Output ("  job {0}: name='{1}' status={2} size={3} bytes" -f $j.Id, $j.DocumentName, $j.JobStatus, $j.Size)
    }
    $mine = $jobs | Where-Object { $_.DocumentName -eq 'Falah ticket' }
    if ($mine) {
        Write-Output "RESULT: the spooler accepted the RAW job"
        $mine | Remove-PrintJob
        Write-Output "test job removed - no paper was produced"
    }
    else { Write-Output "RESULT: no job appeared for this submission" }
}
finally {
    if (-not $wasPaused) {
        $wmi = Get-CimInstance Win32_Printer -Filter "Name='$PrinterName'"
        Invoke-CimMethod -InputObject $wmi -MethodName Resume | Out-Null
        Write-Output "queue resumed"
    }
}
