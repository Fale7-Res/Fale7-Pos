#define AppName "Falah Restaurant"
#define AppVersion "1.0.6"
#define AppPublisher "Falah Restaurant"
#define Workspace GetEnv("FALAH_INSTALLER_WORKSPACE")

[Setup]
AppId={{C547D919-BE32-4DE9-B7AE-1CC50F56D98D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Falah Restaurant
DefaultGroupName=Falah Restaurant
OutputDir={#Workspace}\Output
OutputBaseFilename=Falah-Restaurant-Setup
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
PrivilegesRequired=admin
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
DisableProgramGroupPage=yes
UninstallDisplayName=Falah Restaurant
UninstallDisplayIcon={app}\Cashier\Falah.Client.CashierWinUI.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion=1.0.6.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=Offline installer for Falah Restaurant

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{app}\Deployment"; Permissions: users-readexec
Name: "{app}\Dependencies\Offline Installers"; Permissions: users-readexec
Name: "{app}\Dependencies\Redistributables"; Permissions: users-readexec

[Files]
Source: "{#Workspace}\Application Builds\Cashier\*"; DestDir: "{app}\Cashier"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Workspace}\Application Builds\CallCenter\*"; DestDir: "{app}\CallCenter"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Workspace}\Application Builds\Server\*"; DestDir: "{app}\Server"; Excludes: "appsettings.Production.json"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Workspace}\Application Builds\PrintAgent\*"; DestDir: "{app}\PrintAgent"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Workspace}\Dependencies\Offline Installers\SQLEXPR_x64_ENU.exe"; DestDir: "{app}\Dependencies\Offline Installers"; Flags: ignoreversion
Source: "{#Workspace}\Dependencies\Redistributables\VC_redist.x64.exe"; DestDir: "{app}\Dependencies\Redistributables"; Flags: ignoreversion
Source: "{#Workspace}\Installer Scripts\Configure-Deployment.ps1"; DestDir: "{app}\Deployment"; Flags: ignoreversion
Source: "{#Workspace}\Installer Scripts\Uninstall-Deployment.ps1"; DestDir: "{app}\Deployment"; Flags: ignoreversion
Source: "{#Workspace}\Installer Scripts\Stop-Deployment.ps1"; DestDir: "{app}\Deployment"; Flags: ignoreversion
Source: "{#Workspace}\Documentation\Deployment Guide.md"; DestDir: "{app}\Documentation"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Falah Restaurant\Cashier"; Filename: "{app}\Cashier\Falah.Client.CashierWinUI.exe"
Name: "{autoprograms}\Falah Restaurant\Call Center"; Filename: "{app}\CallCenter\Falah.Client.CallCenterWinUI.exe"
Name: "{autoprograms}\Falah Restaurant\Uninstall"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Falah Restaurant Cashier"; Filename: "{app}\Cashier\Falah.Client.CashierWinUI.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a cashier shortcut on the desktop"; GroupDescription: "Additional shortcuts:"

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Deployment\Uninstall-Deployment.ps1"" -InstallRoot ""{app}"""; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: files; Name: "{app}\Server\appsettings.Production.json"

[Code]
var
  ModePage: TInputOptionWizardPage;
  RolePage: TInputOptionWizardPage;
  SettingsPage: TInputQueryWizardPage;
  DatabasePage: TInputOptionWizardPage;
  WorkstationPage: TInputQueryWizardPage;
  SettingsPath: String;

procedure InitializeWizard;
begin
  ModePage := CreateInputOptionPage(wpWelcome, 'Choose installation mode', 'Select how SQL Server will be handled',
    'For a server installation, Full Installation installs SQL Server Express only if no compatible SQL engine is available. SQL unattended setup accepts Microsoft license terms and suppresses its privacy notice. The other mode never installs or changes SQL Server.', True, False);
  ModePage.Add('Full Installation - install the application and SQL Server if needed on the server host');
  ModePage.Add('Install Application (SQL Server Already Available) - never install or change SQL Server');
  ModePage.SelectedValueIndex := 0;

  RolePage := CreateInputOptionPage(ModePage.ID, 'Choose this computer role', 'Select how this computer will be used',
    'The Workstation role installs the client modules and verifies the central API. It does not configure or start a local API or SQL service.', True, False);
  RolePage.Add('Application server - host the API and connect it to SQL Server');
  RolePage.Add('Workstation - run clients that connect to a separately installed API server');
  RolePage.SelectedValueIndex := 0;

  SettingsPage := CreateInputQueryPage(RolePage.ID, 'Configure server and database', 'Enter the API address and SQL connection for this installation',
    'For computers on a network, use HTTPS and provide a certificate. The installer validates SQL connectivity before it starts the server.');
  SettingsPage.Add('Server API URL (localhost for this computer; HTTPS for network clients):', False);
  SettingsPage.Add('SQL connection string (include Database/Initial Catalog):', False);
  SettingsPage.Add('TLS PFX certificate path (required for HTTPS):', False);
  SettingsPage.Add('TLS PFX password:', True);
  SettingsPage.Add('Initial owner PIN (4–6 digits, not all the same):', True);
  SettingsPage.Values[0] := '';
  SettingsPage.Values[1] := 'Server=.\FALAHRESTAURANT;Database=Falah;Trusted_Connection=True;TrustServerCertificate=True';

  DatabasePage := CreateInputOptionPage(SettingsPage.ID, 'Database authorization', 'Choose how the installer may use the selected database',
    'Existing databases are never migrated by the installer. On startup, the application may set READ_COMMITTED_SNAPSHOT when needed.', False, False);
  DatabasePage.Add('Initialize only if this database does not already exist (create it, run initial migrations, then disable startup migration)');
  DatabasePage.Add('Authorize use of an existing database (the application may apply its READ_COMMITTED_SNAPSHOT setting)');
  DatabasePage.Values[0] := False;
  DatabasePage.Values[1] := False;

  WorkstationPage := CreateInputQueryPage(DatabasePage.ID, 'Connect to the central server', 'Enter the API address configured on the application server',
    'Network connections must use HTTPS and the server certificate authority must be trusted on this computer.');
  WorkstationPage.Add('Central Falah API URL:', False);
  WorkstationPage.Values[0] := '';
end;

procedure WriteInstallSettings; forward;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    if FileExists(ExpandConstant('{app}\Deployment\Stop-Deployment.ps1')) then
    begin
      if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
        '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\Deployment\Stop-Deployment.ps1') + '" -InstallRoot "' + ExpandConstant('{app}') + '" -Role Server',
        '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then RaiseException('Could not stop the existing Falah server service safely.');
      if ResultCode <> 0 then RaiseException('Could not stop the existing Falah server service safely.');
    end;
  end;
  if CurStep = ssPostInstall then
  begin
    WriteInstallSettings;
    Log('Starting Falah deployment configuration.');
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\Deployment\Configure-Deployment.ps1') + '" -SettingsFile "' + SettingsPath + '" -InstallRoot "' + ExpandConstant('{app}') + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException('Could not start Falah deployment configuration: ' + SysErrorMessage(ResultCode));
    Log('Falah deployment configuration exit code: ' + IntToStr(ResultCode));
    if ResultCode <> 0 then
      RaiseException('Falah deployment configuration failed (exit code ' + IntToStr(ResultCode) + '). Review the latest log in C:\ProgramData\Falah\Logs\Installer.');
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ApiUrl, OwnerPin: String;
  I: Integer;
  PinRepeats: Boolean;
begin
  Result := True;
  if (CurPageID = SettingsPage.ID) and (RolePage.SelectedValueIndex = 0) then
  begin
    ApiUrl := Trim(SettingsPage.Values[0]);
    OwnerPin := Trim(SettingsPage.Values[4]);
    if ApiUrl = '' then
    begin
      MsgBox('Enter the server API URL. Use http://localhost:5151 when this computer hosts the application.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (Pos('http://', Lowercase(ApiUrl)) <> 1) and (Pos('https://', Lowercase(ApiUrl)) <> 1) then
    begin
      MsgBox('The server API URL must start with http:// or https://.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if Trim(SettingsPage.Values[1]) = '' then
    begin
      MsgBox('Enter the SQL connection string, including Database/Initial Catalog.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (Length(OwnerPin) < 4) or (Length(OwnerPin) > 6) then
    begin
      MsgBox('Enter a 4–6 digit initial owner PIN.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    PinRepeats := True;
    for I := 1 to Length(OwnerPin) do
      if (OwnerPin[I] < '0') or (OwnerPin[I] > '9') then
      begin
        MsgBox('The initial owner PIN must contain digits only.', mbError, MB_OK);
        Result := False;
        Exit;
      end;
    for I := 2 to Length(OwnerPin) do
      if OwnerPin[I] <> OwnerPin[1] then PinRepeats := False;
    if PinRepeats then
    begin
      MsgBox('The initial owner PIN cannot repeat the same digit.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (Pos('https://', Lowercase(ApiUrl)) = 1) and (Trim(SettingsPage.Values[2]) = '') then
    begin
      MsgBox('Select the TLS PFX certificate for an HTTPS API URL.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
  if (CurPageID = DatabasePage.ID) and (RolePage.SelectedValueIndex = 0) then
  begin
    if DatabasePage.Values[0] = DatabasePage.Values[1] then
    begin
      MsgBox('Choose exactly one database option: initialize a new database, or authorize an existing database.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
  if (CurPageID = WorkstationPage.ID) and (RolePage.SelectedValueIndex = 1) then
  begin
    ApiUrl := Trim(WorkstationPage.Values[0]);
    if ApiUrl = '' then
    begin
      MsgBox('Enter the central Falah API URL.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (Pos('https://', Lowercase(ApiUrl)) <> 1) and
      (Pos('http://localhost', Lowercase(ApiUrl)) <> 1) and
      (Pos('http://127.0.0.1', Lowercase(ApiUrl)) <> 1) then
    begin
      MsgBox('Remote server URLs must use HTTPS.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = SettingsPage.ID) or (PageID = DatabasePage.ID) then Result := RolePage.SelectedValueIndex = 1;
  if PageID = WorkstationPage.ID then Result := RolePage.SelectedValueIndex = 0;
end;

procedure WriteInstallSettings;
var
  ModeValue, RoleValue, ServerUrlValue, Body: String;
begin
  if ModePage.SelectedValueIndex = 0 then ModeValue := 'Full' else ModeValue := 'WithoutSql';
  if RolePage.SelectedValueIndex = 0 then
  begin
    RoleValue := 'Server'; ServerUrlValue := SettingsPage.Values[0];
    if Trim(ServerUrlValue) = '' then RaiseException('Enter the server API URL.');
    if Trim(SettingsPage.Values[1]) = '' then RaiseException('Enter the SQL connection string.');
    if (Length(SettingsPage.Values[4]) < 4) or (Length(SettingsPage.Values[4]) > 6) or (Pos(' ', SettingsPage.Values[4]) > 0) then RaiseException('Enter a 4–6 digit initial owner PIN.');
    if DatabasePage.Values[0] and DatabasePage.Values[1] then RaiseException('Choose one database authorization option, not both.');
    if not DatabasePage.Values[0] and not DatabasePage.Values[1] then RaiseException('Choose whether to initialize a new database or authorize use of an existing database.');
    if (Pos('https://', Lowercase(ServerUrlValue)) = 1) and (Trim(SettingsPage.Values[2]) = '') then RaiseException('Select the TLS PFX certificate for an HTTPS API URL.');
  end
  else
  begin
    RoleValue := 'Workstation'; ServerUrlValue := WorkstationPage.Values[0];
    if Trim(ServerUrlValue) = '' then RaiseException('Enter the central server API URL.');
  end;
  SettingsPath := ExpandConstant('{tmp}\falah-install-settings.ini');
  Body := 'Mode=' + ModeValue + #13#10 +
    'Role=' + RoleValue + #13#10 +
    'ServerUrl=' + ServerUrlValue + #13#10 +
    'SqlConnection=' + SettingsPage.Values[1] + #13#10 +
    'CertificatePath=' + SettingsPage.Values[2] + #13#10 +
    'CertificatePassword=' + SettingsPage.Values[3] + #13#10 +
    'OwnerPin=' + SettingsPage.Values[4] + #13#10 +
    'InitializeNewDatabase=' + IntToStr(Ord(DatabasePage.Values[0])) + #13#10 +
    'AllowExistingDatabase=' + IntToStr(Ord(DatabasePage.Values[1])) + #13#10;
  if not SaveStringToFile(SettingsPath, Body, False) then RaiseException('Could not create the protected installation settings file.');
end;
