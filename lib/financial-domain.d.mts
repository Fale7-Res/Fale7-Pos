import type { PaymentMethod } from './types';
import type { PaymentTreasuryAccountId, TreasuryMovement } from './enterprise-types';
import type { TreasuryMovementType } from './enterprise-types';

export function toPiastres(egp: number): number;
export function fromPiastres(piastres: number): number;
export function createOperationId(prefix?: string): string;
export function paymentAccountFor(method: PaymentMethod): PaymentTreasuryAccountId;
export function allocateNextOrderSequence(lastAllocated: number, existingMax: number): number;
export function appendMovementIdempotently(movements: TreasuryMovement[], movement: TreasuryMovement): {
  movement: TreasuryMovement;
  movements: TreasuryMovement[];
  created: boolean;
};
export function buildSaleMovement(input: {
  movementId: string; checkoutRequestId: string; operationId: string; paymentMethod: Exclude<PaymentMethod, 'multi'>;
  amountPiastres: number; orderId: string; businessDayId: string; shiftId: string; createdBy: string; createdAt: string;
}): TreasuryMovement;
export function buildRefundMovement(input: {
  movementId: string; refundRequestId: string; operationId: string; paymentMethod: Exclude<PaymentMethod, 'multi'>;
  amountPiastres: number; orderId: string; businessDayId: string; operationalShiftId?: string; reason: string;
  createdBy: string; approvedBy: string; createdAt: string;
}): TreasuryMovement;
export function refundableRemaining(total: number, alreadyRefunded: number): number;
export function clampRefundAmount(requested: number, remaining: number): number;
export function assertOrderCanBeCancelled(refundAmount?: number): void;
export function resolveRefundOperationalShift(input: {
  originalShiftId: string;
  originalShiftStatus?: string;
  activeShiftId?: string;
  paymentMethod: Exclude<PaymentMethod, 'multi'>;
}): { historicalImmutable: boolean; operationalShiftId?: string };
export function treasuryBalanceFromMovements(movements: TreasuryMovement[], accountId: string): number;
export function calculateExpectedDrawerCashPiastres(input: { openingCashPiastres: number; cashSalesPiastres: number; drawerExpensesPiastres: number; cashInPiastres: number; cashOutPiastres: number }): number;
export function discrepancyNeedsApproval(differencePiastres: number, thresholdPiastres: number): boolean;
export function assertProtectedSourceBalance(movements: TreasuryMovement[], sourceAccountId: string, amountPiastres: number): void;
export function buildCashLifecycleMovement(input: { movementId: string; idempotencyKey: string; operationId: string; type: TreasuryMovementType; sourceAccountId?: string; destinationAccountId?: string; amountPiastres: number; referenceType: string; referenceId: string; businessDayId: string; shiftId?: string; reason: string; createdBy: string; approvedBy?: string; createdAt: string }): TreasuryMovement;
export function purchasePaymentAccountFor(method: 'cash' | 'bank' | 'instapay' | 'credit'): 'tre-main' | 'tre-bank' | 'tre-instapay' | undefined;
export function purchaseLineTotal(quantityMilliUnits: number, unitCostPiastres: number): number;
export function assertPurchasePayment(paidPiastres: number, totalPiastres: number): void;
export function supplierOutstandingFromLedger(entries: import('./enterprise-types').SupplierLedgerEntry[], supplierId: string, purchaseId?: string): number;
