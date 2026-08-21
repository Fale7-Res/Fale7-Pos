export const toPiastres = (egp) => {
  if (!Number.isFinite(egp)) throw new Error('المبلغ غير صالح.');
  return Math.round((egp + Number.EPSILON) * 100);
};

export const fromPiastres = (piastres) => {
  if (!Number.isInteger(piastres)) throw new Error('قيمة القروش يجب أن تكون عددًا صحيحًا.');
  return piastres / 100;
};

export const createOperationId = (prefix = 'fin') => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return `${prefix}-${crypto.randomUUID()}`;
  }
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
};

export const paymentAccountFor = (method) => {
  if (method === 'cash') return 'tre-drawer';
  if (method === 'card') return 'tre-bank';
  if (method === 'instapay') return 'tre-instapay';
  throw new Error('الدفع المتعدد غير مدعوم في مسار التحصيل المالي الحالي.');
};

export const allocateNextOrderSequence = (lastAllocated, existingMax) =>
  Math.max(127, lastAllocated, existingMax) + 1;

export const appendMovementIdempotently = (movements, movement) => {
  const existing = movements.find(item => item.idempotencyKey === movement.idempotencyKey);
  return existing
    ? { movement: existing, movements, created: false }
    : { movement, movements: [movement, ...movements], created: true };
};

export const buildSaleMovement = (input) => ({
  id: input.movementId,
  idempotencyKey: `sale-${input.checkoutRequestId}`,
  operationId: input.operationId,
  type: 'sale_collection',
  destinationAccountId: paymentAccountFor(input.paymentMethod),
  amount: input.amountPiastres,
  referenceType: 'order',
  referenceId: input.orderId,
  businessDayId: input.businessDayId,
  shiftId: input.shiftId,
  reason: `تحصيل طلب ${input.orderId}`,
  createdBy: input.createdBy,
  createdAt: input.createdAt,
  status: 'posted',
});

export const buildRefundMovement = (input) => ({
  id: input.movementId,
  idempotencyKey: `refund-${input.refundRequestId}`,
  operationId: input.operationId,
  type: 'refund',
  sourceAccountId: paymentAccountFor(input.paymentMethod),
  amount: input.amountPiastres,
  referenceType: 'order_refund',
  referenceId: input.orderId,
  businessDayId: input.businessDayId,
  shiftId: input.operationalShiftId,
  reason: input.reason,
  createdBy: input.createdBy,
  approvedBy: input.approvedBy,
  createdAt: input.createdAt,
  status: 'posted',
});

export const refundableRemaining = (total, alreadyRefunded) =>
  Math.max(0, total - alreadyRefunded);

export const clampRefundAmount = (requested, remaining) =>
  Math.min(Math.max(0, requested), Math.max(0, remaining));

export const assertOrderCanBeCancelled = (refundAmount = 0) => {
  if (refundAmount > 0) {
    throw new Error('هذا الطلب يحتوي على مرتجع سابق. استكمل رد المبلغ المتبقي بدلًا من إلغاء الطلب.');
  }
};

export const resolveRefundOperationalShift = ({ originalShiftId, originalShiftStatus, activeShiftId, paymentMethod }) => {
  const historicalImmutable = originalShiftStatus === 'closed';
  if (historicalImmutable && paymentMethod === 'cash' && !activeShiftId) {
    throw new Error('يجب فتح وردية قبل تنفيذ مرتجع نقدي لطلب من وردية مغلقة.');
  }
  return {
    historicalImmutable,
    operationalShiftId: historicalImmutable ? activeShiftId : originalShiftId,
  };
};

export const treasuryBalanceFromMovements = (movements, accountId) => movements.reduce((sum, movement) => {
  if (movement.status !== 'posted') return sum;
  return sum + (movement.destinationAccountId === accountId ? movement.amount : 0)
    - (movement.sourceAccountId === accountId ? movement.amount : 0);
}, 0);

export const calculateExpectedDrawerCashPiastres = ({
  openingCashPiastres, cashSalesPiastres, drawerExpensesPiastres, cashInPiastres, cashOutPiastres,
}) => openingCashPiastres + cashSalesPiastres + cashInPiastres - drawerExpensesPiastres - cashOutPiastres;

export const discrepancyNeedsApproval = (differencePiastres, thresholdPiastres) =>
  Math.abs(differencePiastres) > Math.max(0, thresholdPiastres);

export const assertProtectedSourceBalance = (movements, sourceAccountId, amountPiastres) => {
  const protectedAccounts = ['tre-main', 'tre-drawer', 'tre-bank', 'tre-instapay'];
  if (protectedAccounts.includes(sourceAccountId) && treasuryBalanceFromMovements(movements, sourceAccountId) < amountPiastres) {
    throw new Error('رصيد حساب المصدر غير كافٍ لتنفيذ الحركة.');
  }
};

export const buildCashLifecycleMovement = (input) => ({
  id: input.movementId,
  idempotencyKey: input.idempotencyKey,
  operationId: input.operationId,
  type: input.type,
  sourceAccountId: input.sourceAccountId,
  destinationAccountId: input.destinationAccountId,
  amount: input.amountPiastres,
  referenceType: input.referenceType,
  referenceId: input.referenceId,
  businessDayId: input.businessDayId,
  shiftId: input.shiftId,
  reason: input.reason,
  createdBy: input.createdBy,
  approvedBy: input.approvedBy,
  createdAt: input.createdAt,
  status: 'posted',
});

export const purchasePaymentAccountFor = (method) => {
  if (method === 'cash') return 'tre-main';
  if (method === 'bank') return 'tre-bank';
  if (method === 'instapay') return 'tre-instapay';
  if (method === 'credit') return undefined;
  throw new Error('مصدر دفع المشتريات غير مدعوم.');
};

export const purchaseLineTotal = (quantityMilliUnits, unitCostPiastres) =>
  Math.round(quantityMilliUnits * unitCostPiastres / 1000);

export const assertPurchasePayment = (paidPiastres, totalPiastres) => {
  if (!Number.isInteger(paidPiastres) || paidPiastres < 0) throw new Error('المبلغ المدفوع غير صالح.');
  if (paidPiastres > totalPiastres) throw new Error('المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.');
};

export const supplierOutstandingFromLedger = (entries, supplierId, purchaseId) => entries
  .filter(entry => entry.supplierId === supplierId && (!purchaseId || entry.purchaseId === purchaseId || entry.referenceId === purchaseId))
  .reduce((sum, entry) => sum + entry.debit - entry.credit, 0);
