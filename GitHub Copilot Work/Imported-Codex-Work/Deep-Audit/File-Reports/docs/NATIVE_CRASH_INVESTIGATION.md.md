# docs/NATIVE_CRASH_INVESTIGATION.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `1aaabfc274b9a585e1efa7447ad9e3d0079d675584615b384d3b20aad1046d55`; Git blob SHA256 `1aaabfc274b9a585e1efa7447ad9e3d0079d675584615b384d3b20aad1046d55`. Read lines1–92.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Historical WindowsAppSDK heap-crash experiments, SDK upgrade narrative and dump-tool instructions.

## reasoning

All92lines read. Current client project references match SDK1.7.260224002; historical crash rates/dumps and causal attribution have not been reproduced. Folder-dependent empty-app/control evidence is suggestive, not proof every later crash is impossible. Blanket no DllImport anywhere claim is false for current source: WindowsSpoolerTicketTransport contains winspool imports, so exclusion of application interop must remain scoped to the old client experiment. Registry/WER dump instructions change system state and dump files may expose secrets, so none were executed. Advice to delete27old bin folders is historical cleanup guidance and is not authorization to delete current user artifacts.

## relationships

- WinUI client csproj SDK versions
- WindowsSpoolerTicketTransport native interop
- tools/repro-native-crash.ps1
- tools/capture-native-crash-dump.ps1
- tools/wer-localdumps.ps1

## tests

No crash loop, debugger attach, WER/registry change, DLL replacement or binary deletion executed. Native test matrix remains hardware/permission-dependent.

## recommendation

Keep historical experiment raw provenance separate from present release assertions, add current-version regression plan and scope interop exclusions precisely. Capture dumps only with separate approval and secret handling.

## risks

Diagnostic scripts can launch many apps, change dump registry or preserve sensitive process memory; historical clean runs are not a current no-crash guarantee.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
