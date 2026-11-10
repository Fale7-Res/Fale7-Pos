function Assert-FalahVerificationQueueEmpty {
    param([object[]]$Jobs)
    if (@($Jobs).Count -ne 0) { throw 'The print queue is not empty. No jobs were submitted or removed.' }
}

function Select-FalahVerificationJob {
    param([object[]]$Jobs, [int]$SubmittedJobId, [string]$DocumentName)
    if ($SubmittedJobId -le 0 -or $DocumentName -notlike 'Falah verification *') {
        throw 'No trustworthy submission identity was returned; no jobs will be removed.'
    }
    $mine = @($Jobs | Where-Object { $_.Id -eq $SubmittedJobId -and $_.DocumentName -ceq $DocumentName })
    if ($mine.Count -ne 1) { throw 'The submitted test job could not be identified uniquely; no jobs will be removed.' }
    return $mine[0]
}
