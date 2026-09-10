<#
Enables (or removes) post-mortem crash dumps for the two Falah station executables ONLY.

Needed because the heap fault is a RtlFailFast: it never reaches the CLR, and attaching any live
debugger makes the fault disappear entirely - so WER's post-mortem capture is the only way to see
the process state at the moment it dies. Scoped per-executable, so no other program on the machine
is affected. Run with -Remove to undo.
#>
param(
    [string]$DumpFolder = 'E:\falah-restaurant\Falah-Restaurant-CSharp\artifacts\verification\native-crash-dumps',
    [switch]$Remove
)
$root = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps'
$apps = @('Falah.Client.CashierWinUI.exe', 'Falah.Client.CallCenterWinUI.exe')

foreach ($app in $apps) {
    $key = Join-Path $root $app
    if ($Remove) {
        if (Test-Path $key) { Remove-Item $key -Recurse -Force; Write-Output "removed: $app" }
        else { Write-Output "not present: $app" }
        continue
    }
    New-Item -Path $key -Force | Out-Null
    New-ItemProperty -Path $key -Name 'DumpFolder' -Value $DumpFolder -PropertyType ExpandString -Force | Out-Null
    New-ItemProperty -Path $key -Name 'DumpCount'  -Value 10 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $key -Name 'DumpType'   -Value 2  -PropertyType DWord -Force | Out-Null   # 2 = full
    Write-Output "enabled: $app -> $DumpFolder"
}
if ($Remove -and -not (Get-ChildItem $root -ErrorAction SilentlyContinue)) {
    Remove-Item $root -Force -ErrorAction SilentlyContinue
    Write-Output "removed empty LocalDumps key"
}
