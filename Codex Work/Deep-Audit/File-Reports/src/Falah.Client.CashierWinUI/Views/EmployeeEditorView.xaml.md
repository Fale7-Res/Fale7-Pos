# src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3213675e213c8e4db32610c6ad4f20bcd54278f7ab33030867e13b191a222bbc`; worktree SHA256 `c210c522ba356a0473873c57eb968b237b785ae4fcf9460bac17d8219bc0bbe0`; LF-equivalent `True`.

Actual role: Employee roster editor for identity, compensation, lifecycle and delivery eligibility.

## Function/resource review

- `SelectCommand` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:53`: Root explicit nested selection command,330px roster plus remaining editor; rows have name/number/job. No search filter in this view; large roster navigation requires manual scrolling.

- `HasForm` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:72`: Form or empty instruction exclusive. Middle editor ScrollViewer and pinned save/footer support many fields; header/footer reduce available middle area with long errors.

- `CompensationType` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:108`: All wage types remain visible, optional salary/overtime distinct from required daily/shift values. Numeric InputScope alone does not validate; all server fields disabled CanEdit.

- `LifecycleStatus` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:149`: Work arrangement, lifecycle, active and can-deliver independent two-way controls. Potential contradictory combinations must be validated by VM/domain, not hidden in UI.

- `NationalId` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:169`: National ID and biometric ID treated ordinary editable text; no real records inspected. UI authorization inherited CanEdit only, permission enforcement independently backend-reviewed.

- `SaveLabel` at `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml:205`: Retry-aware caption and CanEdit gating; errors/success wrap. No loading/error empty state over roster in this markup, so initial read failure presentation depends ActionError/VM; native not observed.

## Tests and limitations

NOT_EXECUTED. Test retained create/update restart, all compensation values roundtrip, long field headers/narrow widths, roster read failure and native focus trap. No callable declarations.

## Recommendations (not implemented)

Keep compensation fields independent and validate lifecycle coherence server-side; expose roster load state and accessible action feedback.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
