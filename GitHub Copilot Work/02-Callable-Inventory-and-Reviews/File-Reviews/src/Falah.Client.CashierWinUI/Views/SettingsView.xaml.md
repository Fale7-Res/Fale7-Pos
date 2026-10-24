# src/Falah.Client.CashierWinUI/Views/SettingsView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `55bab773af5909ad22f8e96e0b2e56df4c60b4002fdabdb76e909b07a783ee5c`; worktree SHA256 `bd092a0d6f5c32d7ab025ea9d81cc23cb74ba120ac542ea4e2a84f633d647d55`; LF-equivalent `True`.

Actual role: Restaurant settings editor and local theme preference surface.

## Function/resource review

- `SelectedTheme` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:43`: Theme combobox deliberately outside CanEdit gating, changes local preference immediately. It is nevertheless inside HasData container, so server settings failure can hide local theme control. Narrative mentions reference app; implementation context exposed in user-facing copy.

- `CanEdit` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:22`: Save button and server fields consistently bind CanEdit. Inverse banner states read-only even if CanEdit false because busy/unresolved, potentially misexplaining state as missingpermission; VM flag interpretation matters.

- `MenuUrl` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:85`: URL text LTR and Url input scope; fields have explicit headers, reasonable44px touch bounds. Validation occursVM/server, not keyboard scope.

- `DefaultOpeningCashText` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:108`: Four fixed equal star financial fields includeEGPunits and explicit server threshold note. At narrow width long headers must wrap/nativecheck; opening cash automationID exists.

- `HoldPreparationUntilPaymentConfirmed` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:129`: Actual printing setting checkbox plus explanatory effect. Turning it off lets kitchen start before electronic confirmation by design. Markup does not execute physical print on toggle; saving changes backend configuration is outside audit scope.

- `SaveError` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:137`: Save error/note wrap at bottom of potentially long ScrollViewer; header Save remains pinned. Operator may not see feedback until scrolling; no InfoBar/live-region declaration.

- `LoadError` at `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml:153`: HasData content separate spinner/error/retry ensures real load failures visible. Theme and configuration not modified by review.

## Tests and limitations

NOT_EXECUTED. Test permission/busy explanation, server unavailable local-theme accessibility, long financial headers, feedback visibility and retained save restart. XAML has no callable symbols.

## Recommendations (not implemented)

Separate local appearance availability and actual write-busy feedback from permission-only messaging. Provide accessible visible save result without user scrolling.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
