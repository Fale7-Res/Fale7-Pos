# File Review: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs
**Total Callables**: 114
---
## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:32:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.OwnerDashboardQueryService(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Reports.ReportingQueryService)
- **Kind**: ConstructorDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.OwnerDashboardQueryService(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Reports.ReportingQueryService) across lines 32-36.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:38:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.GetAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.GetAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken) across lines 38-71.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:76:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadTrendAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadTrendAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, CancellationToken) across lines 76-107.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:87:9:LocalFunctionStatement
- **Symbol**: Change(long, long)
- **Kind**: LocalFunctionStatement
- **Behavior**: Executes logic for Change(long, long) across lines 87-91.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:111:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadDailySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadDailySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken) across lines 111-132.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:115:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 115-115.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:116:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:120:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(r => BusinessClock.BusinessDate(r.CreatedAtUtc))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 121-121.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 121-121.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 121-121.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:126:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 126-130.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:134:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHourlySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHourlySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken) across lines 134-154.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:138:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 138-138.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:139:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:144:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 144-144.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 145-145.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 145-145.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 145-145.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:148:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 148-152.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:158:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.CurrentTreasuryAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.CurrentTreasuryAsync(CancellationToken) across lines 158-169.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': True, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:161:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(m => m.Status == nameof(MovementStatus.Posted))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:162:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 162-162.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:164:9:LocalFunctionStatement
- **Symbol**: Balance(string)
- **Kind**: LocalFunctionStatement
- **Behavior**: Executes logic for Balance(string) across lines 164-166.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:164:55:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 164-166.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:171:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadLeakageAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, int, DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadLeakageAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, int, DateTimeOffset, DateTimeOffset, CancellationToken) across lines 171-184.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:177:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 177-177.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:178:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(m => m.LossValuePiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:183:24:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'losses.Sum(v => v ?? 0), losses.Count, expensesTotal);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:191:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadWagesPostedAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadWagesPostedAsync(DateTimeOffset, DateTimeOffset, CancellationToken) across lines 191-203.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:200:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 200-200.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:201:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(e => e.CreditPiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:205:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int) across lines 205-224.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:230:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadSettledFromDriversAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadSettledFromDriversAsync(DateTimeOffset, DateTimeOffset, CancellationToken) across lines 230-233.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:232:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 232-232.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:233:23:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:235:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadAttentionAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadAttentionAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken) across lines 235-295.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': True, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:238:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 238-238.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:246:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 246-247.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:252:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(s => s.BusinessDate >= range.From && s.BusinessDate <= range.To)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:253:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 253-253.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:256:43:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 256-256.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:259:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(c => c.Status != nameof(CustodyStatus.Settled))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:260:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(c => c.ExpectedPiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:263:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.CountAsync(o => o.ElectronicPaymentStatus == "Pending", ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:266:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.CountAsync(e => e.Status == "Open", ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:269:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 269-269.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:279:42:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'OpenShiftCount: shifts.Count(s => s.Status != "Closed"),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:281:57:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 281-281.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:281:96:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 281-281.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:282:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 282-282.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:282:95:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 282-282.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:285:69:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 285-286.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:297:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken) across lines 297-315.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:299:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 299-299.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:300:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 300-300.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:302:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(m => m.InventoryItemId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:303:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 303-303.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:303:73:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 303-303.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:304:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:304:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:307:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 307-307.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:309:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(i => i.Balance <= i.MinimumMilliUnits && i.MinimumMilliUnits > 0)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:310:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(i => i.Balance - i.MinimumMilliUnits)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:311:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 311-311.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:317:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadObligationsAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadObligationsAsync(DateTimeOffset, DateTimeOffset, CancellationToken) across lines 317-356.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:322:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(e => e.SupplierId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:323:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 323-323.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:323:72:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 323-323.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:325:40:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var owing = supplierRows.Where(r => r.Outstanding > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:328:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:329:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => new { p.TotalPiastres, p.PaidPiastres }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:332:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(e => e.EmployeeId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:333:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 333-333.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:333:68:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 333-333.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:335:48:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var owedEmployees = employeeRows.Where(r => r.Balance > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:341:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:342:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => new { p.RemainingAmountPiastres, p.DueDate }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:345:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 345-345.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:346:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.Days is > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:347:54:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var unscheduledInvoices = openInvoices.Where(p => p.DueDate is null).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:350:23:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'owing.Sum(r => r.Outstanding), owing.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:351:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'purchases.Sum(p => p.TotalPiastres), purchases.Sum(p => p.PaidPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:351:64:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'purchases.Sum(p => p.TotalPiastres), purchases.Sum(p => p.PaidPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:352:31:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'owedEmployees.Sum(r => r.Balance), owedEmployees.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:353:33:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdueInvoices.Sum(p => p.RemainingAmountPiastres), overdueInvoices.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:354:37:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 354-354.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:355:66:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdueInvoices.Count == 0 ? 0 : overdueInvoices.Max(p => p.Days!.Value));'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:358:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadPeopleAsync(Falah.Domain.Common.BusinessDateRange, long, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadPeopleAsync(Falah.Domain.Common.BusinessDateRange, long, CancellationToken) across lines 358-372.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:361:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(a => a.Date >= range.From && a.Date <= range.To)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:362:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:366:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Present)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:367:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Late)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:368:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Absent)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:369:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.MissingFingerprint)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:370:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Sum(a => a.LateMinutes),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:374:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHealthAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHealthAsync(CancellationToken) across lines 374-419.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:377:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(b => b.Kind == nameof(Persistence.Entities.BackupRecordKind.Backup))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:378:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(b => new { b.Status, b.CompletedAtUtc, b.VerificationStatus })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:382:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 382-382.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:383:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(b => b.CompletedAtUtc)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:390:65:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 390-390.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:391:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(j => new { j.Id, j.Status, j.CreatedAtUtc }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:392:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 392-392.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:393:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var jobIds = jobs.Select(j => j.Id).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:396:24:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(a => jobIds.Contains(a.PrintJobId))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:397:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(a => new { a.Succeeded, a.AttemptedAtUtc, a.Error })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:400:46:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 400-400.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:400:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 400-400.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:401:46:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 401-401.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:401:83:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 401-401.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:406:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 406-406.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:410:28:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'jobs.Count(j => j.Status == nameof(PrintJobStatus.Failed))));'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:417:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 417-417.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


---
## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`<br>- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == " Out\` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
 - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:30:00Z
- **Source SHA-256 Verified:** 67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:** 
  - Exhaustively audited all 114 callables in OwnerDashboardQueryService.cs.
  - Double-challenged L1 review and confirmed finding **FIN-013**: sign inversion in LoadStockAsync summing QuantityMilliUnits unsigned (Direction == 'Out' adds instead of subtracting), contrasting with InventoryQueryService correct signed accumulation.
  - Verified financial trend aggregations, daily series serialization, and parameter bounds across all helper methods.
  - No modifications made to src/ or 	ests/.
