# Independent challenge: checkout admitted before completed shift close

**Survives source challenge; HIGH candidate. No canonical ID assigned. No runtime write reproduced.**

Checkout reads Open outside its transaction, then awaits pricing/tax before transaction starts. A different authorized request can close and hand over that shift completely in the gap. The original checkout then creates order/stock/cash movement without rereading or updating Shift. Its FK requires parent existence, not Open status; the Shift Version token only protects Shift updates that checkout never performs.

The schedule closes completely before the checkout transaction begins, so the argument does not depend on uncertain overlapping SQL lock behavior. A cash sale posts to the drawer after its prior handover while stored close expected/actual remain unchanged. Fresh requests first reading after closure are correctly rejected; completed same-key replay is a separate legitimate path.

[Exact source hashes,19 references, stable symbols, timeline, counterevidence and test plan](checkout-after-close.json). FIN004 refund attribution and FIN011 treasury locking are separate findings. No SQL, API, login, business data or application code operation was executed or modified.
