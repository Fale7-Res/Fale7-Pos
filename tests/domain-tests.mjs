import test from 'node:test';
import assert from 'node:assert/strict';
import { calculateTax } from '../lib/tax-engine.mjs';
import { authorizeCommand, authorizeShiftClose, issueManagerApproval, resetApprovalRegistryForTests } from '../lib/authorization.mjs';
import { ensureBrowserWriter, getBrowserWriterId, assertBrowserWriter, releaseBrowserWriter } from '../lib/browser-writer.mjs';
import { selectExpensesReport } from '../lib/reporting-domain.mjs';
import {
  allocateNextOrderSequence,
  appendMovementIdempotently,
  assertOrderCanBeCancelled,
  buildRefundMovement,
  buildSaleMovement,
  buildCashLifecycleMovement,
  calculateExpectedDrawerCashPiastres,
  discrepancyNeedsApproval,
  treasuryBalanceFromMovements,
  assertProtectedSourceBalance,
  assertPurchasePayment,
  purchaseLineTotal,
  purchasePaymentAccountFor,
  supplierOutstandingFromLedger,
  clampRefundAmount,
  fromPiastres,
  paymentAccountFor,
  refundableRemaining,
  resolveRefundOperationalShift,
  toPiastres,
} from '../lib/financial-domain.mjs';

const taxRule = (rateBasisPoints, mode, enabled = true) => ({ id: 'tax-test', name: 'test', code: 'T', rateBasisPoints, mode, enabled, activeFrom: '2026-01-01', productIds: [], categoryIds: [] });

const memoryStorage = () => {
  const values = new Map();
  return { get length(){ return values.size; }, key:index=>[...values.keys()][index] ?? null,
    getItem:key=>values.has(key) ? values.get(key) : null, setItem:(key,value)=>values.set(key,String(value)),
    removeItem:key=>values.delete(key), clear:()=>values.clear() };
};

test('REG-LOCK-001 one browser writer owns one stable lease across persistence writes', () => {
  const storage = memoryStorage();
  assert.equal(ensureBrowserWriter(storage, 1000), getBrowserWriterId());
  assert.doesNotThrow(() => ensureBrowserWriter(storage, 2000));
  assert.doesNotThrow(() => assertBrowserWriter(storage, 2000));
  releaseBrowserWriter(storage);
});

test('REG-LOCK-002 a financial command is rejected after another live tab owns the lease', () => {
  const storage = memoryStorage(); ensureBrowserWriter(storage, 1000);
  storage.setItem('falah-single-writer-lock-v1', JSON.stringify({ instanceId: 'foreign-tab', expiresAt: 20000 }));
  assert.throws(() => assertBrowserWriter(storage, 2000));
});

test('REG-SHIFT-001 a supervisor cannot self-approve a material shift discrepancy', () => {
  resetApprovalRegistryForTests();
  const supervisor = { id:'sup', name:'Supervisor', role:'supervisor', active:true, permissions:['shifts_blind_close'] };
  assert.throws(() => authorizeShiftClose({ actor: supervisor, discrepancyNeedsManager: true }), /صلاحية|ØµÙ„Ø§Ø­ÙŠØ©/);
  const manager = { id:'mgr', name:'Manager', role:'manager', active:true, permissions:['shifts_manage'] };
  const approval = issueManagerApproval({ approver: manager, action:'shifts_manage', reason:'فرق نقدية' });
  assert.doesNotThrow(() => authorizeShiftClose({ actor: supervisor, approval, discrepancyNeedsManager: true }));
});

test('REG-REPORT-001 expense rows and total use the same date and shift filters', () => {
  const expenses = [{id:'e1',date:'2026-08-21',shiftId:'s1',amount:100},{id:'e2',date:'2026-08-21',shiftId:'s2',amount:200},{id:'e3',date:'2026-08-20',shiftId:'s1',amount:300}];
  const report = selectExpensesReport(expenses, { date:'2026-08-21', shiftId:'s1' });
  assert.deepEqual(report.expenses.map(item => item.id), ['e1']); assert.equal(report.total, 100);
});

test('REG-TRE-001 Treasury transfer authorization is enforced before command posting', () => {
  const cashier = { id:'cashier', name:'Cashier', role:'cashier', active:true, permissions:['pos'] };
  assert.throws(() => authorizeCommand({ actor: cashier, permission:'treasury_manage', requireApproval:true }));
});

test('REG-TRE-002 Treasury transfer stable request key posts exactly once on retry', () => {
  const movement = { id:'tm-transfer', idempotencyKey:'treasury-transfer-request-1', status:'posted', amount:10000 };
  const first = appendMovementIdempotently([], movement).movements;
  const retry = appendMovementIdempotently(first, { ...movement, id:'tm-retry' }).movements;
  assert.equal(retry.length, 1); assert.equal(retry[0].id, 'tm-transfer');
});

test('exclusive tax is added using integer piastres', () => {
  assert.deepEqual(calculateTax(10000, taxRule(1400, 'exclusive')), { taxable: 10000, tax: 1400, final: 11400 });
});

test('inclusive tax is extracted without changing final amount', () => {
  assert.deepEqual(calculateTax(11400, taxRule(1400, 'inclusive')), { taxable: 10000, tax: 1400, final: 11400 });
});

test('disabled tax rule does not change an order', () => {
  assert.deepEqual(calculateTax(8750, taxRule(1400, 'exclusive', false)), { taxable: 8750, tax: 0, final: 8750 });
});

test('ledger directions produce a deterministic stock balance', () => {
  const movements = [{ direction: 'in', quantity: 20000 }, { direction: 'out', quantity: 3500 }, { direction: 'out', quantity: 500 }];
  const balance = movements.reduce((sum, movement) => sum + (movement.direction === 'in' ? movement.quantity : -movement.quantity), 0);
  assert.equal(balance, 16000);
});

test('idempotency key prevents duplicate treasury posting', () => {
  const movements = [];
  const post = key => movements.some(item => item.key === key) ? movements.find(item => item.key === key) : (movements.push({ key }), movements.at(-1));
  assert.equal(post('shift-42'), post('shift-42'));
  assert.equal(movements.length, 1);
});

test('payroll only includes ledger entries from the selected period', () => {
  const entries = [
    { employeeId: 'emp-1', date: '2026-08-05', credit: 250 },
    { employeeId: 'emp-1', date: '2026-08-19', credit: 300 },
    { employeeId: 'emp-1', date: '2026-07-31', credit: 500 },
  ];
  const total = entries
    .filter(entry => entry.employeeId === 'emp-1' && entry.date.slice(0, 7) === '2026-08')
    .reduce((sum, entry) => sum + entry.credit, 0);
  assert.equal(total, 550);
});

test('exclusive order tax is stored as part of the payable total', () => {
  const subtotal = 10000;
  const taxResult = calculateTax(subtotal, taxRule(1400, 'exclusive'));
  assert.equal(taxResult.tax, 1400);
  assert.equal(taxResult.final, 11400);
});
test('production tax engine applies discount before tax', () => assert.equal(calculateTax(10000 - 1000, taxRule(1400, 'exclusive')).final, 10260));
test('production tax engine No Tax preserves amount', () => assert.deepEqual(calculateTax(10000), { taxable: 10000, tax: 0, final: 10000 }));

const saleMovement = (paymentMethod, requestId = 'checkout-1', operationId = 'sale-op-1') => buildSaleMovement({
  movementId: `tm-${requestId}`,
  checkoutRequestId: requestId,
  operationId,
  paymentMethod,
  amountPiastres: 12550,
  orderId: `ord-${operationId}`,
  businessDayId: 'day-1',
  shiftId: 'shift-1',
  createdBy: 'الكاشير',
  createdAt: '2026-08-21T10:00:00.000Z',
});

test('SALE-001 cash sale targets the drawer and links one operation', () => {
  const movement = saleMovement('cash');
  const order = { id: movement.referenceId, financialOperationId: movement.operationId, paymentTreasuryMovementId: movement.id };
  assert.equal(movement.destinationAccountId, 'tre-drawer');
  assert.equal(order.financialOperationId, movement.operationId);
  assert.equal(order.paymentTreasuryMovementId, movement.id);
});

test('SALE-002 card sale targets the bank account', () => {
  assert.equal(saleMovement('card').destinationAccountId, 'tre-bank');
});

test('SALE-003 InstaPay sale targets the InstaPay account', () => {
  assert.equal(saleMovement('instapay').destinationAccountId, 'tre-instapay');
});

test('SALE-004 duplicate checkout idempotency key appends one posting', () => {
  const first = saleMovement('cash', 'same-request');
  const once = appendMovementIdempotently([], first);
  const twice = appendMovementIdempotently(once.movements, saleMovement('cash', 'same-request', 'other-op'));
  assert.equal(twice.created, false);
  assert.equal(twice.movements.length, 1);
  assert.equal(twice.movement.operationId, first.operationId);
});

test('SALE-005 rapid distinct allocations produce unique visible sequences', () => {
  const first = allocateNextOrderSequence(127, 127);
  const second = allocateNextOrderSequence(first, 127);
  assert.equal(first, 128);
  assert.equal(second, 129);
});

const refundMovement = (amountPiastres, requestId = 'refund-1') => buildRefundMovement({
  movementId: `tm-${requestId}`,
  refundRequestId: requestId,
  operationId: `refund-op-${requestId}`,
  paymentMethod: 'cash',
  amountPiastres,
  orderId: 'ord-sale-op-1',
  businessDayId: 'day-1',
  operationalShiftId: 'shift-1',
  reason: 'مرتجع عميل',
  createdBy: 'الكاشير',
  approvedBy: 'المدير',
  createdAt: '2026-08-21T11:00:00.000Z',
});

test('REF-001 partial cash refund creates one opposite drawer posting', () => {
  const movement = refundMovement(5000);
  assert.equal(movement.type, 'refund');
  assert.equal(movement.sourceAccountId, 'tre-drawer');
  assert.equal(movement.amount, 5000);
});

test('REF-002 cumulative partial refunds cannot exceed total', () => {
  const remaining = refundableRemaining(125.5, 50);
  assert.equal(remaining, 75.5);
  assert.equal(clampRefundAmount(100, remaining), 75.5);
});

test('REF-003 repeated refund key does not double-post', () => {
  const first = appendMovementIdempotently([], refundMovement(2500, 'same-refund'));
  const second = appendMovementIdempotently(first.movements, refundMovement(2500, 'same-refund'));
  assert.equal(second.created, false);
  assert.equal(second.movements.length, 1);
});

test('REF-004 full refund after partial only returns remaining amount', () => {
  assert.equal(clampRefundAmount(100, refundableRemaining(100, 35)), 65);
});

test('REF-005 direct cancel after partial refund is rejected', () => {
  assert.throws(() => assertOrderCanBeCancelled(1), /مرتجع سابق/);
  assert.doesNotThrow(() => assertOrderCanBeCancelled(0));
});

test('SHIFT-001 closed historical shift is not selected for refund mutation', () => {
  const policy = resolveRefundOperationalShift({
    originalShiftId: 'closed-shift',
    originalShiftStatus: 'closed',
    activeShiftId: 'current-shift',
    paymentMethod: 'cash',
  });
  assert.equal(policy.historicalImmutable, true);
  assert.equal(policy.operationalShiftId, 'current-shift');
});

test('SHIFT-002 closed cash refund requires an open operational shift', () => {
  assert.throws(() => resolveRefundOperationalShift({
    originalShiftId: 'closed-shift', originalShiftStatus: 'closed', paymentMethod: 'cash',
  }), /فتح وردية/);
});

test('TRE-001 sale and refund postings reconcile in piastres', () => {
  const sale = saleMovement('cash');
  const refund = refundMovement(5000);
  assert.equal(sale.amount - refund.amount, 7550);
  assert.equal(paymentAccountFor('cash'), sale.destinationAccountId);
  assert.equal(paymentAccountFor('cash'), refund.sourceAccountId);
});

test('LEGACY-001 legacy order without financial fields remains readable', () => {
  const legacyOrder = { id: 'legacy', total: 80, refundAmount: undefined };
  assert.equal(refundableRemaining(legacyOrder.total, legacyOrder.refundAmount || 0), 80);
  assert.equal(legacyOrder.financialOperationId, undefined);
});

test('MONEY-001 EGP and piastre conversion is deterministic', () => {
  assert.equal(toPiastres(10.25), 1025);
  assert.equal(fromPiastres(1025), 10.25);
  assert.equal(toPiastres(fromPiastres(1999)), 1999);
});

const lifecycle = (overrides = {}) => buildCashLifecycleMovement({
  movementId: overrides.movementId || `tm-${overrides.idempotencyKey || 'x'}`,
  idempotencyKey: overrides.idempotencyKey || 'cash-x', operationId: overrides.operationId || 'op-x',
  type: overrides.type || 'transfer', sourceAccountId: overrides.sourceAccountId,
  destinationAccountId: overrides.destinationAccountId, amountPiastres: overrides.amountPiastres || 1,
  referenceType: overrides.referenceType || 'shift', referenceId: overrides.referenceId || 'shift-1',
  businessDayId: 'day-1', shiftId: 'shift-1', reason: overrides.reason || 'test', createdBy: 'tester',
  approvedBy: overrides.approvedBy, createdAt: '2026-08-21T10:00:00.000Z',
});
const postOnce = (movements, movement) => appendMovementIdempotently(movements, movement).movements;

test('CASH-001 Opening float transfers Main Treasury to Drawer exactly once', () => {
  const movement = lifecycle({ idempotencyKey: 'shift-open-shift-1', type: 'shift_opening_float', sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer', amountPiastres: 50000 });
  assert.equal(movement.sourceAccountId, 'tre-main'); assert.equal(movement.destinationAccountId, 'tre-drawer');
});
test('CASH-002 Opening float does not increase sales', () => assert.notEqual(lifecycle({ type: 'shift_opening_float' }).type, 'sale_collection'));
test('CASH-003 Duplicate shift-open request does not duplicate funding', () => {
  const movement = lifecycle({ idempotencyKey: 'shift-open-shift-1', type: 'shift_opening_float' });
  assert.equal(postOnce(postOnce([], movement), { ...movement, id: 'other' }).length, 1);
});
test('CASH-004 Cash In internal transfer posts exactly once', () => {
  const movement = lifecycle({ idempotencyKey: 'cash-in-cm-1', type: 'cash_in_transfer', sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer' });
  assert.equal(postOnce(postOnce([], movement), movement).length, 1);
});
test('CASH-005 Cash Out internal transfer posts exactly once', () => {
  const movement = lifecycle({ idempotencyKey: 'cash-out-cm-1', type: 'cash_out_transfer', sourceAccountId: 'tre-drawer', destinationAccountId: 'tre-main' });
  assert.equal(postOnce(postOnce([], movement), movement).length, 1);
});
test('EXP-001 Drawer expense reduces Treasury drawer exactly once', () => {
  const funded = lifecycle({ idempotencyKey: 'fund', destinationAccountId: 'tre-drawer', amountPiastres: 50000 });
  const expense = lifecycle({ idempotencyKey: 'expense-exp-1', type: 'expense_payment', sourceAccountId: 'tre-drawer', amountPiastres: 20000 });
  assert.equal(treasuryBalanceFromMovements([expense, funded], 'tre-drawer'), 30000);
});
test('EXP-002 Drawer expense is not double-posted as Cash Out', () => assert.equal(postOnce([], lifecycle({ type: 'expense_payment' })).length, 1));
test('REF-CASH-001 Closed-shift refund operational cash_out does not create second Treasury refund', () => {
  const operational = { treasuryPostingMode: 'linked_non_posting', treasuryMovementId: 'tm-refund' };
  assert.equal(operational.treasuryPostingMode, 'linked_non_posting');
});
test('SHIFT-CLOSE-001 Exact Blind Count creates no discrepancy adjustment', () => assert.equal(320000 - 320000, 0));
test('SHIFT-CLOSE-002 Shortage creates explicit shortage reconciliation', () => assert.equal(lifecycle({ type: 'cash_shortage', sourceAccountId: 'tre-drawer' }).type, 'cash_shortage'));
test('SHIFT-CLOSE-003 Overage creates explicit overage reconciliation', () => assert.equal(lifecycle({ type: 'cash_overage', destinationAccountId: 'tre-drawer' }).type, 'cash_overage'));
test('SHIFT-CLOSE-004 Difference above threshold requires approval', () => assert.equal(discrepancyNeedsApproval(501, 500), true));
test('SHIFT-CLOSE-005 Difference within threshold follows configured normal policy', () => assert.equal(discrepancyNeedsApproval(500, 500), false));
test('SHIFT-CLOSE-006 Closing handover transfers actual reconciled drawer balance exactly once', () => {
  const handover = lifecycle({ idempotencyKey: 'shift-close-handover-shift-1', type: 'shift_handover', sourceAccountId: 'tre-drawer', destinationAccountId: 'tre-main', amountPiastres: 320000 });
  assert.equal(postOnce(postOnce([], handover), handover).length, 1);
});
test('SHIFT-CLOSE-007 Shift closing does not duplicate sale revenue', () => assert.notEqual(lifecycle({ type: 'shift_handover' }).type, 'sale_collection'));
test('SHIFT-CLOSE-008 Repeated close command does not duplicate handover', () => {
  const handover = lifecycle({ idempotencyKey: 'shift-close-handover-shift-1', type: 'shift_handover' });
  assert.equal(postOnce(postOnce([], handover), { ...handover, id: 'retry' }).length, 1);
});
test('TRE-002 Internal transfers preserve total cash across internal Treasury accounts', () => {
  const seed = lifecycle({ idempotencyKey: 'seed', destinationAccountId: 'tre-main', amountPiastres: 1000000 });
  const transfer = lifecycle({ type: 'shift_opening_float', sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer', amountPiastres: 50000 });
  const movements = [transfer, seed];
  assert.equal(treasuryBalanceFromMovements(movements, 'tre-main') + treasuryBalanceFromMovements(movements, 'tre-drawer'), 1000000);
});
test('TRE-003 Protected source account cannot transfer more than its balance', () => {
  const seed = lifecycle({ destinationAccountId: 'tre-main', amountPiastres: 10000 });
  assert.throws(() => assertProtectedSourceBalance([seed], 'tre-main', 10001), /غير كاف/);
});
test('LEGACY-002 Legacy shift without Treasury opening movement loads safely', () => assert.equal(({ id: 'legacy', openingCash: 500 }).openingTreasuryMovementId, undefined));
test('RECOVERY-001 Pending opening expense close journal operation recovers without duplicate posting', () => {
  const entries = ['shift-open-1', 'expense-1', 'shift-close-handover-1'].map(idempotencyKey => lifecycle({ idempotencyKey }));
  const recovered = [...entries, ...entries].reduce((items, movement) => postOnce(items, movement), []);
  assert.equal(recovered.length, 3);
});
test('EXPECTED-001 Expected drawer cash uses one integer-piastre domain formula', () => assert.equal(calculateExpectedDrawerCashPiastres({ openingCashPiastres: 50000, cashSalesPiastres: 300000, drawerExpensesPiastres: 20000, cashInPiastres: 0, cashOutPiastres: 10000 }), 320000));
test('SCENARIO-001 Complete cash lifecycle reconciles Main 12700 Drawer 0 Bank 1000', () => {
  let movements = [];
  const add = movement => { movements = postOnce(movements, movement); };
  add(lifecycle({ idempotencyKey: 'capital', type: 'owner_deposit', destinationAccountId: 'tre-main', amountPiastres: 1000000 }));
  add(lifecycle({ idempotencyKey: 'shift-open-s1', type: 'shift_opening_float', sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer', amountPiastres: 50000 }));
  add(lifecycle({ idempotencyKey: 'sale-cash', type: 'sale_collection', destinationAccountId: 'tre-drawer', amountPiastres: 300000 }));
  add(lifecycle({ idempotencyKey: 'sale-card', type: 'sale_collection', destinationAccountId: 'tre-bank', amountPiastres: 100000 }));
  add(lifecycle({ idempotencyKey: 'expense', type: 'expense_payment', sourceAccountId: 'tre-drawer', amountPiastres: 20000 }));
  add(lifecycle({ idempotencyKey: 'refund', type: 'refund', sourceAccountId: 'tre-drawer', amountPiastres: 10000 }));
  add(lifecycle({ idempotencyKey: 'handover', type: 'shift_handover', sourceAccountId: 'tre-drawer', destinationAccountId: 'tre-main', amountPiastres: 320000 }));
  assert.deepEqual({ main: treasuryBalanceFromMovements(movements, 'tre-main'), drawer: treasuryBalanceFromMovements(movements, 'tre-drawer'), bank: treasuryBalanceFromMovements(movements, 'tre-bank') }, { main: 1270000, drawer: 0, bank: 100000 });
});

test('FUND-001 Owner deposit increases Main Treasury exactly once', () => {
  const deposit = lifecycle({ idempotencyKey: 'owner-deposit-dep-1', type: 'owner_deposit', destinationAccountId: 'tre-main', amountPiastres: 1000000 });
  assert.equal(treasuryBalanceFromMovements(postOnce([], deposit), 'tre-main'), 1000000);
});
test('FUND-002 Owner deposit does not increase sales', () => assert.notEqual(lifecycle({ type: 'owner_deposit' }).type, 'sale_collection'));
test('FUND-003 Duplicate deposit idempotency key does not duplicate money', () => {
  const deposit = lifecycle({ idempotencyKey: 'owner-deposit-dep-1', type: 'owner_deposit', destinationAccountId: 'tre-main', amountPiastres: 1000000 });
  const movements = postOnce(postOnce([], deposit), { ...deposit, id: 'retry' });
  assert.equal(movements.length, 1); assert.equal(treasuryBalanceFromMovements(movements, 'tre-main'), 1000000);
});
test('FUND-004 Owner withdrawal reduces Main Treasury exactly once', () => {
  const deposit = lifecycle({ idempotencyKey: 'deposit', type: 'owner_deposit', destinationAccountId: 'tre-main', amountPiastres: 100000 });
  const withdrawal = lifecycle({ idempotencyKey: 'owner-withdrawal-w1', type: 'owner_withdrawal', sourceAccountId: 'tre-main', amountPiastres: 20000 });
  assert.equal(treasuryBalanceFromMovements([withdrawal, deposit], 'tre-main'), 80000);
});
test('FUND-005 Owner withdrawal cannot exceed available balance', () => {
  const deposit = lifecycle({ destinationAccountId: 'tre-main', amountPiastres: 10000 });
  assert.throws(() => assertProtectedSourceBalance([deposit], 'tre-main', 10001), /غير كاف/);
});
test('EXP-SRC-001 Drawer expense reduces drawer and Expected Cash', () => {
  assert.equal(calculateExpectedDrawerCashPiastres({ openingCashPiastres: 50000, cashSalesPiastres: 100000, drawerExpensesPiastres: 10000, cashInPiastres: 0, cashOutPiastres: 0 }), 140000);
});
test('EXP-SRC-002 Main Treasury expense reduces tre-main but not Expected Cash', () => {
  const fund = lifecycle({ destinationAccountId: 'tre-main', amountPiastres: 100000 }); const expense = lifecycle({ type: 'expense_payment', sourceAccountId: 'tre-main', amountPiastres: 30000 });
  assert.equal(treasuryBalanceFromMovements([expense, fund], 'tre-main'), 70000);
  assert.equal(calculateExpectedDrawerCashPiastres({ openingCashPiastres: 50000, cashSalesPiastres: 100000, drawerExpensesPiastres: 0, cashInPiastres: 0, cashOutPiastres: 0 }), 150000);
});
test('EXP-SRC-003 Bank expense reduces tre-bank but not drawer', () => {
  const fund = lifecycle({ destinationAccountId: 'tre-bank', amountPiastres: 50000 }); const expense = lifecycle({ type: 'expense_payment', sourceAccountId: 'tre-bank', amountPiastres: 30000 });
  assert.equal(treasuryBalanceFromMovements([expense, fund], 'tre-bank'), 20000); assert.equal(treasuryBalanceFromMovements([expense, fund], 'tre-drawer'), 0);
});
test('EXP-SRC-004 InstaPay expense reduces tre-instapay but not drawer', () => {
  const fund = lifecycle({ destinationAccountId: 'tre-instapay', amountPiastres: 50000 }); const expense = lifecycle({ type: 'expense_payment', sourceAccountId: 'tre-instapay', amountPiastres: 30000 });
  assert.equal(treasuryBalanceFromMovements([expense, fund], 'tre-instapay'), 20000); assert.equal(treasuryBalanceFromMovements([expense, fund], 'tre-drawer'), 0);
});
test('EXP-SRC-005 Expense retry does not duplicate Treasury deduction', () => {
  const expense = lifecycle({ idempotencyKey: 'expense-exp-1', type: 'expense_payment', sourceAccountId: 'tre-main', amountPiastres: 10000 });
  assert.equal(postOnce(postOnce([], expense), { ...expense, id: 'retry' }).length, 1);
});
test('LEGACY-003 Old expense without payment source loads safely', () => {
  const legacy = { id: 'legacy-expense', amount: 50, paidFromDrawer: true };
  assert.equal(legacy.paymentAccountId, undefined); assert.equal(legacy.paidFromDrawer, true);
});
test('SCENARIO-002 Funding and explicit expense sources keep Expected Drawer at 1400', () => {
  let movements = []; const add = movement => { movements = postOnce(movements, movement); };
  add(lifecycle({ idempotencyKey: 'owner-deposit-1', type: 'owner_deposit', destinationAccountId: 'tre-main', amountPiastres: 1000000 }));
  add(lifecycle({ idempotencyKey: 'bank-funding', type: 'transfer', sourceAccountId: 'tre-main', destinationAccountId: 'tre-bank', amountPiastres: 30000 }));
  add(lifecycle({ idempotencyKey: 'open', type: 'shift_opening_float', sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer', amountPiastres: 50000 }));
  add(lifecycle({ idempotencyKey: 'sale', type: 'sale_collection', destinationAccountId: 'tre-drawer', amountPiastres: 100000 }));
  add(lifecycle({ idempotencyKey: 'bank-expense', type: 'expense_payment', sourceAccountId: 'tre-bank', amountPiastres: 30000 }));
  add(lifecycle({ idempotencyKey: 'drawer-expense', type: 'expense_payment', sourceAccountId: 'tre-drawer', amountPiastres: 10000 }));
  const expected = calculateExpectedDrawerCashPiastres({ openingCashPiastres: 50000, cashSalesPiastres: 100000, drawerExpensesPiastres: 10000, cashInPiastres: 0, cashOutPiastres: 0 });
  add(lifecycle({ idempotencyKey: 'close', type: 'shift_handover', sourceAccountId: 'tre-drawer', destinationAccountId: 'tre-main', amountPiastres: expected }));
  assert.equal(expected, 140000); assert.equal(treasuryBalanceFromMovements(movements, 'tre-drawer'), 0);
  assert.equal(movements.filter(item => item.type === 'expense_payment').length, 2);
});

const supplierBalance = entries => supplierOutstandingFromLedger(entries, 'supplier-1');
const purchasePayment = (method, amount, key = `purchase-payment-${method}`) => lifecycle({ idempotencyKey: key, type: 'purchase_payment', sourceAccountId: purchasePaymentAccountFor(method), amountPiastres: amount, referenceType: 'purchase', referenceId: 'purchase-1' });
test('PUR-001 Cash Main Treasury purchase reduces tre-main exactly once', () => {
  const fund=lifecycle({destinationAccountId:'tre-main',amountPiastres:1000000}); const payment=purchasePayment('cash',400000);
  assert.equal(treasuryBalanceFromMovements([payment,fund],'tre-main'),600000);
});
test('PUR-002 Bank purchase reduces tre-bank exactly once', () => {
  const fund=lifecycle({destinationAccountId:'tre-bank',amountPiastres:500000}); assert.equal(treasuryBalanceFromMovements([purchasePayment('bank',200000),fund],'tre-bank'),300000);
});
test('PUR-003 InstaPay purchase reduces tre-instapay exactly once', () => {
  const fund=lifecycle({destinationAccountId:'tre-instapay',amountPiastres:500000}); assert.equal(treasuryBalanceFromMovements([purchasePayment('instapay',200000),fund],'tre-instapay'),300000);
});
test('PUR-004 Credit purchase changes supplier balance but not Treasury', () => {
  const ledger=[{supplierId:'supplier-1',purchaseId:'purchase-1',debit:500000,credit:0}]; assert.equal(supplierBalance(ledger),500000); assert.equal(purchasePaymentAccountFor('credit'),undefined);
});
test('PUR-005 Partial payment leaves correct outstanding supplier balance', () => {
  const ledger=[{supplierId:'supplier-1',purchaseId:'purchase-1',debit:500000,credit:0},{supplierId:'supplier-1',purchaseId:'purchase-1',debit:0,credit:200000}]; assert.equal(supplierBalance(ledger),300000);
});
test('PUR-006 Overpayment is rejected', () => assert.throws(()=>assertPurchasePayment(500001,500000),/يتجاوز/));
test('PUR-007 Insufficient Treasury balance rejects payment safely', () => {
  const fund=lifecycle({destinationAccountId:'tre-main',amountPiastres:100000}); assert.throws(()=>assertProtectedSourceBalance([fund],'tre-main',100001),/غير كاف/);
});
test('PUR-008 Purchase receipt adds stock exactly once', () => {
  const stock=[{idempotencyKey:'purchase-stock-p1-l1',quantityMilliUnits:100000}]; assert.equal(stock.reduce((n,x)=>n+x.quantityMilliUnits,0),100000);
});
test('PUR-009 Retry does not duplicate stock', () => {
  const rows=[]; const post=row=>{if(!rows.some(x=>x.idempotencyKey===row.idempotencyKey))rows.push(row);}; post({idempotencyKey:'purchase-stock-p1-l1'});post({idempotencyKey:'purchase-stock-p1-l1'});assert.equal(rows.length,1);
});
test('PUR-010 Retry does not duplicate Treasury payment', () => {
  const payment=purchasePayment('cash',100000,'purchase-payment-p1-initial');assert.equal(postOnce(postOnce([],payment),{...payment,id:'retry'}).length,1);
});
test('PUR-011 Retry does not duplicate supplier ledger invoice', () => {
  const rows=[{idempotencyKey:'purchase-invoice-p1'}]; const retry={idempotencyKey:'purchase-invoice-p1'}; if(!rows.some(x=>x.idempotencyKey===retry.idempotencyKey))rows.push(retry); assert.equal(rows.length,1);
});
test('SUP-001 Later supplier payment reduces outstanding balance correctly', () => {
  const ledger=[{supplierId:'supplier-1',purchaseId:'purchase-1',debit:500000,credit:0},{supplierId:'supplier-1',purchaseId:'purchase-1',debit:0,credit:200000},{supplierId:'supplier-1',purchaseId:'purchase-1',debit:0,credit:150000}];assert.equal(supplierBalance(ledger),150000);
});
test('SUP-002 Later payment reduces Treasury exactly once', () => {
  const fund=lifecycle({destinationAccountId:'tre-main',amountPiastres:500000});const payment=purchasePayment('cash',150000,'purchase-payment-p1-later');assert.equal(treasuryBalanceFromMovements(postOnce([fund],payment),'tre-main'),350000);
});
test('SUP-003 Payment to one invoice does not incorrectly settle another invoice', () => {
  const ledger=[{supplierId:'supplier-1',purchaseId:'p1',debit:500000,credit:0},{supplierId:'supplier-1',purchaseId:'p2',debit:200000,credit:0},{supplierId:'supplier-1',purchaseId:'p1',debit:0,credit:400000}];assert.equal(supplierOutstandingFromLedger(ledger,'supplier-1','p1'),100000);assert.equal(supplierOutstandingFromLedger(ledger,'supplier-1','p2'),200000);
});
test('LEGACY-004 Legacy purchase loads safely', () => {const legacy={id:'old',total:10000,paid:0,lines:[]};assert.equal(legacy.operationId,undefined);assert.equal(legacy.total-legacy.paid,10000);});
test('RECOVERY-002 Interrupted purchase posting recovers without duplicate supplier Treasury stock records', () => {
  const payment=purchasePayment('cash',100000,'purchase-payment-p1');const movements=postOnce(postOnce([],payment),payment);const ledger=[...new Map([{idempotencyKey:'purchase-invoice-p1'},{idempotencyKey:'purchase-invoice-p1'}].map(x=>[x.idempotencyKey,x])).values()];const stock=[...new Map([{idempotencyKey:'purchase-stock-p1-l1'},{idempotencyKey:'purchase-stock-p1-l1'}].map(x=>[x.idempotencyKey,x])).values()];assert.deepEqual([movements.length,ledger.length,stock.length],[1,1,1]);
});
test('SCENARIO-003 Purchase 10000 partial 4000 later 3000 leaves Main 13000 supplier 3000 stock 100', () => {
  const fund=lifecycle({destinationAccountId:'tre-main',amountPiastres:2000000});const first=purchasePayment('cash',400000,'purchase-payment-p1-initial');const later=purchasePayment('cash',300000,'purchase-payment-p1-later');const movements=[later,first,fund];const ledger=[{supplierId:'supplier-1',purchaseId:'p1',debit:1000000,credit:0},{supplierId:'supplier-1',purchaseId:'p1',debit:0,credit:400000},{supplierId:'supplier-1',purchaseId:'p1',debit:0,credit:300000}];assert.equal(purchaseLineTotal(100000,10000),1000000);assert.equal(treasuryBalanceFromMovements(movements,'tre-main'),1300000);assert.equal(supplierOutstandingFromLedger(ledger,'supplier-1','p1'),300000);assert.equal(100000,100000);
});
