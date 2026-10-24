# src/Falah.Client.CashierWinUI/Views/BackupView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `a69c02d8ab4f642832d5f2355175fd79ab15b3f4d9c0c046511de5ab5deedf7e`; worktree SHA256 `c6093c396f78cdb2533a94d86a2f2bb60af5d110a34166bddb7e369612357486`; LF-equivalent `True`.

Actual role: Backup history, verification and guarded destructive restore UI.

## Function/resource review

- `CanManage` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:26`: Create action permission/busy gate; refresh icon lacks explicit accessible name. Last backup headline/detail plus restore history stay outside table loading overlays, requiring VM coherence during refresh.

- `Grid.Row="2"` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:50`: Banner explicitly says restore unavailable from this screen, yet each row has OpenRestoreCommand and full restore overlay below. This is stale contradictory user-facing safety guidance, not evidence restore absent.

- `FileNameConverter` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:91`: Filename displayed, full path tooltip and requester ID. Verification/status labels use actual general converter Valid/Invalid mapping. Verify/restore row commands rely VM permission and pending gates.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:142`: Success toast literalwhite on TextPrimaryBrush repeats dark theme conditional low contrast; errorwhite on DangerBrush a separate pair. Both bottom overlays could compete if VM retains both flags.

- `IsRestoreOpen` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:154`: Last declared overlay covers all4rows. Explicit irreversible data-loss warning, file path, mandatory reason, literal phrase and conditional managerPIN; label/CanSubmitRestore consistent across fields and button. No restore executed.

- `MaxWidth="620"` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:155`: Centered restore card has ScrollViewer but no MaxHeight/stretch constraint, unlike attendance bounded dialog. Available parent measure might constrain; native viewport check required, not asserted clipping.

- `RestoreManagerPin` at `src/Falah.Client.CashierWinUI/Views/BackupView.xaml:176`: PasswordBox max6 and permission visibility; no actual PIN inspected. Startup/service/global operation protections are backend/VM claims not independently guaranteed by warning text. UI014 Completed/Succeeded mismatch persists in reviewed VM.

## Tests and limitations

NOT_EXECUTED. No backup, restore, database or configuration mutation. Need unknown restore recovery, literal phrase, native focus, dark toast and misleading banner tests. XAML no callable declarations.

## Recommendations (not implemented)

Correct safety copy in approved future change and verify end-to-end restore status vocabulary without production DB; pair semantic toast colors.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
