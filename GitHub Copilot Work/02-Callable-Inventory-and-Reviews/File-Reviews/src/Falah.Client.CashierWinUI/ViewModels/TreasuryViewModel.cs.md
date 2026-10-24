# src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `630f3437d0e0517fe71a2343afe3ac1e7d04e1b42c98df4b433e7cf6eaa4dd32`

Worktree SHA-256: `7e6c0ca570caeb79efabacf0a0b381ac5e7e3a3a364fea390c7155c11d909158`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Owner treasury balance/position/ledger and deposit, withdrawal, transfer commands.

## Source paths and function/resource analysis

- `MovementRow` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:27`: Presents stored movement type, date, actor/approver and document reference; replaces raw order ID only in display reason, does not rewrite historic financial data. Unknown labels remain visible.

- `ApplyMovementFilter` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:129`: Client filters up to 200 loaded movements by account and text; history beyond 200 is not searchable from this page. Distinguishes no ledger data from no matching filtered data.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:183`: Sequential real balances, outside-account position, and last200 movements. Error in position can coexist with valid balances; Position null displays dash rather than zero. Uses caller actor ID, server authorization separate.

- `OpenTreasuryAction` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:280`: Opens only known command type when editable, creates one request ID and resets fields; valid funding account lists constrain normal UI.

- `SubmitTreasuryActionAsync` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:305`: Positive amount, mandatory reason, distinct transfer account check; obtains server approval if needed. Captures immutable typed request in delegate _pendingCommit, not durable DTO; process restart loses retry identity UI-001.

- `SendPendingAsync` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:311`: Retains delegate after ambiguity and later rejection; first definite rejection clears; success closes action and reloads. Session pending gate refreshed. No disk journal for any of three money commands.

- `ResolveApprovalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:336`: Single server PIN approval issue; request actor snapshot taken after await from mutable session. Busy gate should prevent normal lock/navigation while resolving.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

ManagementWriteSafetyTests relevant for same-session retention, not yet fully read here. Missing deposit/withdraw/transfer reconstructed-process durability, ledger pagination and read error tests.

## Recommendations (not implemented)

Persist typed operation before send; implement reachable recovery; add complete ledger query paging/filtering for reconciliation.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
