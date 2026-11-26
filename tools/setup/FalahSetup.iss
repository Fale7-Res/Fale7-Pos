; Falah - one Setup.exe for every restaurant PC. Built by tools\build-setup.ps1, which passes:
;   /DPkgId=<release id>  /DPkgDir=<Falah-Install-<id> folder>  /DSqlInstaller=<SQLEXPR_x64_ENU.exe>  /DOutDir=<output folder>
;   /DDryRun  (test build only: no elevation, nothing installed, every step is written to a file instead)
;
; The wizard asks every question itself (what this PC is, the owner PIN, the printers and their PINs,
; the server address): the Windows 10 console cannot display Arabic. It then runs the deploy\*.ps1
; steps hidden through Run-Step.ps1 and shows each result here. On the server PC it installs
; SQL Server 2022 Express silently when the SQLEXPRESS instance is missing. Setup itself never
; connects to a database; PINs reach the steps through environment variables, never a command line.

#ifndef PkgId
  #error PkgId is required (run tools\build-setup.ps1)
#endif

[Setup]
AppId={{6C1F4E8A-3B7D-4E52-9A0C-F4A1D9E27B63}
AppName=فالح أبو العنب - نظام المطعم
AppVersion={#PkgId}
AppPublisher=Falah Abu El Enab
#ifdef DryRun
DefaultDirName={#OutDir}\dryrun-install
PrivilegesRequired=lowest
OutputBaseFilename=Falah-Setup-{#PkgId}-DRYRUN
#else
DefaultDirName={sd}\Falah\Package\{#PkgId}
PrivilegesRequired=admin
OutputBaseFilename=Falah-Setup-{#PkgId}
#endif
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousAppDir=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#OutDir}
Compression=lzma2/normal
SolidCompression=yes
LZMANumBlockThreads=4
; Owner request 2026-11-26: removable from Windows' Apps list. The uninstaller runs deploy\Uninstall-Falah.ps1
; (see [Code] below); the database and SQL Server go only when the owner ticks the box, after a final backup.
Uninstallable=yes
UninstallDisplayName=فالح أبو العنب - نظام المطعم
UninstallDisplayIcon={app}\falah.ico
#ifdef IconFile
SetupIconFile={#IconFile}
#endif
WizardStyle=modern
SetupLogging=yes
ShowLanguageDialog=no
CloseApplications=no

[Languages]
Name: "ar"; MessagesFile: "compiler:Languages\Arabic.isl"

[Files]
Source: "{#PkgDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "Certificate\*.cer"
; Only the server PC needs SQL Server; it goes to {tmp} and is deleted when Setup ends.
Source: "{#SqlInstaller}"; DestDir: "{tmp}"; Flags: nocompression deleteafterinstall; Check: NeedsSql
#ifdef IconFile
Source: "{#IconFile}"; DestDir: "{app}"; DestName: "falah.ico"; Flags: ignoreversion
#endif

[Dirs]
Name: "{app}\Certificate"

[UninstallDelete]
; The installation package this uninstaller came from (the script removes the older ones).
Type: filesandordirs; Name: "{app}"

[Code]
const
  RoleServer = 0;
  RoleCashier = 1;
  RoleCallCenter = 2;
  RolePrinters = 3;
  DRIVE_FIXED = 3;
  RouteCount = 6;
  NoPrinter = '(مفيش)';

var
  RolePage: TInputOptionWizardPage;
  OwnerPage: TInputQueryWizardPage;
  BackupPage: TInputDirWizardPage;
  ServerPage: TInputQueryWizardPage;
  CertPage: TInputFileWizardPage;
  PrintersPage: TWizardPage;
  RouteIds, RouteNames: array of String;
  PrinterBoxes: array of TNewComboBox;
  PinBoxes: array of TPasswordEdit;
  StepsFailed: Boolean;
  RestartAfterSql: Boolean;
  Outcome: String;

function GetDriveType(lpRootPathName: String): Cardinal;
  external 'GetDriveTypeW@kernel32.dll stdcall';
function SetEnvironmentVariable(lpName, lpValue: String): Boolean;
  external 'SetEnvironmentVariableW@kernel32.dll stdcall';

function Role: Integer;
begin
  Result := RolePage.SelectedValueIndex;
end;

function SqlExpressInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\MSSQL$SQLEXPRESS');
end;

function ServerAlreadyInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\FalahServer');
end;

function NeedsSql: Boolean;
begin
  Result := (Role = RoleServer) and not SqlExpressInstalled;
end;

function IsDigits(const S: String): Boolean;
var
  I: Integer;
begin
  Result := Length(S) > 0;
  for I := 1 to Length(S) do
    if (S[I] < '0') or (S[I] > '9') then Result := False;
end;

function ValidPin(const S: String): Boolean;
begin
  Result := IsDigits(S) and (Length(S) >= 4) and (Length(S) <= 8);
end;

// The fixed drive other than Windows' own with the most free space (the HDD beside the SSD), or C:.
function DefaultBackupDir: String;
var
  Letter: Integer;
  Root, SystemLetter: String;
  Free, Total, BestFree: Int64;
begin
  Result := ExpandConstant('{sd}') + '\Falah\Backups';
  SystemLetter := Uppercase(Copy(ExpandConstant('{sd}'), 1, 1));
  BestFree := 0;
  for Letter := Ord('C') to Ord('Z') do
  begin
    Root := Chr(Letter) + ':\';
    if (Chr(Letter) <> SystemLetter) and (GetDriveType(Root) = DRIVE_FIXED) and GetSpaceOnDisk64(Root, Free, Total) then
      if Free > BestFree then
      begin
        BestFree := Free;
        Result := Chr(Letter) + ':\FalahBackups';
      end;
  end;
end;

procedure AddInstalledPrinters(Box: TNewComboBox);
var
  Locator, Service, Items: Variant;
  I: Integer;
begin
  Box.Items.Add(NoPrinter);
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Items := Service.ExecQuery('SELECT Name FROM Win32_Printer');
    for I := 0 to Items.Count - 1 do
      Box.Items.Add(Items.ItemIndex(I).Name);
  except
  end;
  Box.ItemIndex := 0;
end;

procedure AddRoute(I: Integer; const Id, Name: String);
begin
  RouteIds[I] := Id;
  RouteNames[I] := Name;
end;

procedure AddHeading(const Text: String; Left: Integer);
var
  Head: TNewStaticText;
begin
  Head := TNewStaticText.Create(PrintersPage);
  Head.Parent := PrintersPage.Surface;
  Head.Caption := Text;
  Head.Font.Style := [fsBold];
  Head.Left := Left;
  Head.Top := 0;
end;

procedure CreatePrintersPage;
var
  I, Top: Integer;
  Caption: TNewStaticText;
begin
  SetArrayLength(RouteIds, RouteCount);
  SetArrayLength(RouteNames, RouteCount);
  AddRoute(0, 'Receipt', 'إيصال الكاشير');
  AddRoute(1, 'CallCenter', 'الكول سنتر');
  AddRoute(2, 'Charcoal', 'الفحم');
  AddRoute(3, 'Grill', 'الشواية');
  AddRoute(4, 'DeliveryOrder', 'طلب التوصيل');
  AddRoute(5, 'DriverDispatch', 'خروج الطيار');
  SetArrayLength(PrinterBoxes, RouteCount);
  SetArrayLength(PinBoxes, RouteCount);
  PrintersPage := CreateCustomPage(CertPage.ID, 'الطابعات',
    'لكل نوع ورق اختار الطابعة اللي هتطلعه، واكتب رمز مستخدم الطباعة بتاعها. اللي مش على الجهاز ده سيبه (مفيش).');
  AddHeading('نوع الورق', 0);
  AddHeading('الطابعة', ScaleX(100));
  AddHeading('رمز الطباعة', ScaleX(345));
  Top := ScaleY(20);
  for I := 0 to RouteCount - 1 do
  begin
    Caption := TNewStaticText.Create(PrintersPage);
    Caption.Parent := PrintersPage.Surface;
    Caption.Caption := RouteNames[I];
    Caption.Left := 0;
    Caption.Top := Top + ScaleY(3);

    PrinterBoxes[I] := TNewComboBox.Create(PrintersPage);
    PrinterBoxes[I].Parent := PrintersPage.Surface;
    PrinterBoxes[I].Style := csDropDownList;
    PrinterBoxes[I].Left := ScaleX(100);
    PrinterBoxes[I].Top := Top;
    PrinterBoxes[I].Width := ScaleX(230);
    AddInstalledPrinters(PrinterBoxes[I]);

    PinBoxes[I] := TPasswordEdit.Create(PrintersPage);
    PinBoxes[I].Parent := PrintersPage.Surface;
    PinBoxes[I].Left := ScaleX(345);
    PinBoxes[I].Top := Top;
    PinBoxes[I].Width := ScaleX(80);
    Top := Top + ScaleY(30);
  end;
end;

function KnownServerUrl: String;
begin
  if not RegQueryStringValue(HKLM64, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'FALAH_SERVER_URL', Result) then
    Result := 'https://FALAH-SERVER:5443';
end;

function KnownCertificate: String;
begin
  Result := '';
  if FileExists(ExpandConstant('{src}\falah-server.cer')) then Result := ExpandConstant('{src}\falah-server.cer')
  else if FileExists(ExpandConstant('{sd}\Falah\falah-server.cer')) then Result := ExpandConstant('{sd}\Falah\falah-server.cer');
end;

procedure InitializeWizard;
begin
  RolePage := CreateInputOptionPage(wpWelcome,
    'نوع الجهاز', 'الجهاز ده هيشتغل إيه في المطعم؟',
    'ابدأ بجهاز السيرفر. بعده افتح الكاشير واعمل المستخدمين (ومنهم مستخدمين الطباعة)، وبعدين باقي الأجهزة والطابعات.',
    True, False);
  RolePage.Add('جهاز السيرفر الرئيسي (السيرفر + الكاشير على نفس الجهاز)');
  RolePage.Add('جهاز كاشير تاني');
  RolePage.Add('جهاز الكول سنتر');
  RolePage.Add('إضافة طابعات على الجهاز ده');
  // Setup.exe /ROLE=server|cashier|callcenter|printers preselects the choice (the person can still change it).
  case Lowercase(ExpandConstant('{param:ROLE|server}')) of
    'cashier': RolePage.SelectedValueIndex := RoleCashier;
    'callcenter': RolePage.SelectedValueIndex := RoleCallCenter;
    'printers': RolePage.SelectedValueIndex := RolePrinters;
  else
    RolePage.SelectedValueIndex := RoleServer;
  end;

  OwnerPage := CreateInputQueryPage(RolePage.ID,
    'رمز المالك', 'الرقم السري اللي صاحب المطعم هيدخل بيه أول مرة',
    'من 4 لـ 8 أرقام، ومش سهل التخمين (مش 1111 ولا 1234). لو السيرفر متثبت قبل كده سيب الخانتين فاضيين.');
  OwnerPage.Add('رمز المالك:', True);
  OwnerPage.Add('اكتبه تاني:', True);

  BackupPage := CreateInputDirPage(OwnerPage.ID,
    'مكان النسخ الاحتياطية', 'السيرفر بياخد نسخة احتياطية كاملة كل يوم ويحتفظ بآخر 60 نسخة.',
    'الأفضل تكون على هارد غير اللي عليه الويندوز. ده المكان المقترح:', False, '');
  BackupPage.Add('');
  BackupPage.Values[0] := DefaultBackupDir;

  ServerPage := CreateInputQueryPage(BackupPage.ID,
    'عنوان السيرفر', 'العنوان اللي ظهر في آخر تثبيت جهاز السيرفر',
    'لازم يبدأ بـ https:// ويكون بنفس الاسم اللي ظهر هناك. على جهاز السيرفر نفسه: https://localhost:5443');
  ServerPage.Add('العنوان:', False);
  ServerPage.Values[0] := KnownServerUrl;

  CertPage := CreateInputFilePage(ServerPage.ID,
    'شهادة السيرفر', 'الملف falah-server.cer اللي اتعمل على جهاز السيرفر',
    'Setup بيحطه جنبه على الفلاشة بعد تثبيت السيرفر. ولو مش لاقيه: على جهاز السيرفر في C:\Falah\falah-server.cer');
  CertPage.Add('الملف:', 'شهادة السيرفر (*.cer)|*.cer', '.cer');
  CertPage.Values[0] := KnownCertificate;

  CreatePrintersPage;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = OwnerPage.ID) or (PageID = BackupPage.ID) then Result := Role <> RoleServer;
  if (PageID = ServerPage.ID) or (PageID = CertPage.ID) then Result := Role = RoleServer;
  if PageID = PrintersPage.ID then Result := Role <> RolePrinters;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Url: String;
  I, Chosen: Integer;
begin
  Result := True;
  if CurPageID = OwnerPage.ID then
  begin
    if (OwnerPage.Values[0] = '') and (OwnerPage.Values[1] = '') and ServerAlreadyInstalled then exit;
    if not ValidPin(OwnerPage.Values[0]) then
    begin
      MsgBox('رمز المالك لازم يكون أرقام بس، من 4 لـ 8 أرقام.', mbError, MB_OK);
      Result := False;
    end
    else if OwnerPage.Values[0] <> OwnerPage.Values[1] then
    begin
      MsgBox('الرمزين مش زي بعض. اكتبهم تاني.', mbError, MB_OK);
      Result := False;
    end;
  end;
  if CurPageID = BackupPage.ID then
    if (Length(BackupPage.Values[0]) < 4) or (Pos(':\', BackupPage.Values[0]) <> 2) then
    begin
      MsgBox('اختار فولدر على هارد، مثلاً D:\FalahBackups', mbError, MB_OK);
      Result := False;
    end;
  if CurPageID = ServerPage.ID then
  begin
    Url := Trim(ServerPage.Values[0]);
    if (Pos('https://', Lowercase(Url)) <> 1) or (Length(Url) < 12) then
    begin
      MsgBox('العنوان لازم يبدأ بـ https:// زي https://FALAH-SERVER:5443', mbError, MB_OK);
      Result := False;
    end;
  end;
  if CurPageID = CertPage.ID then
    if not FileExists(CertPage.Values[0]) then
    begin
      MsgBox('اختار ملف falah-server.cer اللي جه من جهاز السيرفر.', mbError, MB_OK);
      Result := False;
    end;
  if CurPageID = PrintersPage.ID then
  begin
    Chosen := 0;
    for I := 0 to RouteCount - 1 do
      if PrinterBoxes[I].ItemIndex > 0 then
      begin
        Chosen := Chosen + 1;
        if not ValidPin(PinBoxes[I].Text) then
        begin
          MsgBox('اكتب رمز مستخدم الطباعة لـ "' + RouteNames[I] + '" (من 4 لـ 8 أرقام).' + #13#10 +
            'لو لسه ما عملتهوش: افتح الكاشير برمز المالك، ومن "المستخدمون والصلاحيات" اعمل مستخدم دوره print_agent.',
            mbError, MB_OK);
          Result := False;
          exit;
        end;
      end;
    if Chosen = 0 then
    begin
      MsgBox('اختار طابعة واحدة على الأقل.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  I: Integer;
begin
  Result := 'الجهاز: ' + RolePage.CheckListBox.ItemCaption[Role] + NewLine + NewLine;
  case Role of
    RoleServer:
      begin
        if SqlExpressInstalled then Result := Result + 'SQL Server Express: موجود بالفعل' + NewLine
        else Result := Result + 'SQL Server 2022 Express: هيتثبت (حوالي 10 دقايق)' + NewLine;
        Result := Result + 'السيرفر: خدمة ويندوز بتقوم مع الجهاز، على https://' + GetComputerNameString + ':5443' + NewLine;
        Result := Result + 'النسخ الاحتياطية في: ' + BackupPage.Values[0] + NewLine;
        Result := Result + 'لو فيه قاعدة بيانات قبل كده: هتتاخد منها نسخة احتياطية ويتأكد منها قبل أي تحديث.' + NewLine;
        Result := Result + 'الكاشير: على نفس الجهاز، واختصار على سطح المكتب.';
      end;
    RolePrinters:
      begin
        Result := Result + 'عنوان السيرفر: ' + Trim(ServerPage.Values[0]) + NewLine + 'الطابعات:' + NewLine;
        for I := 0 to RouteCount - 1 do
          if PrinterBoxes[I].ItemIndex > 0 then
            Result := Result + Space + RouteNames[I] + ': ' + PrinterBoxes[I].Text + NewLine;
      end;
  else
    Result := Result + 'عنوان السيرفر: ' + Trim(ServerPage.Values[0]) + NewLine + 'الشهادة: ' + CertPage.Values[0];
  end;
end;

// Runs one deploy step hidden, waits, and reads its result file. Returns True only on success;
// on failure shows the step's own Arabic message and where the full log is.
function RunStep(const Step, Extra, What: String): Boolean;
var
  ResultCode: Integer;
  ResultFile, Parameters: String;
  Lines: TArrayOfString;
begin
  WizardForm.StatusLabel.Caption := What + '...';
  WizardForm.FilenameLabel.Caption := 'ممكن ياخد دقيقة أو اتنين. ما تقفلش الجهاز.';
  ResultFile := ExpandConstant('{tmp}\step-result.txt');
  DeleteFile(ResultFile);
  Parameters := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\Run-Step.ps1') +
    '" -Step ' + Step + ' -ResultFile "' + ResultFile + '" ' + Extra;
#ifdef DryRun
  SaveStringToFile(ExpandConstant('{app}\dryrun-steps.txt'), Step + ' | ' + Extra + ' | owner-pin-set=' +
    IntToStr(Ord(GetEnv('FALAH_SETUP_OWNER_PIN') <> '')) + ' print-pin-set=' + IntToStr(Ord(GetEnv('FALAH_SETUP_PRINT_PIN') <> '')) + #13#10, True);
  if Lowercase(ExpandConstant('{param:FAILSTEP|}')) = Lowercase(Step) then
    // Same file format and encoding Run-Step.ps1 writes, produced the same way (PowerShell, UTF-8 with BOM).
    Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -NonInteractive -Command "[IO.File]::WriteAllLines(''' + ResultFile + ''', [string[]]@(''FAILED'', ''الجهاز مش شايف السيرفر على العنوان ده (تجربة).'', ''C:\Falah\Logs\dry-run.log''), (New-Object Text.UTF8Encoding($true))); exit 1"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
  else
  begin
    SaveStringToFile(ResultFile, 'OK' + #13#10 + '' + #13#10 + 'dry run', False);
    ResultCode := 0;
  end;
#else
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Parameters, ExpandConstant('{app}'),
    SW_HIDE, ewWaitUntilTerminated, ResultCode);
#endif
  // Read the result first: a failed step's message is in this file too (and/or short-circuits).
  if not LoadStringsFromFile(ResultFile, Lines) then SetArrayLength(Lines, 0);
  Result := (ResultCode = 0) and (GetArrayLength(Lines) > 0) and (Lines[0] = 'OK');
  if not Result then
  begin
    StepsFailed := True;
    if GetArrayLength(Lines) >= 3 then
      MsgBox(What + ' ما خلصش:' + #13#10#13#10 + Lines[1] + #13#10#13#10 +
        'السجل الكامل: ' + Lines[2] + #13#10#13#10 + 'بعد ما المشكلة تتحل شغّل Setup تاني بنفس الاختيار.', mbError, MB_OK)
    else
      MsgBox(What + ' ما خلصش (كود ' + IntToStr(ResultCode) + '). السجلات في C:\Falah\Logs', mbError, MB_OK);
  end;
end;

function InstallSqlExpress: Boolean;
var
  ResultCode: Integer;
  Extracted: String;
begin
  Result := False;
#ifdef DryRun
  SaveStringToFile(ExpandConstant('{app}\dryrun-steps.txt'), 'SQL Express install' + #13#10, True);
  Result := True;
  exit;
#endif
  Extracted := ExpandConstant('{sd}\FalahSqlSetup');
  WizardForm.StatusLabel.Caption := 'فك ملفات SQL Server 2022 Express...';
  WizardForm.FilenameLabel.Caption := '';
  if not Exec(ExpandConstant('{tmp}\SQLEXPR_x64_ENU.exe'), '/q /x:"' + Extracted + '"', '', SW_HIDE,
              ewWaitUntilTerminated, ResultCode) or not FileExists(Extracted + '\SETUP.EXE') then
  begin
    StepsFailed := True;
    MsgBox('تعذّر فك ملفات SQL Server (كود ' + IntToStr(ResultCode) + ').', mbError, MB_OK);
    exit;
  end;
  WizardForm.StatusLabel.Caption := 'تثبيت SQL Server 2022 Express...';
  WizardForm.FilenameLabel.Caption := 'ممكن ياخد 10 دقايق. ما تقفلش الجهاز.';
  Exec(Extracted + '\SETUP.EXE',
    '/QS /ACTION=Install /FEATURES=SQLENGINE /INSTANCENAME=SQLEXPRESS ' +
    '/SQLSYSADMINACCOUNTS="BUILTIN\Administrators" /UPDATEENABLED=False /IACCEPTSQLSERVERLICENSETERMS',
    Extracted, SW_SHOW, ewWaitUntilTerminated, ResultCode);
  DelTree(Extracted, True, True, True);
  if ((ResultCode = 0) or (ResultCode = 3010)) and SqlExpressInstalled then
  begin
    RestartAfterSql := ResultCode = 3010;
    Result := True;
  end
  else
  begin
    StepsFailed := True;
    MsgBox('تثبيت SQL Server ما نجحش (كود ' + IntToStr(ResultCode) + ').' + #13#10#13#10 +
      'لو الويندوز مستني إعادة تشغيل، اعمل ريستارت وشغّل Setup تاني.' + #13#10 +
      'التفاصيل في: C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log\Summary.txt', mbError, MB_OK);
  end;
end;

// The stations need the server's certificate: leave a copy beside Setup.exe (the USB stick).
function LeaveCertificateBesideSetup: String;
var
  Source, Target: String;
begin
  Source := ExpandConstant('{sd}\Falah\falah-server.cer');
  Target := ExpandConstant('{src}\falah-server.cer');
  if FileExists(Source) and FileCopy(Source, Target, False) then
    Result := 'ملف الشهادة اتحط جنب Setup.exe:' + #13#10 + Target + #13#10 + 'خد Setup.exe والملف ده مع بعض لباقي الأجهزة.'
  else
    Result := 'انسخ الملف ' + Source + ' على الفلاشة جنب Setup.exe، عشان باقي الأجهزة محتاجاه.';
end;

procedure InstallServerPc;
begin
  if NeedsSql then
    if not InstallSqlExpress then exit;
  SetEnvironmentVariable('FALAH_SETUP_OWNER_PIN', OwnerPage.Values[0]);
  try
    if not RunStep('Server', '-Yes -BackupDir "' + BackupPage.Values[0] + '"', 'تثبيت السيرفر') then exit;
  finally
    SetEnvironmentVariable('FALAH_SETUP_OWNER_PIN', '');
  end;
  if not RunStep('Cashier', '-ServerUrl "https://localhost:5443"', 'تثبيت الكاشير') then exit;
  Outcome := 'السيرفر شغال على:' + #13#10 + 'https://' + GetComputerNameString + ':5443' + #13#10 +
    '(اكتب العنوان ده عندك، هتحتاجه على باقي الأجهزة)' + #13#10#13#10 +
    LeaveCertificateBesideSetup + #13#10#13#10 +
    'الخطوة الجاية: افتح "فالح - الكاشير" من سطح المكتب برمز المالك، واعمل المستخدمين، ' +
    'ومستخدم طباعة (دوره print_agent) لكل طابعة. بعدين شغّل Setup تاني واختار "إضافة طابعات".';
end;

procedure InstallPrinters;
var
  I: Integer;
  Done, Failed: String;
begin
  for I := 0 to RouteCount - 1 do
    if PrinterBoxes[I].ItemIndex > 0 then
    begin
      SetEnvironmentVariable('FALAH_SETUP_PRINT_PIN', PinBoxes[I].Text);
      try
        if RunStep('PrintAgent', '-Route ' + RouteIds[I] + ' -PrinterName "' + PrinterBoxes[I].Text + '" -ServerUrl "' +
                   Trim(ServerPage.Values[0]) + '" -CertificatePath "' + CertPage.Values[0] + '"',
                   'تثبيت طباعة ' + RouteNames[I]) then
          Done := Done + '  ' + RouteNames[I] + ': ' + PrinterBoxes[I].Text + #13#10
        else
          Failed := Failed + '  ' + RouteNames[I] + #13#10;
      finally
        SetEnvironmentVariable('FALAH_SETUP_PRINT_PIN', '');
      end;
    end;
  if Done <> '' then Outcome := 'الطابعات اللي اشتغلت:' + #13#10 + Done + #13#10 +
    'جرّب: اعمل طلب من الكاشير واتأكد إن كل ورقة طلعت على الطابعة بتاعتها بس.';
  if Failed <> '' then Outcome := Outcome + #13#10#13#10 + 'ما اشتغلتش:' + #13#10 + Failed;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  StationArgs: String;
begin
  if CurStep <> ssPostInstall then exit;
  StationArgs := '-ServerUrl "' + Trim(ServerPage.Values[0]) + '" -CertificatePath "' + CertPage.Values[0] + '"';
  case Role of
    RoleServer: InstallServerPc;
    RoleCashier:
      if RunStep('Cashier', StationArgs, 'تثبيت الكاشير') then
        Outcome := 'الكاشير اتثبت. افتحه من "فالح - الكاشير" على سطح المكتب.';
    RoleCallCenter:
      if RunStep('CallCenter', StationArgs, 'تثبيت الكول سنتر') then
        Outcome := 'الكول سنتر اتثبت. افتحه من "فالح - الكول سنتر" على سطح المكتب.';
    RolePrinters: InstallPrinters;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    if StepsFailed then
      WizardForm.FinishedHeadingLabel.Caption := 'التثبيت ما كملش'
    else
      WizardForm.FinishedHeadingLabel.Caption := 'تم التثبيت';
    if Outcome = '' then Outcome := 'راجع الرسالة اللي ظهرت. السجلات في C:\Falah\Logs';
    WizardForm.FinishedLabel.Caption := Outcome;
    WizardForm.AdjustLabelHeight(WizardForm.FinishedLabel);
  end;
end;

function NeedRestart: Boolean;
begin
  Result := RestartAfterSql;
end;

// ---- Uninstall (owner request 2026-11-26) ----------------------------------------------------------------------
// The programs, services, shortcuts and settings always go. The database and SQL Server go only when the owner
// ticks the box and confirms; the script takes a final verified backup first and keeps the backup folders.
// A silent uninstall (/SILENT or /VERYSILENT) asks nothing and removes the database only with /REMOVEDATA.

var
  RemoveData: Boolean;

function InitializeUninstall: Boolean;
var
  Form: TSetupForm;
  Info: TNewStaticText;
  RemoveDataBox: TNewCheckBox;
  OkButton, CancelButton: TNewButton;
begin
  RemoveData := Pos('/REMOVEDATA', Uppercase(GetCmdTail)) > 0;
  Result := True;
  if UninstallSilent then exit;
  Form := CreateCustomForm(ScaleX(480), ScaleY(250), False, False);
  try
    Form.Caption := 'إزالة فالح';

    Info := TNewStaticText.Create(Form);
    Info.Parent := Form;
    Info.AutoSize := False;
    Info.WordWrap := True;
    Info.Left := ScaleX(16);
    Info.Top := ScaleY(16);
    Info.Width := Form.ClientWidth - ScaleX(32);
    Info.Height := ScaleY(120);
    Info.Caption :=
      'هيتشال من الجهاز ده: السيرفر والكاشير والكول سنتر وبرامج الطباعة وخدماتها، والاختصارات والإعدادات.' + #13#10#13#10 +
      'قاعدة البيانات وSQL Server بيفضلوا، ولو ركّبت تاني كل البيانات بترجع.' + #13#10 +
      'علّم المربع لو عايز تمسحهم كمان: هتتعمل نسخة احتياطية أخيرة في مجلد النسخ الأول.' + #13#10 +
      'مجلد النسخ الاحتياطية عمره ما بيتمسح.';

    RemoveDataBox := TNewCheckBox.Create(Form);
    RemoveDataBox.Parent := Form;
    RemoveDataBox.Left := ScaleX(16);
    RemoveDataBox.Top := Info.Top + Info.Height + ScaleY(12);
    RemoveDataBox.Width := Form.ClientWidth - ScaleX(32);
    RemoveDataBox.Height := ScaleY(22);
    RemoveDataBox.Caption := 'امسح قاعدة البيانات وSQL Server كمان';
    RemoveDataBox.Checked := RemoveData;

    OkButton := TNewButton.Create(Form);
    OkButton.Parent := Form;
    OkButton.Caption := 'إزالة';
    OkButton.Width := ScaleX(90);
    OkButton.Height := ScaleY(28);
    OkButton.Left := Form.ClientWidth - ScaleX(16) - OkButton.Width * 2 - ScaleX(10);
    OkButton.Top := Form.ClientHeight - ScaleY(16) - OkButton.Height;
    OkButton.ModalResult := mrOk;

    CancelButton := TNewButton.Create(Form);
    CancelButton.Parent := Form;
    CancelButton.Caption := 'إلغاء';
    CancelButton.Width := OkButton.Width;
    CancelButton.Height := OkButton.Height;
    CancelButton.Left := Form.ClientWidth - ScaleX(16) - CancelButton.Width;
    CancelButton.Top := OkButton.Top;
    CancelButton.ModalResult := mrCancel;
    CancelButton.Cancel := True;
    CancelButton.Default := True;

    Result := Form.ShowModal = mrOk;
    RemoveData := Result and RemoveDataBox.Checked;
    if RemoveData then
    begin
      Result := MsgBox('قاعدة البيانات هتتمسح: كل الطلبات والورديات والمنيو والمستخدمين والعملاء والمخزون.' + #13#10 +
        'هتتعمل نسخة احتياطية أخيرة في مجلد النسخ الاحتياطية قبل المسح، ولو النسخة فشلت مفيش حاجة هتتمسح.' + #13#10 +
        'وبعدين SQL Server هيتشال.' + #13#10#13#10 + 'متأكد؟', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
      RemoveData := Result;
    end;
  finally
    Form.Free;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  ResultFile, Parameters: String;
  Lines: TArrayOfString;
begin
  if CurUninstallStep <> usUninstall then exit;
  ResultFile := AddBackslash(GetTempDir) + 'falah-uninstall-result.txt';
  DeleteFile(ResultFile);
  Parameters := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\Uninstall-Falah.ps1') +
    '" -KeepDir "' + ExpandConstant('{app}') + '" -ResultFile "' + ResultFile + '"';
  if RemoveData then Parameters := Parameters + ' -RemoveDatabase';
#ifdef DryRun
  Parameters := Parameters + ' -DryRun';
#endif
  UninstallProgressForm.StatusLabel.Caption := 'إزالة فالح... ما تقفلش الجهاز.';
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not LoadStringsFromFile(ResultFile, Lines) then SetArrayLength(Lines, 0);
  if UninstallSilent then exit;
  if GetArrayLength(Lines) >= 3 then
  begin
    if Lines[0] = 'OK' then
      MsgBox(Lines[1] + #13#10#13#10 + 'السجل: ' + Lines[2], mbInformation, MB_OK)
    else
      MsgBox(Lines[1] + #13#10#13#10 + 'السجل: ' + Lines[2], mbError, MB_OK);
  end
  else
    MsgBox('الإزالة ما كملتش (كود ' + IntToStr(ResultCode) + ').', mbError, MB_OK);
end;
