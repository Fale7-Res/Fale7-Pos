# src/Falah.Client.CashierWinUI/Views/RefundView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `d751d1faac7696ed65c75fc81b30afb2af7d26a9df6cc44234e97915b6728936`

Worktree SHA-256: `95219575ca8178b1675fe703737bcb5f139f48aa57f2012f2b96f7e0981ad78e`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Amount/reason/approval refund overlay.

## Source paths and function/resource analysis

- `IsRefundOpen` at `src/Falah.Client.CashierWinUI/Views/RefundView.xaml:3`: Shows actual lookup order and refundable remaining. Inputs bind CanEditOperation; commands remain callable so immutable retry can resolve unknown operation.

- `SubmitRefundCommand` at `src/Falah.Client.CashierWinUI/Views/RefundView.xaml:13`: Danger-styled explicit confirmation; return uses VM CancelRefund safety. No direct physical printing; no counterfeit data.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Native modal focus/keyboard and server concurrency tests needed; no physical print tests.

## Recommendations (not implemented)

Add accessible operation status and prevent background modal interaction; persistence recommendation in Refund.cs.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
