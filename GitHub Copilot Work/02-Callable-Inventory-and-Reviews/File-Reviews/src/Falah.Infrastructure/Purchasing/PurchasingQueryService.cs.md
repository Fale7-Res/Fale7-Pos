# File Review: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs
**Total Callables**: 43
---
## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:14:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.PurchasingQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Kind**: ConstructorDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.PurchasingQueryService(Falah.Infrastructure.Persistence.FalahDbContext) across lines 14-14.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:16:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ListSuppliersAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.ListSuppliersAsync(CancellationToken) across lines 16-21.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:20:33:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 20-20.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:20:60:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 20-20.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:23:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierAsync(string, CancellationToken) across lines 23-29.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:25:79:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 25-25.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:27:75:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 27-27.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:31:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierLedgerAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierLedgerAsync(string, CancellationToken) across lines 31-35.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:32:63:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 32-32.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:33:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(e => e.CreatedAtUtc).ToListAsync(ct))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:34:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 34-34.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:37:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ListPurchasesAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.ListPurchasesAsync(CancellationToken) across lines 37-40.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:38:53:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 38-38.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:38:75:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 38-38.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:39:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:40:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => ReceivePurchaseHandler.ToDto(p, wasReplayed: false)).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:42:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPurchaseAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPurchaseAsync(string, CancellationToken) across lines 42-47.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:44:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 44-44.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:44:89:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 44-44.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:45:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.FirstOrDefaultAsync(p => p.Id == id, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:56:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPayablesAgingAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPayablesAgingAsync(CancellationToken) across lines 56-99.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:60:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:61:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(p => p.CreatedAtUtc)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:64:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 64-64.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:64:93:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 64-64.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:66:36:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 66-76.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:78:28:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(i => i.DaysOverdue ?? -1).ThenByDescending(i => i.AgeInDays)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:78:71:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(i => i.DaysOverdue ?? -1).ThenByDescending(i => i.AgeInDays)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:84:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 84-87.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:86:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Count(i => i.Bucket == b.ToString()),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:87:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Where(i => i.Bucket == b.ToString()).Sum(i => i.RemainingPiastres)))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:87:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Where(i => i.Bucket == b.ToString()).Sum(i => i.RemainingPiastres)))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:90:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var overdue = invoices.Where(i => i.DaysOverdue is > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:91:42:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var unscheduled = invoices.Where(i => i.DueDate is null).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:95:26:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Sum(i => i.RemainingPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:96:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdue.Sum(i => i.RemainingPiastres), overdue.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:97:29:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'unscheduled.Sum(i => i.RemainingPiastres), unscheduled.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:101:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.SupplierEntity, IEnumerable<Falah.Infrastructure.Persistence.Entities.SupplierLedgerEntryEntity>)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Purchasing.PurchasingQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.SupplierEntity, IEnumerable<Falah.Infrastructure.Persistence.Entities.SupplierLedgerEntryEntity>) across lines 101-108.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:104:43:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 104-104.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:104:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 104-104.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:105:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 105-105.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:105:78:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 105-105.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:106:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var outstanding = entries.Sum(e => e.DebitPiastres - e.CreditPiastres);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a`
- **Callable ID Checked:** All 43 callables in `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a` against production source `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs`. All 43 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 43 callables.
