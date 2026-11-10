<#
Pre-demo PASS/FAIL check for Falah on this PC. READ-ONLY: changes no data, no SQL configuration and
no Windows setting, and prints nothing.

Why it exists: SQL Server Express on this PC is sometimes squeezed by Windows memory pressure. When
that happens, the memory it can grant to queries shrinks to a few MB, and any query that sorts wide
rows waits out SQL Server's ~25 s memory-grant timeout (wait type RESOURCE_SEMAPHORE). The menu no
longer depends on it, but the audit log and some reports still can. A client timeout increase would
only hide this; this script tells you whether it is happening right now.

Run it ~10 minutes before the demo, with the Falah server already running:
    powershell -ExecutionPolicy Bypass -File tools\pre-demo-check.ps1 -Pin <manager PIN>

Exit code 0 = all PASS. Anything FAIL is listed with what to do.
#>
param(
    [Parameter(Mandatory = $true)][string]$Pin,
    [string]$BaseUrl = 'http://localhost:5151',
    [string]$SqlInstance = '.\SQLEXPRESS',
    [string]$ExpectedMenuUrl = 'https://fale7-res.vercel.app/',
    [string]$AgentConfig = ''
)
# Resolved here rather than as the parameter default: Windows PowerShell 5.1 leaves $PSScriptRoot
# empty inside param() defaults.
if (-not $AgentConfig) { $AgentConfig = Join-Path $PSScriptRoot '..\src\Falah.PrintAgent\bin\Release\net8.0\appsettings.json' }

$results = New-Object System.Collections.Generic.List[object]
function Add-Result([string]$Check, [bool]$Pass, [string]$Detail, [string]$Fix = '') {
    $results.Add([pscustomobject]@{ Result = $(if ($Pass) { 'PASS' } else { 'FAIL' }); Check = $Check; Detail = $Detail; Fix = $Fix })
}
function Invoke-Sql([string]$Query) {
    $out = & sqlcmd -S $SqlInstance -E -h -1 -W -s '|' -Q "SET NOCOUNT ON; $Query" 2>&1
    return ($out | Where-Object { $_ -and $_.Trim() -ne '' }) -join "`n"
}

# ---- 1. SQL Server service ----
$svc = Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
Add-Result 'SQL Server service' ($svc -and $svc.Status -eq 'Running') "$($svc.Status)" 'Start the MSSQL$SQLEXPRESS service'

# ---- 2. Free memory on the PC ----
$os = Get-CimInstance Win32_OperatingSystem
$freeMb = [math]::Round($os.FreePhysicalMemory / 1024)
Add-Result 'Free physical memory >= 1500 MB' ($freeMb -ge 1500) "$freeMb MB free" 'Close browsers / Visual Studio / other heavy apps, then re-run'

# ---- 3. SQL memory grants (the actual cause of the 25 s stalls) ----
$sem = Invoke-Sql "SELECT available_memory_kb/1024, waiter_count, timeout_error_count FROM sys.dm_exec_query_resource_semaphores WHERE resource_semaphore_id = 0 AND pool_id = (SELECT MAX(pool_id) FROM sys.dm_exec_query_resource_semaphores WHERE resource_semaphore_id = 0)"
$parts = ($sem -split "`n")[0] -split '\|'
$grantMb = [int]$parts[0]; $waiters = [int]$parts[1]
Add-Result 'SQL query-memory pool >= 64 MB' ($grantMb -ge 64) "$grantMb MB available for query memory grants" 'Close heavy apps; if still low, restart the MSSQL$SQLEXPRESS service and re-run'
Add-Result 'No queries waiting for memory (RESOURCE_SEMAPHORE)' ($waiters -eq 0) "$waiters waiting" 'Same as above'

$cfg = Invoke-Sql "SELECT CAST(value_in_use AS int) FROM sys.configurations WHERE name = 'min server memory (MB)'"
$minMem = [int](($cfg -split "`n")[0])
# Reported, not failed: changing it is a go-live decision (GO_LIVE_CLEANUP_CHECKLIST.md 12.1).
$results.Add([pscustomobject]@{ Result = 'INFO'; Check = 'SQL min server memory'; Detail = "$minMem MB (recommended for the restaurant PC: 1024)"; Fix = '' })

# ---- 4. Falah server ----
try {
    $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 10
    Add-Result 'Falah server /health' ($health.StatusCode -eq 200) "HTTP $($health.StatusCode)" 'Start src\Falah.Server\bin\Release\net8.0\Falah.Server.exe'
} catch {
    Add-Result 'Falah server /health' $false $_.Exception.Message 'Start src\Falah.Server\bin\Release\net8.0\Falah.Server.exe'
    $results | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Output
    exit 1
}

$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login-by-pin" -ContentType 'application/json; charset=utf-8' `
    -Body ([System.Text.Encoding]::UTF8.GetBytes((@{ pin = $Pin } | ConvertTo-Json)))
$headers = @{ Authorization = "Bearer $($login.token)" }
$actor = [uri]::EscapeDataString($login.id)
$today = (Get-Date).ToString('yyyy-MM-dd')

# ---- 5. Timed demo endpoints: one warm-up, then the timed call ----
$endpoints = @(
    @{ Name = '/api/menu';                   Url = '/api/menu';                                                     Limit = 2 },
    @{ Name = '/api/audit/events';           Url = "/api/audit/events?actorUserId=$actor&limit=200";               Limit = 3 },
    @{ Name = '/api/reports/owner-dashboard'; Url = "/api/reports/owner-dashboard?actorUserId=$actor";              Limit = 3 },
    @{ Name = '/api/reports/daily-statement'; Url = "/api/reports/daily-statement?actorUserId=$actor&businessDate=$today"; Limit = 3 },
    @{ Name = '/api/reports/sales';          Url = "/api/reports/sales?actorUserId=$actor";                        Limit = 3 },
    @{ Name = '/api/orders';                 Url = "/api/orders?actorUserId=$actor";                               Limit = 3 },
    @{ Name = '/api/treasury/balances';      Url = "/api/treasury/balances?actorUserId=$actor";                    Limit = 3 }
)
foreach ($e in $endpoints) {
    try {
        Invoke-WebRequest -Uri "$BaseUrl$($e.Url)" -Headers $headers -UseBasicParsing -TimeoutSec 60 | Out-Null
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $r = Invoke-WebRequest -Uri "$BaseUrl$($e.Url)" -Headers $headers -UseBasicParsing -TimeoutSec 60
        $sw.Stop()
        $s = [math]::Round($sw.Elapsed.TotalSeconds, 2)
        Add-Result "$($e.Name) < $($e.Limit) s" (($r.StatusCode -eq 200) -and ($s -lt $e.Limit)) "HTTP $($r.StatusCode) in $s s" 'Check the SQL memory rows above'
    } catch {
        Add-Result "$($e.Name) < $($e.Limit) s" $false $_.Exception.Message 'Check the server log'
    }
}

# ---- 6. Printing safety ----
$overview = Invoke-RestMethod -Uri "$BaseUrl/api/print-jobs/overview" -Headers $headers
$pending = ($overview.routes | Measure-Object -Property pendingCount -Sum).Sum
$failed = ($overview.routes | Measure-Object -Property failedCount -Sum).Sum
Add-Result 'No pending print jobs' ($pending -eq 0) "$pending pending" 'Drain via the file transport before starting a real agent (see docs/PRINT_AGENT.md)'
Add-Result 'No failed print jobs' ($failed -eq 0) "$failed failed" 'Review on the Printing screen'

$settings = Invoke-RestMethod -Uri "$BaseUrl/api/settings" -Headers $headers
Add-Result 'Menu link unchanged' ($settings.menuUrl -eq $ExpectedMenuUrl) "$($settings.menuUrl)" "Expected $ExpectedMenuUrl"

if (Test-Path $AgentConfig) {
    $agent = Get-Content $AgentConfig -Raw -Encoding UTF8 | ConvertFrom-Json
    $ok = ($agent.RenderAsImage -eq $true) -and ($agent.ImageWidthDots -eq 576) -and ($agent.ImageMarginDots -eq 24)
    Add-Result 'Deployed print agent: image mode 576/24' $ok "RenderAsImage=$($agent.RenderAsImage) width=$($agent.ImageWidthDots) margin=$($agent.ImageMarginDots)" 'Rebuild the agent (the source config carries these values)'
} else {
    Add-Result 'Deployed print agent config present' $false "not found: $AgentConfig" 'Build src\Falah.PrintAgent -c Release'
}
$agents = @(Get-Process -Name 'Falah.PrintAgent' -ErrorAction SilentlyContinue)
$results.Add([pscustomobject]@{ Result = 'INFO'; Check = 'Print agents running'; Detail = "$($agents.Count) (start one Receipt agent only if printing is shown)"; Fix = '' })

$results | Format-Table -AutoSize -Wrap | Out-String -Width 220 | Write-Output
$failCount = @($results | Where-Object { $_.Result -eq 'FAIL' }).Count
if ($failCount -eq 0) { Write-Output 'OVERALL: PASS'; exit 0 } else { Write-Output "OVERALL: FAIL ($failCount)"; exit 1 }
