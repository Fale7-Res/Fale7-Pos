$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/PrinterVerificationSafety.ps1"
$real = [pscustomobject]@{ Id = 1; DocumentName = 'Falah ticket' }
$test = [pscustomobject]@{ Id = 2; DocumentName = 'Falah verification unique' }
$other = [pscustomobject]@{ Id = 3; DocumentName = 'Falah verification other' }
Assert-FalahVerificationQueueEmpty -Jobs @()
$refused = $false
try { Assert-FalahVerificationQueueEmpty -Jobs @($real) } catch { $refused = $true }
if (-not $refused) { throw 'A live queue was allowed.' }
$selected = Select-FalahVerificationJob -Jobs @($real, $test, $other) -SubmittedJobId 2 -DocumentName $test.DocumentName
if ($selected.Id -ne 2) { throw "Selected another job." }
foreach ($id in @(0, 1, 3)) {
    $refused = $false
    try { Select-FalahVerificationJob -Jobs @($real, $test, $other) -SubmittedJobId $id -DocumentName $test.DocumentName } catch { $refused = $true }
    if (-not $refused) { throw "Unsafe cleanup accepted for id $id" }
}
Write-Output 'PASS: empty-queue gate and exact submitted-id/document ownership (5 cases). No printer accessed.'
