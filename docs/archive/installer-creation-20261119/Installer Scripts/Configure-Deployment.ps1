[CmdletBinding()]
param([Parameter(Mandatory)][string]$SettingsFile,[Parameter(Mandatory)][string]$InstallRoot)
$ErrorActionPreference = 'Stop'
$logDir = Join-Path $env:ProgramData 'Falah\Logs\Installer'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir ('install-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
Start-Transcript -Path $log -Append | Out-Null
$createdService = $false
$deploymentPhase = 'Read installation settings'
try {
    $settings = @{}
    foreach ($line in Get-Content -LiteralPath $SettingsFile) { if ($line -match '^([^=]+)=(.*)$') { $settings[$matches[1]] = $matches[2] } }
    $mode = $settings.Mode
    if ($mode -notin @('Full','WithoutSql')) { throw 'Installation mode is missing or invalid.' }
    $role = $settings.Role
    if ($role -notin @('Server','Workstation')) { throw 'Deployment role is missing or invalid.' }
    $deploymentPhase = 'Normalize SQL connection string'
    $sqlConnection = $settings.SqlConnection
    if ($sqlConnection) {
        # SqlClient accepts "Initial Catalog" and "Database", but not "InitialCatalog".
        # Replace the key token directly to avoid depending on whitespace quantifiers.
        $hasUnspacedCatalogKey = $sqlConnection -match '(?i)InitialCatalog'
        $sqlConnection = [regex]::Replace($sqlConnection, '(?i)InitialCatalog', 'Initial Catalog')
        Write-Host "Unspaced InitialCatalog key was present: $hasUnspacedCatalogKey"
    }
    $initializeNewDatabase = $settings.InitializeNewDatabase -eq '1'
    $allowExistingDatabase = $settings.AllowExistingDatabase -eq '1'
    $statePath = Join-Path $env:ProgramData 'Falah\install-state.json'
    $service = Get-Service FalahServer -ErrorAction SilentlyContinue
    if ($service) {
        if (-not (Test-Path $statePath)) { throw 'A Windows service named FalahServer exists without Falah installation state. It was left untouched.' }
        $priorState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        if ([IO.Path]::GetFullPath($priorState.InstallRoot).TrimEnd('\') -ne [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')) { throw 'FalahServer belongs to another installation directory. It was left untouched.' }
    }
    $clientUrl = $settings.ServerUrl
    if ([string]::IsNullOrWhiteSpace($clientUrl)) { throw 'Server URL for the cashier and call-center clients is required.' }
    if ($clientUrl -match '^http://(?!localhost|127\.0\.0\.1)') { throw 'Remote server URLs must use HTTPS. Configure TLS before connecting clients over the network.' }
    $deploymentPhase = 'Check Windows compatibility'
    $os = Get-CimInstance Win32_OperatingSystem
    if ([int]$os.BuildNumber -lt 17763) { throw 'This application requires Windows 10 version 1809 (build 17763) or newer, x64.' }

    $deploymentPhase = 'Check Visual C++ prerequisite'
    # VC++ is shared by the unpackaged WinUI applications. Reuse a compatible runtime.
    $vcRuntimeKey = 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64'
    $vcInstalled = $false
    if (Test-Path $vcRuntimeKey) {
        $v = Get-ItemProperty $vcRuntimeKey
        $vcInstalled = $v.Installed -eq 1 -and ([version]('{0}.{1}.{2}.{3}' -f $v.Major,$v.Minor,$v.Bld,$v.Rbld)) -ge [version]'14.40.33810.0'
    }
    if (-not $vcInstalled) {
        $vc = Join-Path $InstallRoot 'Dependencies\Redistributables\VC_redist.x64.exe'
        if (-not (Test-Path $vc)) { throw 'Microsoft Visual C++ x64 redistributable is missing from the installation package.' }
        $p = Start-Process -FilePath $vc -ArgumentList '/install /quiet /norestart' -Wait -PassThru
        if ($p.ExitCode -notin @(0,1638,3010)) { throw "Visual C++ installation failed with exit code $($p.ExitCode)." }
        if ($p.ExitCode -eq 3010) { Write-Warning 'Visual C++ requires a reboot to finish.' }
    }

    if ($role -eq 'Workstation') {
        $healthUrl = $clientUrl.TrimEnd('/') + '/health'
        try { $health = Invoke-WebRequest -Uri $healthUrl -TimeoutSec 8 } catch { throw "Cannot reach the selected Falah API at '$healthUrl': $($_.Exception.Message)" }
        if ($health.StatusCode -ne 200) { throw "The selected Falah API returned HTTP $($health.StatusCode)." }
        [Environment]::SetEnvironmentVariable('FALAH_SERVER_URL',$clientUrl,'Machine')
        [Environment]::SetEnvironmentVariable('FALAH_SERVER_URL',$clientUrl,'Process')
        $state = @{ Product='Falah Restaurant'; Version='1.0.6'; InstallRoot=$InstallRoot; InstalledAt=(Get-Date).ToUniversalTime().ToString('o'); Mode=$mode; Role=$role; ServerUrl=$clientUrl }
        $statePath = Join-Path $env:ProgramData 'Falah\install-state.json'
        $state | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
        New-Item -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Force | Out-Null
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Name InstallRoot -Value $InstallRoot
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Name Version -Value '1.0.6'
        Write-Host 'Workstation installation succeeded; the central API health check passed. No SQL Server or API service was installed or changed.'
        return
    }

    if ([string]::IsNullOrWhiteSpace($sqlConnection)) { throw 'SQL Server connection string is required for the server role.' }
    $ownerPin = $settings.OwnerPin
    if ($ownerPin -notmatch '^\d{4,6}$' -or $ownerPin -match '^(\d)\1+$') { throw 'Owner PIN must contain 4–6 digits and must not repeat the same digit.' }

    $deploymentPhase = 'Parse SQL connection string'
    $instanceRegistry = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
    $instances = if (Test-Path $instanceRegistry) { (Get-ItemProperty $instanceRegistry).PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' } } else { @() }
    $compatibleInstances = @()
    foreach ($instance in $instances) {
        $versionKey = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$($instance.Value)\MSSQLServer\CurrentVersion"
        $versionInfo = Get-ItemProperty $versionKey -ErrorAction SilentlyContinue
        if ($versionInfo.CurrentVersion -and [version]$versionInfo.CurrentVersion -ge [version]'13.0') {
            $serviceName = if ($instance.Name -eq 'MSSQLSERVER') { 'MSSQLSERVER' } else { "MSSQL`$$($instance.Name)" }
            $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
            if ($svc) { $compatibleInstances += [pscustomobject]@{ Name=$instance.Name; Service=$svc.Name; Version=$versionInfo.CurrentVersion } }
        }
    }
    $engineServices = @(Get-Service -Name 'MSSQLSERVER','MSSQL$*' -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq 'MSSQLSERVER' -or $_.Name -like 'MSSQL$*' })
    Add-Type -AssemblyName System.Data
    try { $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($sqlConnection) }
    catch { throw "Invalid SQL connection string. Use Database=Falah or Initial Catalog=Falah. Details: $($_.Exception.Message)" }
    $sqlConnection = $builder.ConnectionString
    $databaseName = $builder.InitialCatalog
    if ([string]::IsNullOrWhiteSpace($databaseName)) { throw 'The SQL connection string must specify an Initial Catalog/Database.' }
    $serverName = $builder.DataSource
    $localNames = @('.', '(local)', 'localhost', '127.0.0.1', $env:COMPUTERNAME, $env:COMPUTERNAME + '\FALAHRESTAURANT', $env:COMPUTERNAME + '\SQLEXPRESS')
    $isLocalSql = $localNames -contains $serverName -or $serverName.StartsWith('.\') -or $serverName.StartsWith('(local)\') -or $serverName.StartsWith('localhost\')
    if ($mode -eq 'Full' -and $compatibleInstances.Count -eq 0 -and $isLocalSql) {
        $deploymentPhase = 'Install SQL Server Express'
        if ($engineServices.Count -gt 0) { throw 'An older SQL Server engine is present. It was left untouched. Install a supported engine side-by-side and configure its instance, then retry.' }
        $sqlMedia = Join-Path $InstallRoot 'Dependencies\Offline Installers\SQLEXPR_x64_ENU.exe'
        if (-not (Test-Path $sqlMedia)) { throw 'SQL Server Express offline media is missing from the installation package.' }
        $args = @('/Q','/ACTION=Install','/FEATURES=SQLENGINE','/INSTANCENAME=FALAHRESTAURANT',
            '/SQLSYSADMINACCOUNTS=BUILTIN\Administrators','/ADDCURRENTUSERASSQLADMIN=True',
            '/TCPENABLED=0','/BROWSERSVCSTARTUPTYPE=Disabled','/SUPPRESSPRIVACYSTATEMENTNOTICE',
            '/IACCEPTSQLSERVERLICENSETERMS',"/UpdateEnabled=False")
        $p = Start-Process -FilePath $sqlMedia -ArgumentList $args -Wait -PassThru
        $sqlLogRoot = Join-Path ${env:ProgramFiles} 'Microsoft SQL Server\160\Setup Bootstrap\Log'
        $sqlSummary = Get-ChildItem -LiteralPath $sqlLogRoot -Filter 'Summary.txt' -File -Recurse -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
        if (-not $sqlSummary) { $sqlSummary = $sqlLogRoot }
        if ($p.ExitCode -notin @(0,3010)) { throw "SQL Server Express setup failed with exit code $($p.ExitCode). Review the SQL Server setup summary at $sqlSummary." }
        if (-not (Get-Service -Name 'MSSQL$FALAHRESTAURANT' -ErrorAction SilentlyContinue)) { throw "SQL Server setup reported success but its engine service was not found. Review the SQL Server setup summary at $sqlSummary." }
    }

    $deploymentPhase = 'Validate SQL Server connection'
    # Validate SQL connectivity as the installer account before touching application state.
    $masterBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($sqlConnection)
    # PowerShell 5.1 treats these property assignments as dynamic connection-string
    # keywords on this builder; use SqlClient's accepted indexer keys explicitly.
    $masterBuilder['Initial Catalog'] = 'master'
    $masterBuilder['Connect Timeout'] = 8
    $connection = New-Object System.Data.SqlClient.SqlConnection($masterBuilder.ConnectionString)
    try { $connection.Open() } catch { throw "Cannot connect to SQL Server '$($builder.DataSource)': $($_.Exception.Message)" }
    $deploymentPhase = 'Authorize or initialize application database'
    try {
        $cmd = $connection.CreateCommand(); $cmd.CommandText = 'SELECT COUNT(*) FROM sys.databases WHERE name = @name'
        $null = $cmd.Parameters.Add('@name',[System.Data.SqlDbType]::NVarChar,128); $cmd.Parameters['@name'].Value = $databaseName
        $databaseExists = [int]$cmd.ExecuteScalar() -gt 0
        if ($databaseExists -and $initializeNewDatabase) { throw "Database '$databaseName' already exists. New-database initialization is refused and no migration was run." }
        if ($databaseExists -and -not $allowExistingDatabase) { throw "Database '$databaseName' exists. Explicitly authorize using an existing database before starting the application service." }
        if (-not $databaseExists -and -not $initializeNewDatabase) { throw "Database '$databaseName' does not exist. Select explicit new-database initialization or create it through your DBA." }
        if (-not $databaseExists) { $safeName = $databaseName.Replace(']',']]'); $cmd.CommandText = "CREATE DATABASE [$safeName]"; $null = $cmd.ExecuteNonQuery() }
    } finally { $connection.Dispose() }

    $deploymentPhase = 'Write protected server configuration'
    $serverDir = Join-Path $InstallRoot 'Server'
    $productionConfig = Join-Path $serverDir 'appsettings.Production.json'
    $backupDir = Join-Path $env:ProgramData 'Falah\Backups'
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    $backupAcl = Get-Acl $backupDir; $backupAcl.SetAccessRuleProtection($true,$false); $backupAcl.Access | ForEach-Object { $null = $backupAcl.RemoveAccessRule($_) }
    foreach ($identity in @('SYSTEM','BUILTIN\Administrators')) { $backupAcl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))) }
    Set-Acl -LiteralPath $backupDir -AclObject $backupAcl
    $configObject = @{ Database=@{ MigrateOnStartup=[bool]$initializeNewDatabase }; ConnectionStrings=@{ Falah=$sqlConnection }; Seed=@{ DemoUsers=$false }; Bootstrap=@{ OwnerPin=$ownerPin }; Urls='http://localhost:5151'; Backup=@{ DefaultDirectory=$backupDir } }
    if ($clientUrl -match '^https://') {
        $certPath = $settings.CertificatePath; $certPassword = $settings.CertificatePassword
        if ([string]::IsNullOrWhiteSpace($certPath) -or -not (Test-Path -LiteralPath $certPath)) { throw 'For an HTTPS server URL, select a valid TLS PFX certificate.' }
        $uri = [uri]$clientUrl; $port = if ($uri.IsDefaultPort) { 443 } else { $uri.Port }
        $tlsDir = Join-Path $env:ProgramData 'Falah\Tls'; New-Item -ItemType Directory -Force -Path $tlsDir | Out-Null
        $installedCert = Join-Path $tlsDir 'server.pfx'; Copy-Item -LiteralPath $certPath -Destination $installedCert -Force
        $certAcl = Get-Acl $installedCert; $certAcl.SetAccessRuleProtection($true,$false); $certAcl.Access | ForEach-Object { $null = $certAcl.RemoveAccessRule($_) }
        foreach ($identity in @('SYSTEM','BUILTIN\Administrators')) { $certAcl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($identity,'FullControl','Allow'))) }
        Set-Acl -LiteralPath $installedCert -AclObject $certAcl
        $configObject.Kestrel = @{ Endpoints=@{ Https=@{ Url="https://0.0.0.0:$port"; Certificate=@{ Path=$installedCert; Password=$certPassword } } } }; $configObject.Urls=''
        Get-NetFirewallRule -Group 'Falah Restaurant Deployment' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
        New-NetFirewallRule -Name 'FalahRestaurantHttpsInbound' -DisplayName 'Falah Restaurant API (HTTPS)' -Group 'Falah Restaurant Deployment' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $port -Profile Domain,Private | Out-Null
        $healthUrl = $clientUrl.TrimEnd('/') + '/health'
    } else {
        $healthUrl = 'http://127.0.0.1:5151/health'
    }
    $configObject | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $productionConfig -Encoding UTF8
    $acl = Get-Acl -LiteralPath $productionConfig; $acl.SetAccessRuleProtection($true,$false); $acl.Access | ForEach-Object { $null = $acl.RemoveAccessRule($_) }
    foreach ($identity in @('SYSTEM','BUILTIN\Administrators')) { $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($identity,'FullControl','Allow'))) }
    Set-Acl -LiteralPath $productionConfig -AclObject $acl

    $deploymentPhase = 'Configure and start Falah server service'
    $exe = Join-Path $serverDir 'Falah.Server.exe'
    if ($service) { if ($service.Status -eq 'Running') { Stop-Service FalahServer -Force }; sc.exe config FalahServer "binPath= `"$exe`"" start= auto | Out-Null }
    else { New-Service -Name FalahServer -DisplayName 'Falah Restaurant Server' -BinaryPathName "`"$exe`"" -StartupType Automatic | Out-Null; $createdService = $true }
    Set-Service FalahServer -StartupType Automatic; Start-Service FalahServer
    $deploymentPhase = 'Verify server health endpoint'
    $ready = $false
    for ($i=0; $i -lt 30; $i++) { Start-Sleep -Seconds 2; try { if ((Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2).StatusCode -eq 200) { $ready=$true; break } } catch { }; if ((Get-Service FalahServer).Status -ne 'Running') { break } }
    if (-not $ready) { throw 'FalahServer did not pass its local /health check. See the installer and Windows Application logs.' }
    $deploymentPhase = 'Finalize server configuration and verify health'
    $configObject.Database.MigrateOnStartup=$false
    $configObject.Remove('Bootstrap')
    $configObject | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $productionConfig -Encoding UTF8
    Restart-Service FalahServer -Force
    $readyAfterSecretRemoval = $false
    for ($i=0; $i -lt 30; $i++) { Start-Sleep -Seconds 2; try { if ((Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2).StatusCode -eq 200) { $readyAfterSecretRemoval=$true; break } } catch { }; if ((Get-Service FalahServer).Status -ne 'Running') { break } }
    if (-not $readyAfterSecretRemoval) { throw 'FalahServer failed its final health check after removing the bootstrap PIN. See installer and Windows Application logs.' }
    [Environment]::SetEnvironmentVariable('FALAH_SERVER_URL',$clientUrl,'Machine'); [Environment]::SetEnvironmentVariable('FALAH_SERVER_URL',$clientUrl,'Process')
    $deploymentPhase = 'Write successful installation state'
    $state = @{ Product='Falah Restaurant'; Version='1.0.6'; InstallRoot=$InstallRoot; InstalledAt=(Get-Date).ToUniversalTime().ToString('o'); Mode=$mode; ServerUrl=$clientUrl; Database=$databaseName }
    $state | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
    New-Item -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Force | Out-Null
    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Name InstallRoot -Value $InstallRoot
    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Falah\Restaurant\Installer' -Name Version -Value '1.0.6'
    Write-Host 'Installation and server health verification succeeded.'
} catch {
    $record = $_
    Write-Host "Deployment failed during phase: $deploymentPhase"
    Write-Host "Exception type: $($record.Exception.GetType().FullName)"
    Write-Host "Exception message: $($record.Exception.Message)"
    if ($record.InvocationInfo.ScriptLineNumber) { Write-Host "Script line: $($record.InvocationInfo.ScriptLineNumber)" }
    if ($record.ScriptStackTrace) { Write-Host "Script stack:`r`n$($record.ScriptStackTrace)" }
    if ($createdService) {
        Stop-Service FalahServer -Force -ErrorAction SilentlyContinue
        sc.exe delete FalahServer | Out-Null
        Get-NetFirewallRule -Group 'Falah Restaurant Deployment' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    }
    exit 1
} finally {
    Remove-Item -LiteralPath $SettingsFile -Force -ErrorAction SilentlyContinue
    Stop-Transcript | Out-Null
}



