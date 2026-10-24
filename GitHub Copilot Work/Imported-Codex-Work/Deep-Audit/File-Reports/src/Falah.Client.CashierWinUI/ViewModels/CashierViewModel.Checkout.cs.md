# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `35b105d46b2f493c0b9e2c2fe8da13b2b3993356cf0db799976758294237230e`

Worktree SHA-256: `066c11322cca65d746b3c082a7e58573ac8430f5431b21b359f6d5b192224e5a`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Payment form, checkout command, electronic payment resolution.

## Source paths and function/resource analysis

- `DiscountAmountPiastres` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:25`: Discount values parse through CashInput; positive discounts require orders_discount before transmission. Local totals are estimates; request sends discount, item identities, quantities, offer identities, not client unit prices.

- `CompAmountPiastres` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:54`: Comp is always manager-approved even for privileged users; ResolveManagerApprovalIfNeededAsync sends PIN to the existing approval endpoint. Reason text is required when enabled; reason taxonomy exposes actual CLR record properties suitable for reflection binding.

- `ConfirmElectronicPaymentAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:122`: A durable request can be recovered without LastCheckoutResult, but this command and CanConfirmPendingPayment require that transient result. Source-confirmed recovery dead end UI-005. Success updates paid/electronic status from server.

- `CancelPendingElectronicPaymentAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:147`: Same recovery dead end; cancellation requires reason for a fresh attempt and retains the exact cancellation DTO after ambiguity. Durable owner keys lack per-user suffixes.

- `OpenCheckout` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:209`: Rejects empty/unloaded menu and missing delivery/pickup fields; sets default cash tender and one request ID. It does not persist a preparation order. All order types proceed toward the same checkout UI, conflicting with requested pickup Stage A (UI-003).

- `PressAmountKey` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:264`: Tender guard prevents editing cash-on-delivery and pending commands. Replacement-on-first-key works through OnAmountPaidTextChanged resetting the flag; decimal/amount validity is deferred to CashInput at submission.

- `SubmitPaymentAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:290`: Synchronous busy guard plus generated CanExecute limit double-click concurrency. Validation and manager approval precede immutable request construction; payment split, wallet and customer/address/pickup values are transmitted. CustomerPrintRoute Receipt delegates automatic ticket creation to the server; no kitchen-before-payment call occurs.

- `CompleteCheckoutAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:300`: Freezes exact DTO in a private field and reuses it during same-process retries. Clears only on success or definite first rejection; retains after ambiguous history followed by rejection. This field is never persisted, producing UI-001 across termination. Success clears cart and flags, but does not clear PickupDate/Time (UI-006).

- `StartNewOrder` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:394`: Resets result/error and shared delivery details only; no durable checkout recovery or pickup schedule reset.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

CashierCorrectnessTests/FinalVerificationTests are candidate same-session coverage; RetainedRequestDurabilityTests test a helper this checkout does not use. No process reconstruction assertion for this concrete checkout or electronic-recovery UI found in reviewed tests.

## Recommendations (not implemented)

Adopt durable operator-scoped checkout state before send; recover electronic actions from retained DTO order identity independently of transient result; design pickup preparation/pay stages explicitly; clear all order-scoped pickup values only after resolved order completion.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
