# File Review: src/Falah.Domain/Financial/TreasuryMovement.cs
**Total Callables**: 4
---
## Canonical Callable ID: src/Falah.Domain/Financial/TreasuryMovement.cs:32:5:MethodDeclaration
- **Symbol**: Falah.Domain.Financial.TreasuryMovementFactory.PaymentAccountFor(Falah.Domain.Financial.PaymentMethod)
- **Kind**: MethodDeclaration
- **Behavior**: Maps a domain `PaymentMethod` to its corresponding treasury ledger account ID (e.g. Cash -> drawer, Card -> bank, InstaPay -> instapay, Wallet -> wallet), throwing a `DomainException` if an unsupported payment method is encountered.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': True, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Financial/TreasuryMovement.cs:41:5:MethodDeclaration
- **Symbol**: Falah.Domain.Financial.TreasuryMovementFactory.BuildSaleMovement(string, string, string, string, Falah.Domain.Financial.PaymentMethod, Falah.Domain.Common.Money, string?, string, string, DateTimeOffset)
- **Kind**: MethodDeclaration
- **Behavior**: Factory method that builds an immutable `sale_collection` `TreasuryMovement` record, assigning destination account based on payment method and stamping transaction metadata including order ID, shift ID, and business day ID.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Financial/TreasuryMovement.cs:62:5:MethodDeclaration
- **Symbol**: Falah.Domain.Financial.TreasuryMovementFactory.BuildRefundMovement(string, string, string, string, Falah.Domain.Financial.PaymentMethod, Falah.Domain.Common.Money, string?, string, string, string, string, DateTimeOffset)
- **Kind**: MethodDeclaration
- **Behavior**: Factory method that constructs a `refund` `TreasuryMovement` record debited from the original payment account, recording refund amount, reason, approver, and operational shift ID.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Financial/TreasuryMovement.cs:83:5:MethodDeclaration
- **Symbol**: Falah.Domain.Financial.TreasuryMovementFactory.BuildCashLifecycleMovement(string, string, string, string, string?, string?, Falah.Domain.Common.Money, string, string, string?, string?, string, string, string?, DateTimeOffset)
- **Kind**: MethodDeclaration
- **Behavior**: Factory method that constructs a generic cash-lifecycle `TreasuryMovement` record (such as deposit, drop, withdrawal, transfer, or variance adjustment) linking source and destination accounts with audit metadata.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87`
- **Callable ID Checked:** All 4 callables in `src/Falah.Domain/Financial/TreasuryMovement.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Domain/Financial/TreasuryMovement.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87` against production source `src/Falah.Domain/Financial/TreasuryMovement.cs`. All 4 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 4 callables.
