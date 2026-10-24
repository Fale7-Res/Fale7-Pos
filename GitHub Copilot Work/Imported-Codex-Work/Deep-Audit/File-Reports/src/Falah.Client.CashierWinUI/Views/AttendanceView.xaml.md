# src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `5b4ee88ddfff2cf2cdaa7daf8f5b9c379a46a7c56a800be361989831145742ae`; worktree SHA256 `8edc260f015677a3adbf7ebcc0d692e1333624f61c88b5843c61aea8d05b3594`; LF-equivalent `True`.

Actual role: Dated attendance read and authorized correction overlay.

## Function/resource review

- `CanBrowse` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:27`: Previous/next actual enabled gate and accessible day names. Table lists employee, times, hours, late minutes, textual attendance status and manual correction marker; no invented desktop check-in action.

- `OpenCorrectionCommand` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:102`: Row correction command binds root and record; button not permission-hidden, so VM gate must explain refusal. Fixed110x4+120 columns leave employee star narrow; vertical-only table scroll cannot expand horizontally.

- `AttendanceCorrectionCard` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:140`: Correction overlay max640x730 stretched with24margin, header/scroll/footer named for isolated geometry probe. Four numeric fields/status/reason disabledCanEditCorrection; save command uses retry-aware caption.

- `CorrectionRecord.Date` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:152`: Employee/date context displayed, warning says correction can affect wage ledger. No biometric device behavior or pay difference validated from markup.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:134`: Toast backgroundTextPrimaryBrush with literalwhite foreground: dark theme primary#E6E9EE can create very low contrast, same root semantic-pair misuse as UI002. No rendered screenshot. Positioned before modal so modal overlays it.

- `HasNoRecords` at `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml:125`: Read loading/error/no-record states distinct, empty text attributes attendance to biometric feed. It does not prove current device integration working.

## Tests and limitations

NOT_EXECUTED. Existing optional probe measures this modal but was not executed by reviewer. Need permission refusal, missing fingerprint, retained restart, wage impacts in isolated fixture and dark toast contrast. No callable XAML symbols.

## Recommendations (not implemented)

Use paired toast foreground/background tokens, keep correction financial context, verify focus and geometry natively.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
