# src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `fb8cda0af18c3b5073826704d1b72243fd196e6ddc084070a7a7b1e139791105`; worktree SHA256 `fb8cda0af18c3b5073826704d1b72243fd196e6ddc084070a7a7b1e139791105`; LF-equivalent `True`.

Actual role: WPF PIN-only reference login.

## Function/resource review

- `PressDigit` at `src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:33`: Caps six chars, appends command parameter, clears error. Backspace/Clear simple; no busy guard on keypad.

- `SubmitAsync` at `src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:52`: Real HTTP login reads user summary, clears PIN and updates session/event. Fail result always invalid PIN, catches raw exception message. ApiClient does not install bearer token, so success screen transition alone cannot prove authorized subsequent data loads.

## Tests and limitations

No credential entry performed. Current client suite compiles WinUI Login classes, not this WPF.

## Recommendations (not implemented)

If maintained, use actual token contract and safe error classification; do not assume login screen transition means POS connectivity.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
