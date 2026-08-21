import type { Purchase, StockMovement, SupplierLedgerEntry, TreasuryMovement } from './enterprise-types';
import type { Order } from './types';
import type { EmployeeLedgerEntry } from './types';
export {
  allocateNextOrderSequence,
  appendMovementIdempotently,
  assertOrderCanBeCancelled,
  buildRefundMovement,
  buildSaleMovement,
  buildCashLifecycleMovement,
  calculateExpectedDrawerCashPiastres,
  clampRefundAmount,
  createOperationId,
  fromPiastres,
  paymentAccountFor,
  refundableRemaining,
  resolveRefundOperationalShift,
  toPiastres,
  treasuryBalanceFromMovements,
  discrepancyNeedsApproval,
  assertProtectedSourceBalance,
  assertPurchasePayment,
  purchaseLineTotal,
  purchasePaymentAccountFor,
  supplierOutstandingFromLedger,
} from './financial-domain.mjs';
import { createOperationId } from './financial-domain.mjs';
import { FINANCIAL_JOURNAL_KEY, readFinancialJournal, startJournal, completeJournal } from './financial-journal.mjs';

export { FINANCIAL_JOURNAL_KEY };

export interface FinancialJournalEntry {
  operationId: string;
  idempotencyKey: string;
  kind: 'sale' | 'refund' | 'shift_open' | 'cash_in' | 'cash_out' | 'expense' | 'reconciliation' | 'handover' | 'purchase' | 'purchase_payment' | 'owner_deposit' | 'owner_withdrawal' | 'capital_injection' | 'other_external_inflow' | 'treasury_transfer' | 'payroll_payment' | 'driver_settlement' | 'safe_drop';
  status: 'pending' | 'complete' | 'failed' | 'recovery_required';
  createdAt: string;
  movement?: TreasuryMovement;
  order?: Order;
  purchase?: Purchase;
  supplierEntries?: SupplierLedgerEntry[];
  stockMovements?: StockMovement[];
  employeeSettlements?: EmployeeLedgerEntry[];
}

const readJournal = (): FinancialJournalEntry[] => {
  if (typeof window === 'undefined') return [];
  return readFinancialJournal(window.localStorage) as FinancialJournalEntry[];
};

export const startFinancialJournalEntry = (entry: Omit<FinancialJournalEntry, 'status' | 'createdAt'>) => {
  const entries = readJournal();
  if (entries.some(item => item.idempotencyKey === entry.idempotencyKey)) return;
  startJournal(window.localStorage, entry);
};

export const completeFinancialJournalEntry = (
  idempotencyKey: string,
  movement: TreasuryMovement,
  order: Order
) => {
  const entries = readJournal();
  const current = entries.find(item => item.idempotencyKey === idempotencyKey);
  const complete: FinancialJournalEntry = {
    operationId: current?.operationId || movement.operationId || createOperationId(),
    idempotencyKey,
    kind: current?.kind || (movement.type === 'refund' ? 'refund' : 'sale'),
    createdAt: current?.createdAt || new Date().toISOString(),
    status: 'complete',
    movement,
    order,
  };
  completeJournal(window.localStorage, idempotencyKey, complete);
};

export const completeTreasuryJournalEntry = (idempotencyKey: string, movement: TreasuryMovement) => {
  const entries = readJournal();
  const current = entries.find(item => item.idempotencyKey === idempotencyKey);
  if (!current) return;
  completeJournal(window.localStorage, idempotencyKey, { movement });
};

export const completePurchaseJournalEntry = (idempotencyKey: string, data: {
  purchase: Purchase; movement?: TreasuryMovement; supplierEntries: SupplierLedgerEntry[]; stockMovements?: StockMovement[];
}) => {
  const entries = readJournal();
  const current = entries.find(item => item.idempotencyKey === idempotencyKey);
  if (!current) return;
  completeJournal(window.localStorage, idempotencyKey, data);
};

export const completePayrollJournalEntry = (idempotencyKey: string, movement: TreasuryMovement, employeeSettlements: EmployeeLedgerEntry[]) =>
  completeJournal(window.localStorage, idempotencyKey, { movement, employeeSettlements });

export const completedFinancialJournalEntries = (): FinancialJournalEntry[] =>
  readJournal().filter(entry => entry.status === 'complete' && (entry.movement || entry.purchase));
