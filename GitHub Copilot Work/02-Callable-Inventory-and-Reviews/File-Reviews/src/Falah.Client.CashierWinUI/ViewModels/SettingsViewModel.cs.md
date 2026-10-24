# src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `8781dfcd1df272ecb3032354fc07b8a999824a043591d0a49d1aeecddcea7f08`; worktree SHA256 `f06eb43ddc17ea1ec982f536c2a6d8080e655430c3894f78ee70550181a6e8cf`; LF-equivalent `True`.

Actual role: Partial restaurant settings load/save and local appearance initialization.

## Function/resource review

- `SettingsViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs:19`: Initializes local appearance partial immediately; undefined persisted AppTheme can cause First exception in companion, already noted ThemeService. _loaded gates save.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs:70`: Reads actual settings, fills all visible fields and print flags; preserves properties not in update DTO as null. No guard against IsSaving, so concurrent refresh can replace form while save in progress; actual XAML button disable state to inspect.

- `SaveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs:112`: Parses money with CashInput, integers nonnegative AutoLock and copies>=1. Does not check CanEdit internally though UI permission intended; backend must enforce. Builds partial request with current form, UpdateSettings response set_loaded and header actual identity. No retained unknown outcome/global busy gate: retry/new user can rebuild request; configuration idempotent value replacement semantics differ from duplicate money.

- `AutoLockMinutesText` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs:99`: Exposes persisted auto-lock setting, but neither root/MainViewModel reviewed implemented idle monitor; parent checking repository-wide use. Saving setting not proof feature operational.

- `HoldPreparationUntilPaymentConfirmed` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.cs:103`: Exposes real kitchen hold setting, warns different from owner desired unpaid pickup preparation stage. Changing could affect actual printed work; audit did not invoke save.

## Tests and limitations

Settings appearance excluded headless tests. Need per-field partial-null roundtrip, unauthorized save, invalid integer Arabic counts and ambiguous update UI tests; no settings changes made.

## Recommendations (not implemented)

Clearly distinguish local theme vs server config save; validate actual auto-lock implementation and preserve safe concurrency/authoritative permission.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
