import sys
from substantive_review_engine import execute_review_batch

batch_files = [
    "src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs",
    "src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs",
    "src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs",
    "src/Falah.Infrastructure/Orders/RefundOrderHandler.cs",
    "src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs",
    "src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs",
    "src/Falah.Infrastructure/Treasury/SafeDropHandler.cs"
]

analyses = {
    "src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs": {
        "file_overview": "Atomic order checkout handler orchestrating authoritative pricing, approvals, stock deduction, order entity generation, and treasury movement journaling under database transactions. Hosts critical vulnerability FIN-001 (split payment journal key collision).",
        "Falah.Infrastructure.Orders.CheckoutOrderHandler.CheckoutOrderHandler(FalahDbContext, IApprovalRegistry, MenuPricingService, InventoryDeductionService, IAuditLog)": {
            "behavior": "Injects database context, approval registry, menu pricing service, inventory deduction service, and audit log.",
            "trust": "Internal DI container.",
            "mutations": "Assigns private fields. Zero state mutations.",
            "concurrency": "Thread-safe constructor.",
            "risks": "None."
        },
        "Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)": {
            "behavior": "Validates cashier user, opens business day, resolves prices and items, deducts inventory, posts sales movements to TreasuryMovements, and commits EfFinancialJournal with key 'sale-{request.CheckoutRequestId}'. In split tender (Cash + InstaPay), completes journal immediately for cash leg, leaving the same key claimed on server.",
            "trust": "Receives CheckoutOrderRequest from client till.",
            "mutations": "Inserts OrderEntity, OrderItemEntity, TreasuryMovementEntity, EfFinancialJournal record, updates stock balances, commits transaction.",
            "concurrency": "Guarded by business day checks and idempotency journal. Flaw on concurrent split payment completion.",
            "risks": "CRITICAL FLAW FIN-001: Claims unsuffixed saleKey 'sale-{CheckoutRequestId}' on checkout. When ConfirmElectronicPaymentHandler tries to post electronic split leg, it attempts to claim the same key, throwing ConfirmConflictException.",
            "finding": "FIN-001"
        },
        "Falah.Infrastructure.Orders.CheckoutOrderHandler.FindExistingAsync(string, CancellationToken)": {
            "behavior": "Idempotency lookup querying Orders table by CheckoutRequestId.",
            "trust": "Internal database lookup.",
            "mutations": "None.",
            "concurrency": "Thread-safe AsNoTracking read.",
            "risks": "None."
        }
    },
    "src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs": {
        "file_overview": "Electronic payment settlement handler confirming pending card/InstaPay/wallet tender legs after checkout. Reconciles electronic leg into TreasuryMovement and EfFinancialJournal.",
        "Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)": {
            "behavior": "Locks order row with UPDLOCK/ROWLOCK/HOLDLOCK. Verifies order is unpaid with pending electronic leg. Calls order.ConfirmElectronicPayment(). Constructs saleKey = 'sale-{orderEntity.CheckoutRequestId}' and calls journal.TryStart(saleKey, 'sale'). Throws ConfirmConflictException if key exists.",
            "trust": "Cashier confirmation request with reference transaction ID.",
            "mutations": "Updates OrderEntity.ElectronicPaymentStatus, OrderEntity.IsPaid, appends TreasuryMovementEntity, commits journal.",
            "concurrency": "UPDLOCK prevents race conditions on confirmation.",
            "risks": "CRITICAL FLAW FIN-001: Because CheckoutOrderHandler claimed 'sale-{CheckoutRequestId}', journal.TryStart returns false! The electronic leg can NEVER be confirmed.",
            "finding": "FIN-001"
        }
    },
    "src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs": {
        "file_overview": "Handles operator cancellation of a pending electronic payment before confirmation arrives.",
        "Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)": {
            "behavior": "Loads order with UPDLOCK/ROWLOCK/HOLDLOCK. Cancels electronic leg by transitioning Order to Cancelled and IsPaid to false. Does NOT reverse cash splits already deposited into drawer.",
            "trust": "Operator cancellation request.",
            "mutations": "Updates OrderEntity status to Cancelled. Zero treasury movement compensating cash leg.",
            "concurrency": "Guarded by row locks.",
            "risks": "CRITICAL FLAW FIN-002: In split payment (cash + InstaPay), collected cash remains in till while order is cancelled with zero sales revenue, causing untracked drawer overages.",
            "finding": "FIN-002"
        }
    },
    "src/Falah.Infrastructure/Orders/RefundOrderHandler.cs": {
        "file_overview": "Order refund processing handler executing debit reversals against cashier drawer under SQL Server UPDLOCK/ROWLOCK/HOLDLOCK.",
        "Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)": {
            "behavior": "Locks original order with FromSqlRaw UPDLOCK. Verifies order is not already refunded. Calls Order.ApplyRefund. Does NOT assert IsPaid == true before disbursing cash refund.",
            "trust": "Refund request submitted by cashier till.",
            "mutations": "Inserts OrderRefundEntity, TreasuryMovementEntity (Refund debit), updates OrderEntity.Status to Refunded.",
            "concurrency": "UPDLOCK prevents twin refunds on concurrent clicks.",
            "risks": "CRITICAL FLAW FIN-003: Permitting refunds on unpaid COD or pending orders drains physical cash from drawer for orders that were never paid.",
            "finding": "FIN-003"
        }
    },
    "src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs": {
        "file_overview": "Supplier purchasing payment settlement handler disbursing cash or bank transfers against supplier debt invoices.",
        "Falah.Infrastructure.Purchasing.PayPurchaseHandler.HandleAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken)": {
            "behavior": "Reads supplier invoice balance without UPDLOCK/serializable transaction. Verifies payment amount <= remaining balance. Deducts cash from drawer/treasury and commits payment.",
            "trust": "Operator supplier payment command.",
            "mutations": "Inserts PurchasePaymentEntity, TreasuryMovementEntity, updates PurchaseEntity.PaidAmountPiastres.",
            "concurrency": "Lack of row lock allows concurrent payments to race and overpay supplier invoices.",
            "risks": "CRITICAL FLAW FIN-005: Concurrency race condition allowing double payments beyond outstanding invoice balance.",
            "finding": "FIN-005"
        }
    },
    "src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs": {
        "file_overview": "Payroll run lifecycle state machine handler transitioning runs between Draft, Approved, Paid, and Cancelled.",
        "Falah.Infrastructure.Employees.TransitionPayrollHandler.HandleAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken)": {
            "behavior": "Loads PayrollRunEntity and updates Status to requested transition target. When target is Paid, marks run as paid without posting financial journal or employee ledger movements.",
            "trust": "Manager payroll command.",
            "mutations": "Mutates PayrollRunEntity.Status and AuditLog.",
            "concurrency": "Guarded by database transaction.",
            "risks": "CRITICAL FLAW FIN-006: Status reaches Paid with zero cash treasury movement or employee disbursement records.",
            "finding": "FIN-006"
        }
    },
    "src/Falah.Infrastructure/Treasury/SafeDropHandler.cs": {
        "file_overview": "Cash safe drop handler skimming cash from cashier drawer into central restaurant safe during active shifts.",
        "Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)": {
            "behavior": "Loads active shift with AsNoTracking. Opens transaction. Records TreasuryMovement with CashLifecycle direction and SafeDrop reason.",
            "trust": "Cashier safe drop command.",
            "mutations": "Inserts TreasuryMovementEntity, commits transaction.",
            "concurrency": "Shifts read with AsNoTracking allows safe drop to race shift closure.",
            "risks": "Attribution discrepancy if safe drop commits after shift close handover calculation.",
            "finding": "FIN-013"
        }
    }
}

if __name__ == '__main__':
    execute_review_batch(batch_files, "Financial Integrity Core (Batch 2)", "financial-auditor", analyses)
