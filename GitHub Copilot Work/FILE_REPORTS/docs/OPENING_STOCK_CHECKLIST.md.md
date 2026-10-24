# docs/OPENING_STOCK_CHECKLIST.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `3ccb3f856d610c9ec3dba9c621062fe91cb00abef1669a53d7a519e7ff433063`; Git blob SHA256 `3ccb3f856d610c9ec3dba9c621062fe91cb00abef1669a53d7a519e7ff433063`. Read lines1–63.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Human opening-inventory/cost data workflow and historical completeness metrics.

## reasoning

All63lines read. Correctly distinguishes unknown stock cost from zero, asks for real count/invoice evidence and advises stopping sales while counting. It describes incoming adjustments and recorded reasons rather than fabricated inventory; current batch count-sheet source additionally has unresolved retry and off-filter draft risks not covered here. Claim that tools are fully complete cannot be used as release acceptance. Dated92menu/52items/36movements statistics are from2026-09-12 and are not current testDB facts. Inventory source applies signed directions; FIN013 dashboard omits sign, so document assertion that all warnings recalculate correctly needs qualification.

## relationships

- InventoryViewModel count-sheet versus single count flow
- InventoryQueryService signed balances
- OwnerDashboardQueryService FIN013
- ReceivePurchaseHandler cost/stock posting

## tests

No stock/cost/purchase updates or current business-data query executed. Historical counts not remeasured. Financial migration/race behavior remains independently documented.

## recommendation

Retain provenance/date of opening data, use safe retained request and stable count payloads, require paused-sales/clear operational policy and validate dashboard sign/cost warnings after later approved fixes.

## risks

Wrong opening data propagates stock/COGS; running suggested write workflow against unknown production DB would violate audit safety.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
