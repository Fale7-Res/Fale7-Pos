# src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `70dc69c12cf0df77ee7ddb3ac1b9ab151649d8fde1f111326435f6ce6c1b1e48`; worktree SHA256 `70dc69c12cf0df77ee7ddb3ac1b9ab151649d8fde1f111326435f6ce6c1b1e48`; LF-equivalent `True`.

Actual role: CallCenter PIN login commands and masking.

## Function/resource review

- `PressDigit` at `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:35`: Caps PIN length6, appends command string, clears error; no IsBusy guard so keypad can alter visual PIN during await though actual sent string already captured. Backspace/Clear likewise alter displayed string.

- `CanSubmit` at `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:51`: Requires minimum4 and !busy with generated CanExecute updates.

- `SubmitAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:54`: Real API login sets CurrentUser on success, clears PIN then LoggedIn event. ApiResult offline/5xx/403 converted indiscriminately into invalid PIN; exception branch shows raw ex.Message. Async void event continuation errors not caught after await.

## Tests and limitations

No credentials entered by reviewer. Missing rejected network vs credential UI message and keypad busy-lock tests.

## Recommendations (not implemented)

Safe actionable error classification, freeze keypad while busy, clear session/token coherently on lock; preserve actual server identity.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
