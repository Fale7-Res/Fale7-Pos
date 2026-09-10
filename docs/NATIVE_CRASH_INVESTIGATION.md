# Native heap crash (0xC0000374) — investigation and fix

**Status: fixed.** Root cause was a defect in Windows App SDK 1.5, not in Falah code.
Both WinUI clients now reference `Microsoft.WindowsAppSDK` **1.7.260224002**.

## Symptom

Both station executables died intermittently with `STATUS_HEAP_CORRUPTION` (`0xC0000374`) in
`ntdll.dll`, at two distinct moments: during startup before any window appeared, and during graceful
shutdown. Seven Application-Error events shared the identical fault offset `0x00000000000ff489`.
Nothing was logged by the app, and no managed exception was ever raised.

## Why the first hypotheses were wrong

The crash looked like an application-lifetime bug, so the obvious suspects were tested first. All
were disproved by measurement, not by reading code:

| Hypothesis | Test | Result |
| --- | --- | --- |
| Startup ordering (window maximised before the visual tree was attached) | rebuilt with the pre-fix ordering restored | 12/12 clean — not the cause |
| Async health check writing to a bound property after teardown | rebuilt with the `_stopped` guard and timer stops removed | 12/12 clean — not the cause |
| Mismatched Windows App SDK runtime files | byte-compared the deployed native DLLs between builds | identical — not the cause |
| A marshalling bug in our own interop | searched the whole solution | there is no `DllImport` anywhere — not possible |
| Windows App SDK 1.5 servicing defect | overlaid 1.5.250108004 runtime files onto the reproducing build | 7/12 still crashing — the 1.5 branch never got the fix |

## The measurement that settled it

A 30-line WinUI application — no Falah code, no styles, no view models, no timers, no HTTP — built
with the same csproj settings and the same SDK version, crashed **8/12** with the identical
`0xC0000374`. The same executable, byte for byte, was **10/10 clean** when run from a different
folder.

Conversely, a build that had never once crashed in 56 runs crashed **10/10** as a byte-identical
copy in a fresh directory (`diff -rq` confirmed 308 identical files).

Since an empty application reproduces the fault and identical bytes flip behaviour purely on
placement, no line of Falah code can be responsible. The fault is a timing-sensitive race inside the
SDK's own XAML initialisation and teardown; file placement only perturbs load timing enough to
decide whether the race bites.

## Corroborating evidence from the crash dump

WER post-mortem capture was required, because attaching any live debugger made the fault vanish
entirely (15/15 clean under ProcDump) and .NET's own dump facility never saw it — `RtlFailFast`
terminates the process without passing through the CLR.

The startup-phase dump put the faulting thread here:

```
Microsoft.UI.Xaml.Controls.XamlControlsResources..ctor()
  Microsoft.UI.Xaml.ResourceDictionary..ctor(WinRT.DerivedComposed)
    System.Collections.Generic.Dictionary`2..ctor()      <- corruption detected here
  XamlTypeInfoProvider.Activate_0_XamlControlsResources()
  Microsoft.UI.Xaml.Application.Start(...)
```

That is the framework's own first resource dictionary, constructed before a single line of
application code has run — at that moment `Falah.Domain.dll` and `Falah.Application.dll` are not
even loaded. (Confirmed by a control experiment: swapping those two assemblies for different builds
left the crash rate unchanged.)

## The fix

`Microsoft.WindowsAppSDK` 1.5.240311000 -> **1.7.260224002** in both client projects. No source
changes were needed; the solution builds unchanged.

Verification, all in the directory shape that previously crashed:

| Build | SDK 1.5 | SDK 1.7 |
| --- | --- | --- |
| Empty control app | 8/12 crash | **14/14 clean** |
| Cashier (build output) | — | **14/14 clean** |
| Cashier (byte-identical copy) | 10/10 crash | **14/14 clean** |
| Call Center | — | **12/12 clean** |

## Tooling left behind

- `tools/repro-native-crash.ps1` — the reproduction harness. Launches an executable repeatedly,
  samples the Application log per phase, and classifies each run by process exit code, so a genuine
  fault (`0xC0000374`) is never confused with a clean early exit. Use it as a regression gate
  whenever the SDK version or startup path changes.
- `tools/capture-native-crash-dump.ps1` — ProcDump-based capture that keeps only faulting runs.
  Note that a live debugger masks this class of fault; post-mortem capture is what worked.
- `tools/wer-localdumps.ps1` — enables/removes scoped WER post-mortem dumps for the two station
  executables only. **The registry change was removed after the investigation**; re-run it (elevated)
  only if a native fault needs capturing again, and remove it afterwards with `-Remove`.

## Outstanding cleanup

`src/Falah.Client.*/bin/` still holds ~27 build output directories from earlier sessions, all
produced against SDK 1.5. Every one of them still reproduces the crash if launched. They are
regenerable build artifacts and should be deleted so nobody runs a stale binary by mistake.
