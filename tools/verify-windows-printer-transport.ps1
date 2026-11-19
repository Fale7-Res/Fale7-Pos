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

. "$PSScriptRoot/PrinterVerificationSafety.ps1"
$ErrorActionPreference = 'Stop'
$printer = Get-Printer -Name $PrinterName -ErrorAction Stop
Assert-FalahVerificationQueueEmpty -Jobs @(Get-PrintJob -PrinterName $PrinterName -ErrorAction Stop)
$wasPaused = $printer.PrinterStatus -eq 'Paused'
$pausedByUs = $false
$submittedJobId = 0
$documentName = 'Falah verification ' + [guid]::NewGuid().ToString('N')
$submissionStarted = $false
$cleanupComplete = $true
$wmi = @(Get-CimInstance Win32_Printer | Where-Object { $_.Name -ceq $PrinterName })
if ($wmi.Count -ne 1) { throw 'Could not uniquely identify the printer.' }
try {
    if (-not $wasPaused) {
        $pause = Invoke-CimMethod -InputObject $wmi[0] -MethodName Pause
        if ($pause.ReturnValue -ne 0) { throw 'Could not pause the queue.' }
        $pausedByUs = $true
    }
    # Recheck after pausing: tickets may have arrived between the first read and Pause.
    Assert-FalahVerificationQueueEmpty -Jobs @(Get-PrintJob -PrinterName $PrinterName -ErrorAction Stop)
    $cfgDir = Join-Path $env:TEMP ('falah-printer-check-' + [guid]::NewGuid().ToString('N'))
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
        Pin = 'not-used-by-test-print'
        RenderAsImage = $true
        ImageWidthDots = 576
        ImageMarginDots = 24
        DataDirectory = $cfgDir
    } | ConvertTo-Json | Set-Content -Path $cfg -Encoding utf8
    $exe = Join-Path $AgentDirectory 'Falah.PrintAgent.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Build the agent before verifying the transport.' }
    $submissionStarted = $true
    $cleanupComplete = $false
    $out = @(& $exe $cfg --test-print "--test-document=$documentName")
    $agentExit = $LASTEXITCODE
    $out | ForEach-Object { Write-Output "agent: $_" }
    $identity = @($out | Where-Object { $_ -match '^FALAH_TEST_JOB_ID=[1-9][0-9]*$' })
    if ($identity.Count -ne 1) { throw 'The agent did not return one submitted job id. No jobs will be removed.' }
    $submittedJobId = [int]($identity[0] -replace '^FALAH_TEST_JOB_ID=', '')
    $jobs = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction Stop)
    $mine = Select-FalahVerificationJob -Jobs $jobs -SubmittedJobId $submittedJobId -DocumentName $documentName
    if ($mine.Size -le 0 -or $agentExit -ne 0) { throw 'The agent did not successfully submit a non-empty RAW job.' }
    Write-Output "RESULT: spooler accepted test job $submittedJobId ($($mine.Size) bytes)"
}
finally {
    if ($submittedJobId -gt 0) {
        # Both the OS-assigned id and this invocation's random document name must still match.
        $mine = Select-FalahVerificationJob -Jobs @(Get-PrintJob -PrinterName $PrinterName -ErrorAction Stop) -SubmittedJobId $submittedJobId -DocumentName $documentName
        Remove-PrintJob -PrinterName $PrinterName -ID $mine.Id -ErrorAction Stop
        $cleanupComplete = $true
        Write-Output "Only test job $submittedJobId removed."
    }
    if ($pausedByUs -and $cleanupComplete) {
        $resume = Invoke-CimMethod -InputObject $wmi[0] -MethodName Resume
        if ($resume.ReturnValue -ne 0) { throw 'Could not restore the print queue state.' }
    }
    elseif ($pausedByUs -and $submissionStarted) {
        Write-Warning "Queue left paused: inspect the test job named '$documentName' before resuming. No unrelated jobs were removed."
    }
}
