'use client';

import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type {
  BusinessDay, CashHandover, CashDenominationCount, ChecklistCompletion, ControlAuditEvent, ControlException, DailyCloseSnapshot, DeliveryAssignment, DeliveryEvent, DeliveryReassignment, DeliveryTrip, DiningTable, Driver, DriverCustody, DriverSettlement, InventoryItem, LeaveRequest, OperationalChecklistItem, PayrollLine, PayrollRun,
  Purchase, StockMovement, Supplier, SupplierLedgerEntry, TaxRule, TreasuryAccount,
  TreasuryMovement,
} from './enterprise-types';
import type { PaymentMethod } from './types';
import type { User } from './types';
import type { ManagerApproval } from './authorization.mjs';
import { authorizeCommand } from './authorization.mjs';
import { ensureBrowserWriter, assertBrowserWriter } from './browser-writer.mjs';
import type { TreasuryMovementType } from './enterprise-types';
import {
  appendMovementIdempotently,
  assertProtectedSourceBalance,
  assertPurchasePayment,
  buildCashLifecycleMovement,
  buildRefundMovement,
  buildSaleMovement,
  completedFinancialJournalEntries,
  completeTreasuryJournalEntry,
  completePurchaseJournalEntry,
  completePayrollJournalEntry,
  createOperationId,
  purchaseLineTotal,
  purchasePaymentAccountFor,
  supplierOutstandingFromLedger,
  startFinancialJournalEntry,
} from './financial-posting';
import { assertPayrollTransition, buildPayrollSettlements } from './payroll-domain.mjs';
import { assertCollectionAmount, assertDeliveryTransition, assertShiftHasNoUnsettledCustody, calculateTripSettlement, createCashCustody, createDeliveryTrip as buildDeliveryTrip, deliveryExceptions as selectDeliveryExceptions, deliveryReport as selectDeliveryReport } from './delivery-domain.mjs';
import type { EmployeeLedgerEntry } from './types';
import { ENTERPRISE_STORAGE_KEY, readJson, writeJson, PersistenceError } from './local-persistence.mjs';
import { reportPersistenceIssue } from './persistence-health';
import { assertBusinessDayTransition, buildOpenedBusinessDay, buildReopenedBusinessDay, buildSafeDropMovement, calculateDenominationCount, createDailyCloseSnapshot, evaluateCloseBlockers, latestBusinessDaySnapshot, updateExceptionState } from './control-layer-domain.mjs';
import { normalizeEnterpriseState } from './state-normalization.mjs';

const key = ENTERPRISE_STORAGE_KEY;
const uid = (prefix: string) => `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}`;
const now = () => new Date().toISOString();

const seedAccounts: TreasuryAccount[] = [
  { id: 'tre-main', name: 'الخزنة الرئيسية', type: 'main_safe', active: true },
  { id: 'tre-drawer', name: 'درج الكاشير', type: 'cashier_drawer', active: true },
  { id: 'tre-bank', name: 'الحساب البنكي', type: 'bank', active: true },
  { id: 'tre-instapay', name: 'حساب InstaPay', type: 'instapay', active: true },
];

const seedInventory: InventoryItem[] = [
  { id: 'inv-chicken', name: 'فراخ', category: 'لحوم ودواجن', unit: 'kg', minimumMilliUnits: 15000, active: true },
  { id: 'inv-coal', name: 'فحم', category: 'تشغيل', unit: 'bag', minimumMilliUnits: 5000, active: true },
  { id: 'inv-oil', name: 'زيت', category: 'خامات', unit: 'liter', minimumMilliUnits: 10000, active: true },
  { id: 'inv-packaging', name: 'عبوات تغليف', category: 'تغليف', unit: 'piece', minimumMilliUnits: 100000, active: true },
];

interface EnterpriseState {
  treasuryAccounts: TreasuryAccount[];
  treasuryMovements: TreasuryMovement[];
  cashHandovers: CashHandover[];
  suppliers: Supplier[];
  supplierLedger: SupplierLedgerEntry[];
  purchases: Purchase[];
  inventoryItems: InventoryItem[];
  stockMovements: StockMovement[];
  recipes: Recipe[];
  recipeItems: RecipeItem[];
  payrollRuns: PayrollRun[];
  payrollLines: PayrollLine[];
  leaveRequests: LeaveRequest[];
  taxRules: TaxRule[];
  businessDays: BusinessDay[];
  drivers: Driver[];
  deliveries: DeliveryAssignment[];
  deliveryTrips: DeliveryTrip[];
  driverCustodies: DriverCustody[];
  deliveryReassignments: DeliveryReassignment[];
  deliveryEvents: DeliveryEvent[];
  driverSettlements: DriverSettlement[];
  diningTables: DiningTable[];
  dailyCloseSnapshots: DailyCloseSnapshot[];
  cashDenominationCounts: CashDenominationCount[];
  checklistItems: OperationalChecklistItem[];
  checklistCompletions: ChecklistCompletion[];
  exceptionStates: Record<string,{status:'acknowledged'|'resolved';actor:string;reason:string;timestamp:string}>;
  controlAuditEvents: ControlAuditEvent[];
}

interface EnterpriseContextValue extends EnterpriseState {
  exportState: () => EnterpriseState;
  importState: (snapshot: unknown) => boolean;
  treasuryBalance: (accountId: string) => number;
  supplierBalance: (supplierId: string) => number;
  stockBalance: (itemId: string) => number;
  transferTreasury: (input: TreasuryTransferInput) => TreasuryMovement;
  postSaleCollection: (input: FinancialSalePostingInput) => TreasuryMovement;
  postRefund: (input: FinancialRefundPostingInput) => TreasuryMovement;
  postCashLifecycleMovement: (input: CashLifecyclePostingInput) => TreasuryMovement;
  postExternalFunding: (input: ExternalFundingInput) => TreasuryMovement;
  postOwnerWithdrawal: (input: OwnerWithdrawalInput) => TreasuryMovement;
  createHandover: (handover: Omit<CashHandover, 'id' | 'createdAt'>) => void;
  addSupplier: (supplier: Omit<Supplier, 'id'>) => void;
  addSupplierEntry: (entry: Omit<SupplierLedgerEntry, 'id' | 'createdAt'>) => void;
  addInventoryItem: (item: Omit<InventoryItem, 'id'>) => void;
  postStockMovement: (movement: Omit<StockMovement, 'id' | 'createdAt'>) => void;
  receivePurchase: (purchase: PurchaseCommandInput) => Purchase;
  payPurchase: (payment: PurchasePaymentCommandInput) => Purchase;
  createPayroll: (run: Omit<PayrollRun, 'id' | 'createdAt' | 'status'>, lines: Omit<PayrollLine, 'id' | 'payrollRunId' | 'netPayable'>[]) => void;
  setPayrollStatus: (id: string, status: PayrollRun['status'], approvedBy?: string) => void;
  transitionPayroll: (id: string, status: PayrollRun['status'], actor: User, approval?: ManagerApproval) => void;
  payPayroll: (input: { payrollRunId:string; paymentAccountId:'tre-main'|'tre-bank'|'tre-instapay'; actor:User; approval?:ManagerApproval; createdBy:string }) => { movement:TreasuryMovement; settlements:EmployeeLedgerEntry[] };
  addLeave: (leave: Omit<LeaveRequest, 'id' | 'status'>) => void;
  decideLeave: (id: string, status: 'approved' | 'rejected', approvedBy: string) => void;
  addTaxRule: (rule: Omit<TaxRule, 'id'>) => void;
  updateTaxRule: (rule: TaxRule) => void;
  closeBusinessDay: (input: {businessDayId:string;actor:User;approval?:ManagerApproval;reconciliation:Record<string,unknown>;shifts:unknown[];journalEntries?:unknown[];persistenceHealthy?:boolean;writerOwned?:boolean;exceptions?:ControlException[]}) => DailyCloseSnapshot;
  openBusinessDay: (input:{requestId:string;businessDate:string;actor:User}) => BusinessDay;
  reopenBusinessDay: (input:{businessDayId:string;reason:string;actor:User;approval?:ManagerApproval;persistenceHealthy:boolean}) => BusinessDay;
  saveCashCount: (input:{id:string;businessDayId:string;shiftId?:string;rows:{denominationPiastres:number;quantity:number}[];expectedPiastres:number;actor:User}) => CashDenominationCount;
  completeChecklistItem: (businessDayId:string,itemId:string,completed:boolean,actor:User) => void;
  setExceptionStatus: (id:string,status:'acknowledged'|'resolved',reason:string,actor:User,approval?:ManagerApproval) => void;
  postSafeDrop: (input:{dropId:string;shiftId:string;amountPiastres:number;reason:string;actor:User;approval?:ManagerApproval}) => TreasuryMovement;
  addDriver: (driver: Omit<Driver, 'id' | 'custodyAccountId'>) => void;
  ensureEmployeeDriver: (employee: {id:string;name:string;phone:string;isActive:boolean}) => Driver;
  registerDeliveryOrder: (input: Omit<DeliveryAssignment,'id'|'status'> & {status?:DeliveryAssignment['status']}) => DeliveryAssignment;
  markDeliveryReady: (deliveryId:string,actor:User,approval?:ManagerApproval) => DeliveryAssignment;
  assignDelivery: (input:{requestId:string;deliveryId:string;driverId:string;actor:User;approval?:ManagerApproval}) => DeliveryAssignment;
  reassignDelivery: (input:{requestId:string;deliveryId:string;newDriverId:string;reason:string;actor:User;approval?:ManagerApproval}) => DeliveryAssignment;
  createDeliveryTrip: (input:{requestId:string;driverId:string;deliveryIds:string[];actor:User;approval?:ManagerApproval}) => DeliveryTrip;
  handoverDeliveryTrip: (input:{tripId:string;actor:User;approval?:ManagerApproval}) => DeliveryTrip;
  startDeliveryTrip: (input:{tripId:string;actor:User;approval?:ManagerApproval}) => DeliveryTrip;
  recordDeliveryResult: (input:{deliveryId:string;result:'delivered'|'failed'|'returned';collectedAmountPiastres?:number;reason?:string;notes?:string;actor:User;approval?:ManagerApproval}) => DeliveryAssignment;
  returnDeliveryTrip: (input:{tripId:string;actor:User;approval?:ManagerApproval}) => DeliveryTrip;
  settleDeliveryTrip: (input:{settlementId:string;tripId:string;actualPiastres:number;reason?:string;actor:User;approval?:ManagerApproval}) => {movement?:TreasuryMovement;settlement:DriverSettlement};
  assertShiftDeliverySettled: (shiftId:string) => true;
  deliveryReport: () => ReturnType<typeof selectDeliveryReport>;
  deliveryExceptions: () => ReturnType<typeof selectDeliveryExceptions>;
  updateTable: (id: string, status: DiningTable['status'], orderId?: string) => void;
}

interface FinancialSalePostingInput {
  operationId: string;
  checkoutRequestId: string;
  orderId: string;
  shiftId: string;
  paymentMethod: Exclude<PaymentMethod, 'multi'>;
  amountPiastres: number;
  createdBy: string;
}

interface FinancialRefundPostingInput {
  operationId: string;
  refundRequestId: string;
  orderId: string;
  originalShiftId: string;
  operationalShiftId?: string;
  paymentMethod: Exclude<PaymentMethod, 'multi'>;
  amountPiastres: number;
  createdBy: string;
  approvedBy: string;
  reason: string;
}

interface CashLifecyclePostingInput {
  operationId: string;
  idempotencyKey: string;
  type: Extract<TreasuryMovementType, 'shift_opening_float' | 'cash_in_transfer' | 'cash_out_transfer' | 'expense_payment' | 'cash_shortage' | 'cash_overage' | 'shift_handover'>;
  sourceAccountId?: string;
  destinationAccountId?: string;
  amountPiastres: number;
  referenceType: string;
  referenceId: string;
  shiftId: string;
  reason: string;
  createdBy: string;
  approvedBy?: string;
}

interface TreasuryTransferInput {
  requestId: string; sourceAccountId: string; destinationAccountId: string;
  amountPiastres: number; reason: string; actor: User; approval?: ManagerApproval;
}

interface ExternalFundingInput {
  operationId: string; depositId: string;
  type: 'owner_deposit' | 'capital_injection' | 'other_external_inflow';
  amountPiastres: number; destinationAccountId: 'tre-main' | 'tre-bank' | 'tre-instapay';
  reason: string; reference?: string; createdBy: string; approvedBy?: string;
  actor: User; approval?: ManagerApproval;
}

interface OwnerWithdrawalInput {
  operationId: string; withdrawalId: string; amountPiastres: number;
  sourceAccountId: 'tre-main' | 'tre-bank' | 'tre-instapay';
  reason: string; reference?: string; createdBy: string; approvedBy?: string;
  actor: User; approval?: ManagerApproval;
}

interface PurchaseCommandInput {
  requestId: string; supplierId: string; invoiceNumber?: string; lines: Purchase['lines'];
  paid: number; paymentMethod: Purchase['paymentMethod']; receivedBy: string; createdBy: string; notes?: string; approvedBy?: string;
}

interface PurchasePaymentCommandInput {
  requestId: string; purchaseId: string; amount: number;
  paymentMethod: Exclude<Purchase['paymentMethod'], 'credit'>; createdBy: string; approvedBy?: string;
}

const initial: EnterpriseState = {
  treasuryAccounts: seedAccounts, treasuryMovements: [], cashHandovers: [], suppliers: [], supplierLedger: [], purchases: [],
  inventoryItems: seedInventory, stockMovements: [], recipes: [], recipeItems: [], payrollRuns: [], payrollLines: [], leaveRequests: [], taxRules: [],
  businessDays: [{ id: `day-${new Date().toISOString().slice(0, 10)}`, date: new Date().toISOString().slice(0, 10), businessDate:new Date().toISOString().slice(0,10), openedAt:now(), openedBy:'system', status: 'open' }],
  drivers: [], deliveries: [], deliveryTrips: [], driverCustodies: [], deliveryReassignments: [], deliveryEvents: [], driverSettlements: [],
  diningTables: Array.from({ length: 12 }, (_, i) => ({ id: `table-${i+1}`, name: `طاولة ${i+1}`, area: i < 8 ? 'الصالة الرئيسية' : 'العائلات', status: 'available' as const })),
  dailyCloseSnapshots: [], cashDenominationCounts: [], checklistCompletions: [], exceptionStates: {}, controlAuditEvents: [],
  checklistItems: [
    {id:'open-drawer',label:'تجهيز درج النقدية',phase:'opening',required:true,active:true,sortOrder:1},
    {id:'open-printer',label:'فحص الطابعة',phase:'opening',required:false,active:true,sortOrder:2},
    {id:'open-stations',label:'تأكيد جاهزية محطات التشغيل والتنظيف',phase:'opening',required:false,active:true,sortOrder:3},
    {id:'close-shifts',label:'مراجعة إغلاق كل الورديات',phase:'closing',required:true,active:true,sortOrder:1},
    {id:'close-drawer',label:'عد درج النقدية',phase:'closing',required:true,active:true,sortOrder:2},
    {id:'close-delivery',label:'مراجعة عهد السائقين',phase:'closing',required:true,active:true,sortOrder:3},
    {id:'close-review',label:'مراجعة المرتجعات والمصروفات',phase:'closing',required:false,active:true,sortOrder:4},
  ],
};

const Context = createContext<EnterpriseContextValue | null>(null);

export function EnterpriseProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<EnterpriseState>(initial);
  const stateRef = useRef<EnterpriseState>(initial);
  const movementsRef = useRef<TreasuryMovement[]>(initial.treasuryMovements);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    try {
      const persisted = readJson<EnterpriseState>(localStorage, key, { validate: value => !!value && typeof value === 'object' && Array.isArray((value as EnterpriseState).treasuryMovements) });
      const raw = persisted.exists ? persisted.raw : null;
      const saved = raw ? normalizeEnterpriseState(persisted.value, initial) as EnterpriseState : initial;
      const journalEntries = completedFinancialJournalEntries();
      const recovered = journalEntries.reduce(
        (movements, entry) => entry.movement ? appendMovementIdempotently(movements, entry.movement).movements : movements,
        saved.treasuryMovements
      );
      const recoveredPurchases = journalEntries.slice().reverse().reduce((items, entry) => !entry.purchase ? items : items.some(item => item.id === entry.purchase?.id) ? items.map(item => item.id === entry.purchase?.id ? entry.purchase as Purchase : item) : [entry.purchase, ...items], saved.purchases);
      const recoveredSupplierLedger = journalEntries.flatMap(entry => entry.supplierEntries || []).reduce((items, entry) => items.some(item => item.idempotencyKey && item.idempotencyKey === entry.idempotencyKey) ? items : [entry, ...items], saved.supplierLedger);
      const recoveredStock = journalEntries.flatMap(entry => entry.stockMovements || []).reduce((items, entry) => items.some(item => item.idempotencyKey && item.idempotencyKey === entry.idempotencyKey) ? items : [entry, ...items], saved.stockMovements);
      movementsRef.current = recovered;
      // Hydration and financial-journal recovery intentionally initialize client state after mount.
      if (raw || recovered.length !== initial.treasuryMovements.length || recoveredPurchases.length || recoveredSupplierLedger.length || recoveredStock.length) setState({ ...saved, treasuryMovements: recovered, purchases: recoveredPurchases, supplierLedger: recoveredSupplierLedger, stockMovements: recoveredStock });
    } catch (error) { reportPersistenceIssue({ code:error instanceof PersistenceError?error.code:'CORRUPT', message:error instanceof Error?error.message:'تعذر قراءة بيانات الخزينة المحفوظة.', rawKey:key }); return; }
    setReady(true);
  }, []);
  useEffect(() => { if (ready) { try { ensureBrowserWriter(localStorage); writeJson(localStorage,key,state); } catch(error) { reportPersistenceIssue({code:error instanceof PersistenceError?error.code:'WRITE_FAILED',message:error instanceof Error?error.message:'تعذر حفظ بيانات الخزينة.'}); } } }, [ready, state]);
  useEffect(() => { stateRef.current = state; }, [state]);

  const treasuryBalance = useCallback((accountId: string) => state.treasuryMovements.reduce((sum, m) => {
    if (m.status !== 'posted') return sum;
    return sum + (m.destinationAccountId === accountId ? m.amount : 0) - (m.sourceAccountId === accountId ? m.amount : 0);
  }, 0), [state.treasuryMovements]);
  const supplierBalance = useCallback((supplierId: string) => state.supplierLedger.filter(x => x.supplierId === supplierId).reduce((s, x) => s + x.debit - x.credit, 0), [state.supplierLedger]);
  const stockBalance = useCallback((itemId: string) => state.stockMovements.filter(x => x.inventoryItemId === itemId).reduce((s, x) => s + (x.direction === 'in' ? x.quantityMilliUnits : -x.quantityMilliUnits), 0), [state.stockMovements]);

  const saveCashCount = useCallback((input:{id:string;businessDayId:string;shiftId?:string;rows:{denominationPiastres:number;quantity:number}[];expectedPiastres:number;actor:User}) => {
    assertBrowserWriter(localStorage); authorizeCommand({actor:input.actor,permission:'cash_count'});
    const existing=stateRef.current.cashDenominationCounts.find(item=>item.id===input.id); if(existing)return existing;
    const calculated=calculateDenominationCount(input.rows,input.expectedPiastres);
    const record:CashDenominationCount={id:input.id,businessDayId:input.businessDayId,shiftId:input.shiftId,...calculated,countedBy:input.actor.name,createdAt:now(),finalizedAt:now()};
    setState(s=>({...s,cashDenominationCounts:[record,...s.cashDenominationCounts],controlAuditEvents:[{id:uid('control-audit'),action:'cash_count_finalized',actor:input.actor.name,entityId:record.id,amountPiastres:record.totalPiastres,createdAt:now()},...s.controlAuditEvents]})); return record;
  },[]);

  const completeChecklistItem = useCallback((businessDayId:string,itemId:string,completed:boolean,actor:User) => {
    authorizeCommand({actor,permission:'business_day_close'}); const completion:ChecklistCompletion={businessDayId,itemId,completed,completedAt:completed?now():undefined,completedBy:completed?actor.name:undefined};
    setState(s=>({...s,checklistCompletions:[completion,...s.checklistCompletions.filter(x=>x.businessDayId!==businessDayId||x.itemId!==itemId)]}));
  },[]);

  const setExceptionStatus = useCallback((id:string,status:'acknowledged'|'resolved',reason:string,actor:User,approval?:ManagerApproval) => {
    authorizeCommand({actor,permission:'control_center',approval,requireApproval:true}); if(!reason.trim())throw new Error('سبب إجراء المراجعة مطلوب.');
    setState(s=>({...s,exceptionStates:updateExceptionState(s.exceptionStates,id,status,actor,reason.trim(),now()),controlAuditEvents:[{id:uid('control-audit'),action:status==='resolved'?'exception_resolved':'exception_acknowledged',actor:actor.name,entityId:id,reason:reason.trim(),approvedBy:approval?.approverName,createdAt:now()},...s.controlAuditEvents]}));
  },[]);

  const openBusinessDay = useCallback((input:{requestId:string;businessDate:string;actor:User})=>{assertBrowserWriter(localStorage);authorizeCommand({actor:input.actor,permission:'business_day_open'});const timestamp=now(),day=buildOpenedBusinessDay({...input,timestamp,businessDays:stateRef.current.businessDays});setState(s=>({...s,businessDays:[day,...s.businessDays],controlAuditEvents:[{id:uid('control-audit'),action:'business_day_opened',actor:input.actor.name,entityId:day.id,operationId:`business-open-${input.requestId}`,createdAt:timestamp},...s.controlAuditEvents]}));return day},[]);

  const reopenBusinessDay = useCallback((input:{businessDayId:string;reason:string;actor:User;approval?:ManagerApproval;persistenceHealthy:boolean})=>{assertBrowserWriter(localStorage);const timestamp=now(),day=stateRef.current.businessDays.find(x=>x.id===input.businessDayId);if(!day)throw new Error('يوم العمل غير موجود.');const attempted:ControlAuditEvent={id:uid('control-audit'),action:'business_day_reopen_attempted',actor:input.actor.name,entityId:day.id,reason:input.reason,createdAt:timestamp};try{authorizeCommand({actor:input.actor,permission:'business_day_reopen',approval:input.approval,requireApproval:true});if(!input.persistenceHealthy)throw new Error('لا يمكن إعادة الفتح أثناء وجود مشكلة في التخزين.');const reopened=buildReopenedBusinessDay({day,businessDays:stateRef.current.businessDays,reason:input.reason,actor:input.actor,approval:input.approval,timestamp});setState(s=>({...s,businessDays:s.businessDays.map(x=>x.id===day.id?reopened:x),checklistCompletions:s.checklistCompletions.filter(x=>x.businessDayId!==day.id||!s.checklistItems.some(i=>i.id===x.itemId&&i.phase==='closing')),controlAuditEvents:[{id:uid('control-audit'),action:'business_day_reopened',actor:input.actor.name,entityId:day.id,reason:input.reason,approvedBy:input.approval?.approverName,createdAt:timestamp},attempted,...s.controlAuditEvents]}));return reopened}catch(error){setState(s=>({...s,controlAuditEvents:[{id:uid('control-audit'),action:'business_day_reopen_blocked',actor:input.actor.name,entityId:day.id,reason:error instanceof Error?error.message:input.reason,createdAt:timestamp},attempted,...s.controlAuditEvents]}));throw error}},[]);

  const closeBusinessDay = useCallback((input:{businessDayId:string;actor:User;approval?:ManagerApproval;reconciliation:Record<string,unknown>;shifts:unknown[];journalEntries?:unknown[];persistenceHealthy?:boolean;writerOwned?:boolean;exceptions?:ControlException[]}) => {
    assertBrowserWriter(localStorage); authorizeCommand({actor:input.actor,permission:'business_day_close',approval:input.approval,requireApproval:true});
    const day=stateRef.current.businessDays.find(x=>x.id===input.businessDayId); if(!day)throw new Error('يوم العمل غير موجود.');
    const existing=latestBusinessDaySnapshot(stateRef.current.dailyCloseSnapshots,day.id); if(day.status==='closed'&&existing)return existing;
    assertBusinessDayTransition(day.status,'closing');
    const requiredChecklist=stateRef.current.checklistItems.filter(x=>x.phase==='closing'&&x.active&&x.required).map(item=>({id:item.id,label:item.label,completed:stateRef.current.checklistCompletions.some(c=>c.businessDayId===day.id&&c.itemId===item.id&&c.completed)}));
    const blockers=evaluateCloseBlockers({shifts:input.shifts,deliveryTrips:stateRef.current.deliveryTrips,driverCustodies:stateRef.current.driverCustodies,journalEntries:input.journalEntries,persistenceHealthy:input.persistenceHealthy,writerOwned:input.writerOwned,requiredChecklist,reconciliation:input.reconciliation});
    const started:ControlAuditEvent={id:uid('control-audit'),action:'business_day_close_started',actor:input.actor.name,entityId:day.id,operationId:`business-close-${day.id}-r${(day.closeRevision||0)+1}`,createdAt:now()};
    if(blockers.some(b=>b.severity==='critical')){setState(s=>({...s,controlAuditEvents:[{id:uid('control-audit'),action:'business_day_close_blocked',actor:input.actor.name,entityId:day.id,operationId:started.operationId,blockerCodes:blockers.map(b=>b.code),createdAt:now()},started,...s.controlAuditEvents]}));throw new Error('لا يمكن إغلاق يوم العمل قبل معالجة الموانع المالية الحرجة.')}
    const timestamp=now(), snapshot=createDailyCloseSnapshot({day,actor:input.actor,timestamp,reconciliation:input.reconciliation,blockers,existingSnapshots:stateRef.current.dailyCloseSnapshots,cashCount:stateRef.current.cashDenominationCounts.find(c=>c.businessDayId===day.id),exceptions:input.exceptions,checklist:stateRef.current.checklistCompletions.filter(c=>c.businessDayId===day.id)}); assertBusinessDayTransition('closing','closed');
    setState(s=>({...s,dailyCloseSnapshots:[snapshot,...s.dailyCloseSnapshots.map(x=>x.businessDayId===day.id?{...x,authoritative:false}:x)],businessDays:s.businessDays.map(x=>x.id===day.id?{...x,status:'closed',closeRevision:snapshot.closeRevision,closingStartedAt:timestamp,closingStartedBy:input.actor.name,closedAt:timestamp,closedBy:input.actor.name,snapshotId:snapshot.id,closeOperationId:snapshot.operationId}:x),controlAuditEvents:[{id:uid('control-audit'),action:'business_day_closed',actor:input.actor.name,entityId:day.id,operationId:snapshot.operationId,approvedBy:input.approval?.approverName,createdAt:timestamp},started,...s.controlAuditEvents]})); return snapshot;
  },[]);

  const postTreasuryMovement = useCallback((input: Omit<TreasuryMovement, 'id' | 'createdAt' | 'status'>) => {
    assertBrowserWriter(localStorage);
    const existing = movementsRef.current.find(x => x.idempotencyKey === input.idempotencyKey);
    if (existing) return existing;
    if (!Number.isInteger(input.amount) || input.amount < 0 || (input.amount === 0 && input.type !== 'driver_settlement')) throw new Error('المبلغ يجب أن يكون صحيحًا وأكبر من صفر.');
    const movement: TreasuryMovement = { ...input, id: uid('tm'), createdAt: now(), status: 'posted' };
    const appended = appendMovementIdempotently(movementsRef.current, movement);
    movementsRef.current = appended.movements;
    setState(s => ({ ...s, treasuryMovements: appendMovementIdempotently(s.treasuryMovements, movement).movements }));
    return movement;
  }, []);

  const requireOpenBusinessDay = useCallback(() => {
    const day = state.businessDays.find(item => item.status !== 'closed');
    if (!day) throw new Error('يجب فتح يوم عمل قبل تسجيل الحركة المالية.');
    return day;
  }, [state.businessDays]);

  const postSafeDrop = useCallback((input:{dropId:string;shiftId:string;amountPiastres:number;reason:string;actor:User;approval?:ManagerApproval}) => {
    assertBrowserWriter(localStorage); const idempotencyKey=`safe-drop-${input.dropId}`, existing=movementsRef.current.find(m=>m.idempotencyKey===idempotencyKey); if(existing)return existing;
    authorizeCommand({actor:input.actor,permission:'safe_drop',approval:input.approval,requireApproval:true}); if(!input.reason.trim())throw new Error('سبب الإيداع الآمن مطلوب.');
    assertProtectedSourceBalance(movementsRef.current,'tre-drawer',input.amountPiastres); const day=requireOpenBusinessDay(), operationId=createOperationId('safe-drop');
    startFinancialJournalEntry({operationId,idempotencyKey,kind:'safe_drop'}); const movement=buildSafeDropMovement({movementId:uid('tm'),dropId:input.dropId,operationId,businessDayId:day.id,shiftId:input.shiftId,amountPiastres:input.amountPiastres,reason:input.reason.trim(),actor:input.actor,approval:input.approval,timestamp:now()});
    completeTreasuryJournalEntry(idempotencyKey,movement); const posted=postTreasuryMovement(movement); setState(s=>({...s,controlAuditEvents:s.controlAuditEvents.some(a=>a.operationId===operationId)?s.controlAuditEvents:[{id:uid('control-audit'),action:'safe_drop_posted',actor:input.actor.name,entityId:input.dropId,amountPiastres:input.amountPiastres,operationId,approvedBy:input.approval?.approverName,reason:input.reason.trim(),createdAt:now()},...s.controlAuditEvents]})); return posted;
  },[postTreasuryMovement,requireOpenBusinessDay]);

  const transferTreasury = useCallback((input: TreasuryTransferInput) => {
    const idempotencyKey = `treasury-transfer-${input.requestId}`;
    const existing = movementsRef.current.find(item => item.idempotencyKey === idempotencyKey);
    if (existing) return existing;
    authorizeCommand({ actor: input.actor, permission: 'treasury_manage', approval: input.approval, requireApproval: true });
    if (input.sourceAccountId === input.destinationAccountId) throw new Error('يجب أن يختلف حساب المصدر عن حساب الوجهة.');
    if (!stateRef.current.treasuryAccounts.some(item => item.id === input.sourceAccountId && item.active)
      || !stateRef.current.treasuryAccounts.some(item => item.id === input.destinationAccountId && item.active)) throw new Error('حساب الخزينة المحدد غير صالح.');
    if (!input.reason.trim()) throw new Error('سبب التحويل مطلوب.');
    assertProtectedSourceBalance(movementsRef.current, input.sourceAccountId, input.amountPiastres);
    const day = requireOpenBusinessDay(); const operationId = createOperationId('treasury-transfer');
    startFinancialJournalEntry({ operationId, idempotencyKey, kind: 'treasury_transfer' });
    const movement = buildCashLifecycleMovement({ movementId: uid('tm'), idempotencyKey, operationId, type: 'transfer',
      sourceAccountId: input.sourceAccountId, destinationAccountId: input.destinationAccountId,
      amountPiastres: input.amountPiastres, referenceType: 'treasury_transfer', referenceId: input.requestId,
      businessDayId: day.id, reason: input.reason.trim(), createdBy: input.actor.name,
      approvedBy: input.approval?.approverName, createdAt: now() });
    completeTreasuryJournalEntry(idempotencyKey, movement);
    return postTreasuryMovement(movement);
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const ensureEmployeeDriver = useCallback((employee: {id:string;name:string;phone:string;isActive:boolean}) => {
    const existing = stateRef.current.drivers.find(item => item.employeeId === employee.id);
    if (existing) return existing;
    if (!employee.isActive) throw new Error('لا يمكن تفعيل موظف غير نشط كسائق.');
    const driver: Driver = { id:`driver-employee-${employee.id}`, employeeId:employee.id, name:employee.name, phone:employee.phone, active:true, custodyAccountId:`tre-driver-employee-${employee.id}` };
    stateRef.current = { ...stateRef.current, drivers:[driver,...stateRef.current.drivers] };
    setState(current => ({ ...current, drivers:[driver,...current.drivers] })); return driver;
  }, []);

  const registerDeliveryOrder = useCallback((input: Omit<DeliveryAssignment,'id'|'status'> & {status?:DeliveryAssignment['status']}) => {
    const existing = stateRef.current.deliveries.find(item => item.orderId === input.orderId); if (existing) return existing;
    const timestamp = input.createdAt || now(); const delivery: DeliveryAssignment = { ...input, id:`delivery-${input.orderId}`, status:input.status || 'new', createdAt:timestamp };
    stateRef.current = { ...stateRef.current, deliveries:[delivery,...stateRef.current.deliveries] };
    setState(current => ({ ...current, deliveries:[delivery,...current.deliveries] })); return delivery;
  }, []);

  const markDeliveryReady = useCallback((deliveryId:string,actor:User,approval?:ManagerApproval)=>{authorizeCommand({actor,permission:'delivery_manage',approval,requireApproval:true});const delivery=stateRef.current.deliveries.find(item=>item.id===deliveryId);if(!delivery)throw new Error('طلب الدليفري غير موجود.');assertDeliveryTransition(delivery.status,'ready');const timestamp=now(),updated={...delivery,status:'ready' as const,readyAt:timestamp};setState(current=>({...current,deliveries:current.deliveries.map(item=>item.id===delivery.id?updated:item),deliveryEvents:[{id:uid('delivery-event'),deliveryId:delivery.id,orderId:delivery.orderId,type:'ready',actor:actor.name,approver:approval?.approverName,createdAt:timestamp},...current.deliveryEvents]}));return updated;},[]);

  const assignDelivery = useCallback((input:{requestId:string;deliveryId:string;driverId:string;actor:User;approval?:ManagerApproval}) => {
    authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});
    const delivery=stateRef.current.deliveries.find(item=>item.id===input.deliveryId),driver=stateRef.current.drivers.find(item=>item.id===input.driverId&&item.active);
    if(!delivery||delivery.status!=='ready')throw new Error('يمكن تعيين السائق للطلبات الجاهزة فقط.'); if(!driver)throw new Error('السائق غير نشط أو غير موجود.');
    const timestamp=now(); assertDeliveryTransition(delivery.status,'assigned'); const updated={...delivery,status:'assigned' as const,driverId:driver.id,assignedBy:input.actor.name,assignedAt:timestamp};
    const event:DeliveryEvent={id:`delivery-assignment-${input.requestId}`,deliveryId:delivery.id,orderId:delivery.orderId,driverId:driver.id,type:'assignment',actor:input.actor.name,approver:input.approval?.approverName,createdAt:timestamp};
    setState(current=>({...current,deliveries:current.deliveries.map(item=>item.id===delivery.id?updated:item),deliveryEvents:current.deliveryEvents.some(item=>item.id===event.id)?current.deliveryEvents:[event,...current.deliveryEvents]}));return updated;
  },[]);

  const reassignDelivery = useCallback((input:{requestId:string;deliveryId:string;newDriverId:string;reason:string;actor:User;approval?:ManagerApproval}) => {
    const delivery=stateRef.current.deliveries.find(item=>item.id===input.deliveryId),driver=stateRef.current.drivers.find(item=>item.id===input.newDriverId&&item.active);
    if(!delivery||!delivery.driverId||!driver||!input.reason.trim())throw new Error('بيانات إعادة التعيين غير مكتملة.');
    const afterCustody=['handed_to_driver','out_for_delivery','delivered','failed','returned'].includes(delivery.status);
    authorizeCommand({actor:input.actor,permission:afterCustody?'delivery_override':'delivery_manage',approval:input.approval,requireApproval:true});
    const timestamp=now(),history:DeliveryReassignment={id:`delivery-reassignment-${input.requestId}`,orderId:delivery.orderId,oldDriverId:delivery.driverId,newDriverId:driver.id,reason:input.reason.trim(),changedBy:input.actor.name,changedAt:timestamp};
    const updated={...delivery,driverId:driver.id,assignedBy:input.actor.name,assignedAt:timestamp};
    setState(current=>({...current,deliveries:current.deliveries.map(item=>item.id===delivery.id?updated:item),deliveryReassignments:current.deliveryReassignments.some(item=>item.id===history.id)?current.deliveryReassignments:[history,...current.deliveryReassignments]}));return updated;
  },[]);

  const createTripCommand = useCallback((input:{requestId:string;driverId:string;deliveryIds:string[];actor:User;approval?:ManagerApproval}) => {
    authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});
    const existing=stateRef.current.deliveryTrips.find(item=>item.id===`delivery-trip-${input.requestId}`);if(existing)return existing;
    const deliveries=stateRef.current.deliveries.filter(item=>input.deliveryIds.includes(item.id));
    if(deliveries.length!==input.deliveryIds.length||deliveries.some(item=>item.status!=='assigned'||item.driverId!==input.driverId||item.tripId))throw new Error('كل طلبات الرحلة يجب أن تكون معينة لنفس السائق وغير مرتبطة برحلة أخرى.');
    const trip=buildDeliveryTrip({tripId:`delivery-trip-${input.requestId}`,driverId:input.driverId,orderIds:deliveries.map(item=>item.orderId),createdBy:input.actor.name,createdAt:now()});
    setState(current=>({...current,deliveryTrips:[trip,...current.deliveryTrips],deliveries:current.deliveries.map(item=>input.deliveryIds.includes(item.id)?{...item,tripId:trip.id}:item)}));return trip;
  },[]);

  const handoverDeliveryTrip = useCallback((input:{tripId:string;actor:User;approval?:ManagerApproval}) => {
    authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true}); const trip=stateRef.current.deliveryTrips.find(item=>item.id===input.tripId);if(!trip||trip.status!=='open')throw new Error('الرحلة غير جاهزة للتسليم.');
    const timestamp=now(),deliveries=stateRef.current.deliveries.filter(item=>item.tripId===trip.id);if(!deliveries.length||deliveries.some(item=>item.status!=='assigned'))throw new Error('طلبات الرحلة غير جاهزة للتسليم الفعلي.');
    const custodies=deliveries.map(delivery=>createCashCustody({custodyId:`custody-${trip.id}-${delivery.id}`,tripId:trip.id,delivery:{...delivery,paymentMethod:delivery.paymentMethod||'cash',paymentStatus:delivery.paymentStatus||'paid'},driverId:trip.driverId,createdAt:timestamp})).filter(Boolean).map(item=>({...item,deliveryId:deliveries.find(delivery=>delivery.orderId===item.orderId)?.id})) as DriverCustody[];
    setState(current=>({...current,deliveries:current.deliveries.map(item=>item.tripId===trip.id?{...item,status:'handed_to_driver',handedBy:input.actor.name,handedToDriverAt:timestamp}:item),driverCustodies:custodies.reduce((items,custody)=>items.some(item=>item.id===custody.id)?items:[custody,...items],current.driverCustodies),deliveryEvents:[...deliveries.map(delivery=>({id:`handover-${trip.id}-${delivery.id}`,deliveryId:delivery.id,tripId:trip.id,orderId:delivery.orderId,driverId:trip.driverId,type:'handover',actor:input.actor.name,approver:input.approval?.approverName,amountPiastres:delivery.amountToCollect,createdAt:timestamp})),...current.deliveryEvents]}));return trip;
  },[]);

  const startDeliveryTrip = useCallback((input:{tripId:string;actor:User;approval?:ManagerApproval}) => {authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});const trip=stateRef.current.deliveryTrips.find(item=>item.id===input.tripId);if(!trip||trip.status!=='open')throw new Error('الرحلة غير جاهزة للخروج.');const deliveries=stateRef.current.deliveries.filter(item=>item.tripId===trip.id);if(deliveries.some(item=>item.status!=='handed_to_driver'))throw new Error('يجب تسليم كل الطلبات للسائق أولًا.');const timestamp=now(),updated={...trip,status:'out' as const,startedAt:timestamp};setState(current=>({...current,deliveryTrips:current.deliveryTrips.map(item=>item.id===trip.id?updated:item),deliveries:current.deliveries.map(item=>item.tripId===trip.id?{...item,status:'out_for_delivery',outForDeliveryAt:timestamp}:item),deliveryEvents:[{id:uid('delivery-event'),tripId:trip.id,driverId:trip.driverId,type:'out_for_delivery',actor:input.actor.name,approver:input.approval?.approverName,createdAt:timestamp},...current.deliveryEvents]}));return updated;},[]);

  const recordDeliveryResult = useCallback((input:{deliveryId:string;result:'delivered'|'failed'|'returned';collectedAmountPiastres?:number;reason?:string;notes?:string;actor:User;approval?:ManagerApproval}) => {authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});const delivery=stateRef.current.deliveries.find(item=>item.id===input.deliveryId);if(!delivery)throw new Error('طلب الدليفري غير موجود.');assertDeliveryTransition(delivery.status,input.result);const timestamp=now();let updated:DeliveryAssignment={...delivery,status:input.result};if(input.result==='delivered'){const expected=delivery.paymentMethod==='cash'?delivery.amountToCollect:0,collected=input.collectedAmountPiastres??expected;assertCollectionAmount(expected,collected,input.reason);updated={...updated,deliveredAt:timestamp,collectedAmountPiastres:collected,failureNotes:input.reason};}else if(input.result==='failed'){const allowed=['customer_unreachable','customer_refused','wrong_address','order_problem','driver_problem','other'];if(!input.reason||!allowed.includes(input.reason))throw new Error('سبب فشل التوصيل غير صالح.');updated={...updated,failedAt:timestamp,failureReason:input.reason as DeliveryAssignment['failureReason'],failureNotes:input.notes};}else{if(!input.reason?.trim())throw new Error('سبب المرتجع مطلوب.');updated={...updated,returnedAt:timestamp,returnReason:input.reason};}const event:DeliveryEvent={id:uid('delivery-event'),deliveryId:delivery.id,tripId:delivery.tripId,orderId:delivery.orderId,driverId:delivery.driverId,type:input.result,reason:input.reason,amountPiastres:input.collectedAmountPiastres,actor:input.actor.name,approver:input.approval?.approverName,createdAt:timestamp};setState(current=>({...current,deliveries:current.deliveries.map(item=>item.id===delivery.id?updated:item),driverCustodies:current.driverCustodies.map(item=>item.deliveryId===delivery.id&&input.result==='delivered'?{...item,collectedAmountPiastres:updated.collectedAmountPiastres||0,differencePiastres:(updated.collectedAmountPiastres||0)-item.expectedAmountPiastres,status:'collected'}:item),deliveryEvents:[event,...current.deliveryEvents]}));return updated;},[]);

  const returnDeliveryTrip = useCallback((input:{tripId:string;actor:User;approval?:ManagerApproval})=>{authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});const trip=stateRef.current.deliveryTrips.find(item=>item.id===input.tripId);if(!trip||trip.status!=='out')throw new Error('الرحلة ليست في حالة خروج.');const active=stateRef.current.deliveries.filter(item=>item.tripId===trip.id&&item.status==='out_for_delivery');if(active.length)throw new Error('يجب تسجيل نتيجة كل طلب قبل عودة الرحلة.');const timestamp=now(),updated={...trip,status:'returned' as const,returnedAt:timestamp};setState(current=>({...current,deliveryTrips:current.deliveryTrips.map(item=>item.id===trip.id?updated:item),deliveryEvents:[{id:uid('delivery-event'),tripId:trip.id,driverId:trip.driverId,type:'trip_return',actor:input.actor.name,createdAt:timestamp},...current.deliveryEvents]}));return updated;},[]);

  const settleDeliveryTrip = useCallback((input:{settlementId:string;tripId:string;actualPiastres:number;reason?:string;actor:User;approval?:ManagerApproval}) => {assertBrowserWriter(localStorage);const trip=stateRef.current.deliveryTrips.find(item=>item.id===input.tripId);if(!trip||trip.status!=='returned')throw new Error('يجب تسجيل عودة الرحلة قبل التسوية.');authorizeCommand({actor:input.actor,permission:'delivery_manage',approval:input.approval,requireApproval:true});const idempotencyKey=`driver-settlement-${input.settlementId}`,existingSettlement=stateRef.current.driverSettlements.find(item=>item.idempotencyKey===idempotencyKey),existingMovement=movementsRef.current.find(item=>item.idempotencyKey===idempotencyKey);if(existingSettlement&&existingMovement)return{movement:existingMovement,settlement:existingSettlement};const custodies=stateRef.current.driverCustodies.filter(item=>item.tripId===trip.id),totals=calculateTripSettlement(custodies,input.actualPiastres);if(totals.differencePiastres!==0){if(!input.reason?.trim())throw new Error('فرق تسوية السائق يتطلب سببًا.');authorizeCommand({actor:input.actor,permission:'delivery_settlement_approve',approval:input.approval,requireApproval:true});}const day=requireOpenBusinessDay(),operationId=createOperationId('driver-settlement'),timestamp=now();startFinancialJournalEntry({operationId,idempotencyKey,kind:'driver_settlement'});const movement=buildCashLifecycleMovement({movementId:uid('tm'),idempotencyKey,operationId,type:'driver_settlement',destinationAccountId:'tre-main',amountPiastres:input.actualPiastres,referenceType:'delivery_trip',referenceId:trip.id,businessDayId:day.id,reason:`تسوية رحلة ${trip.id}${input.reason?` - ${input.reason}`:''}`,createdBy:input.actor.name,approvedBy:input.approval?.approverName||input.actor.name,createdAt:timestamp});completeTreasuryJournalEntry(idempotencyKey,movement);const posted=postTreasuryMovement(movement),settlement:DriverSettlement={id:input.settlementId,tripId:trip.id,driverId:trip.driverId,orderIds:trip.orderIds,expectedPiastres:totals.expectedPiastres,actualPiastres:totals.actualPiastres,differencePiastres:totals.differencePiastres,reason:input.reason,approvedBy:input.approval?.approverName||input.actor.name,operationId,idempotencyKey,treasuryMovementId:posted.id,receivedBy:input.actor.name,createdAt:timestamp};setState(current=>({...current,driverSettlements:[settlement,...current.driverSettlements],driverCustodies:current.driverCustodies.map(item=>item.tripId===trip.id?{...item,status:'settled',settledAmountPiastres:item.collectedAmountPiastres,settledAt:timestamp}:item),deliveryTrips:current.deliveryTrips.map(item=>item.id===trip.id?{...item,status:'settled',settledAt:timestamp,settledBy:input.actor.name,expectedCash:totals.expectedPiastres,actualCash:totals.actualPiastres,difference:totals.differencePiastres}:item),deliveries:current.deliveries.map(item=>item.tripId===trip.id&&['delivered','returned','failed'].includes(item.status)?{...item,status:'settled',settledAt:timestamp}:item),deliveryEvents:[{id:uid('delivery-event'),tripId:trip.id,driverId:trip.driverId,type:totals.differencePiastres<0?'settlement_shortage':totals.differencePiastres>0?'settlement_overage':'settlement',amountPiastres:totals.actualPiastres,reason:input.reason,actor:input.actor.name,approver:settlement.approvedBy,operationId,createdAt:timestamp},...current.deliveryEvents]}));return{movement:posted,settlement};},[postTreasuryMovement,requireOpenBusinessDay]);

  const postSaleCollection = useCallback((input: FinancialSalePostingInput) => {
    const day = requireOpenBusinessDay();
    const candidate = buildSaleMovement({
      ...input,
      movementId: uid('tm'),
      businessDayId: day.id,
      createdAt: now(),
    });
    return postTreasuryMovement(candidate);
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const postRefund = useCallback((input: FinancialRefundPostingInput) => {
    const day = requireOpenBusinessDay();
    const candidate = buildRefundMovement({
      ...input,
      movementId: uid('tm'),
      businessDayId: day.id,
      createdAt: now(),
    });
    const accountId = candidate.sourceAccountId as 'tre-drawer' | 'tre-bank' | 'tre-instapay';
    const existing = movementsRef.current.find(item => item.idempotencyKey === `refund-${input.refundRequestId}`);
    if (existing) return existing;
    const currentBalance = movementsRef.current.reduce((sum, movement) => {
      if (movement.status !== 'posted') return sum;
      return sum
        + (movement.destinationAccountId === accountId ? movement.amount : 0)
        - (movement.sourceAccountId === accountId ? movement.amount : 0);
    }, 0);
    if (currentBalance < input.amountPiastres) {
      throw new Error('رصيد حساب الدفع غير كافٍ لتنفيذ المرتجع. راجع الخزينة أولًا.');
    }
    return postTreasuryMovement(candidate);
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const postCashLifecycleMovement = useCallback((input: CashLifecyclePostingInput) => {
    const existing = movementsRef.current.find(item => item.idempotencyKey === input.idempotencyKey);
    if (existing) return existing;
    const day = requireOpenBusinessDay();
    if (input.sourceAccountId) assertProtectedSourceBalance(movementsRef.current, input.sourceAccountId, input.amountPiastres);
    return postTreasuryMovement(buildCashLifecycleMovement({
      ...input,
      movementId: uid('tm'),
      businessDayId: day.id,
      createdAt: now(),
    }));
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const postExternalFunding = useCallback((input: ExternalFundingInput) => {
    const idempotencyKey = `${input.type.replaceAll('_', '-')}-${input.depositId}`;
    const existing = movementsRef.current.find(item => item.idempotencyKey === idempotencyKey);
    if (existing) return existing;
    authorizeCommand({ actor: input.actor, permission: 'treasury_manage', approval: input.approval, requireApproval: true });
    const day = requireOpenBusinessDay();
    startFinancialJournalEntry({operationId:input.operationId,idempotencyKey,kind:input.type});
    const movement=buildCashLifecycleMovement({
      movementId: uid('tm'), idempotencyKey, operationId: input.operationId,
      type: input.type, destinationAccountId: input.destinationAccountId, amountPiastres: input.amountPiastres,
      referenceType: 'external_funding', referenceId: input.reference || input.depositId,
      businessDayId: day.id, reason: input.reason, createdBy: input.createdBy,
      approvedBy: input.approvedBy, createdAt: now(),
    });
    completeTreasuryJournalEntry(idempotencyKey,movement);
    return postTreasuryMovement(movement);
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const postOwnerWithdrawal = useCallback((input: OwnerWithdrawalInput) => {
    const idempotencyKey = `owner-withdrawal-${input.withdrawalId}`;
    const existing = movementsRef.current.find(item => item.idempotencyKey === idempotencyKey);
    if (existing) return existing;
    authorizeCommand({ actor: input.actor, permission: 'treasury_manage', approval: input.approval, requireApproval: true });
    assertProtectedSourceBalance(movementsRef.current, input.sourceAccountId, input.amountPiastres);
    const day = requireOpenBusinessDay();
    startFinancialJournalEntry({operationId:input.operationId,idempotencyKey,kind:'owner_withdrawal'});
    const movement=buildCashLifecycleMovement({
      movementId: uid('tm'), idempotencyKey, operationId: input.operationId, type: 'owner_withdrawal',
      sourceAccountId: input.sourceAccountId, amountPiastres: input.amountPiastres,
      referenceType: 'owner_withdrawal', referenceId: input.reference || input.withdrawalId,
      businessDayId: day.id, reason: input.reason, createdBy: input.createdBy,
      approvedBy: input.approvedBy, createdAt: now(),
    });
    completeTreasuryJournalEntry(idempotencyKey,movement);
    return postTreasuryMovement(movement);
  }, [postTreasuryMovement, requireOpenBusinessDay]);

  const receivePurchase = useCallback((input: PurchaseCommandInput) => {
    const purchaseId = `purchase-${input.requestId}`;
    const existing = stateRef.current.purchases.find(item => item.id === purchaseId || item.idempotencyKey === `purchase-${purchaseId}`);
    if (existing) return existing;
    if (!stateRef.current.suppliers.some(item => item.id === input.supplierId)) throw new Error('المورد المحدد غير موجود.');
    if (!input.lines.length) throw new Error('يجب إضافة صنف واحد على الأقل للفاتورة.');
    const lines = input.lines.map(line => ({ ...line, lineTotal: purchaseLineTotal(line.quantityMilliUnits, line.unitCost) }));
    const total = lines.reduce((sum, line) => sum + (line.lineTotal || 0), 0);
    assertPurchasePayment(input.paid, total);
    const operationId = createOperationId('purchase');
    startFinancialJournalEntry({ operationId, idempotencyKey: `purchase-${purchaseId}`, kind: 'purchase' });
    const accountId = purchasePaymentAccountFor(input.paymentMethod);
    if (input.paid > 0 && !accountId) throw new Error('يجب تحديد حساب دفع فعلي للمبلغ المدفوع.');
    if (accountId) assertProtectedSourceBalance(movementsRef.current, accountId, input.paid);
    const day = requireOpenBusinessDay();
    const paymentMovement = input.paid > 0 && accountId ? buildCashLifecycleMovement({
      movementId: uid('tm'), idempotencyKey: `purchase-payment-${purchaseId}-initial`, operationId,
      type: 'purchase_payment', sourceAccountId: accountId, amountPiastres: input.paid,
      referenceType: 'purchase', referenceId: purchaseId, businessDayId: day.id,
      reason: `دفعة فاتورة ${input.invoiceNumber || purchaseId}`, createdBy: input.createdBy,
      approvedBy: input.approvedBy, createdAt: now(),
    }) : undefined;
    if (paymentMovement) movementsRef.current = appendMovementIdempotently(movementsRef.current, paymentMovement).movements;
    const createdAt = now();
    const paymentStatus: Purchase['paymentStatus'] = input.paid === total ? 'paid' : input.paid > 0 ? 'partially_paid' : 'unpaid';
    const purchase: Purchase = { supplierId: input.supplierId, lines, paid: input.paid, paymentMethod: input.paymentMethod,
      receivedBy: input.receivedBy, notes: input.notes, id: purchaseId, createdAt, total, subtotal: total,
      remainingAmount: total - input.paid, invoiceNumber: input.invoiceNumber, paymentAccountId: accountId,
      status: paymentStatus === 'paid' ? 'paid' : paymentStatus === 'partially_paid' ? 'partially_paid' : 'received',
      paymentStatus, createdBy: input.createdBy, operationId, idempotencyKey: `purchase-${purchaseId}`,
      treasuryMovementId: paymentMovement?.id, payments: paymentMovement && accountId ? [{ id: `payment-${input.requestId}-initial`,
        purchaseId, supplierId: input.supplierId, amount: input.paid, paymentAccountId: accountId,
        treasuryMovementId: paymentMovement.id, operationId, idempotencyKey: paymentMovement.idempotencyKey,
        createdBy: input.createdBy, approvedBy: input.approvedBy, createdAt }] : [] };
    const invoiceEntry: SupplierLedgerEntry = { id: `ledger-invoice-${purchaseId}`, supplierId: input.supplierId,
      type: 'purchase_invoice', debit: total, credit: 0, referenceId: purchaseId, purchaseId,
      reason: `فاتورة مشتريات ${input.invoiceNumber || purchaseId}`, createdAt, createdBy: input.createdBy,
      operationId, idempotencyKey: `purchase-invoice-${purchaseId}` };
    const paymentEntry: SupplierLedgerEntry | undefined = paymentMovement ? { id: `ledger-payment-${paymentMovement.idempotencyKey}`,
      supplierId: input.supplierId, type: 'supplier_payment', debit: 0, credit: input.paid,
      referenceId: purchaseId, purchaseId, reason: 'دفعة عند استلام المشتريات', createdAt, createdBy: input.createdBy,
      operationId, idempotencyKey: paymentMovement.idempotencyKey, treasuryMovementId: paymentMovement.id } : undefined;
    const stock = lines.map(line => ({ id: `stock-${purchaseId}-${line.id}`, inventoryItemId: line.inventoryItemId,
      type: 'purchase_receipt' as const, direction: 'in' as const, quantityMilliUnits: line.quantityMilliUnits,
      unitCost: line.unitCost, referenceId: purchaseId, supplierId: input.supplierId, operationId,
      idempotencyKey: `purchase-stock-${purchaseId}-${line.id}`, reason: `استلام مشتريات ${input.invoiceNumber || purchaseId}`,
      createdBy: input.createdBy, createdAt }));
    completePurchaseJournalEntry(`purchase-${purchaseId}`, { purchase, movement: paymentMovement,
      supplierEntries: [invoiceEntry, ...(paymentEntry ? [paymentEntry] : [])], stockMovements: stock });
    setState(current => ({ ...current, purchases: current.purchases.some(item => item.id === purchaseId) ? current.purchases : [purchase, ...current.purchases],
      treasuryMovements: paymentMovement ? appendMovementIdempotently(current.treasuryMovements, paymentMovement).movements : current.treasuryMovements,
      supplierLedger: [invoiceEntry, ...(paymentEntry ? [paymentEntry] : [])].reduce((entries, entry) => entries.some(item => item.idempotencyKey === entry.idempotencyKey) ? entries : [entry, ...entries], current.supplierLedger),
      stockMovements: stock.reduce((items, movement) => items.some(item => item.idempotencyKey === movement.idempotencyKey) ? items : [movement, ...items], current.stockMovements) }));
    return purchase;
  }, [requireOpenBusinessDay]);

  const payPurchase = useCallback((input: PurchasePaymentCommandInput) => {
    const purchase = stateRef.current.purchases.find(item => item.id === input.purchaseId);
    if (!purchase || purchase.status === 'cancelled') throw new Error('فاتورة الشراء غير متاحة للسداد.');
    const idempotencyKey = `purchase-payment-${purchase.id}-${input.requestId}`;
    const existingMovement = movementsRef.current.find(item => item.idempotencyKey === idempotencyKey);
    if (existingMovement) return purchase;
    const outstanding = supplierOutstandingFromLedger(stateRef.current.supplierLedger, purchase.supplierId, purchase.id);
    assertPurchasePayment(input.amount, outstanding);
    if (input.amount <= 0) throw new Error('مبلغ السداد يجب أن يكون أكبر من صفر.');
    const accountId = purchasePaymentAccountFor(input.paymentMethod);
    if (!accountId) throw new Error('يجب تحديد حساب دفع فعلي.');
    assertProtectedSourceBalance(movementsRef.current, accountId, input.amount);
    const operationId = createOperationId('supplier-payment'); const day = requireOpenBusinessDay(); const createdAt = now();
    startFinancialJournalEntry({ operationId, idempotencyKey, kind: 'purchase_payment' });
    const movement = buildCashLifecycleMovement({ movementId: uid('tm'), idempotencyKey, operationId,
      type: 'purchase_payment', sourceAccountId: accountId, amountPiastres: input.amount,
      referenceType: 'purchase_payment', referenceId: purchase.id, businessDayId: day.id,
      reason: `سداد مورد للفاتورة ${purchase.invoiceNumber || purchase.id}`, createdBy: input.createdBy,
      approvedBy: input.approvedBy, createdAt });
    movementsRef.current = appendMovementIdempotently(movementsRef.current, movement).movements;
    const remaining = outstanding - input.amount; const totalPaid = purchase.paid + input.amount;
    const updated: Purchase = { ...purchase, paid: totalPaid, remainingAmount: remaining,
      paymentStatus: remaining === 0 ? 'paid' : 'partially_paid', status: remaining === 0 ? 'paid' : 'partially_paid',
      payments: [...(purchase.payments || []), { id: `payment-${input.requestId}`, purchaseId: purchase.id,
        supplierId: purchase.supplierId, amount: input.amount, paymentAccountId: accountId,
        treasuryMovementId: movement.id, operationId, idempotencyKey, createdBy: input.createdBy,
        approvedBy: input.approvedBy, createdAt }] };
    const ledger: SupplierLedgerEntry = { id: `ledger-${idempotencyKey}`, supplierId: purchase.supplierId,
      type: 'supplier_payment', debit: 0, credit: input.amount, referenceId: purchase.id, purchaseId: purchase.id,
      reason: 'سداد لاحق لفاتورة مشتريات', createdAt, createdBy: input.createdBy, operationId,
      idempotencyKey, treasuryMovementId: movement.id };
    completePurchaseJournalEntry(idempotencyKey, { purchase: updated, movement, supplierEntries: [ledger] });
    setState(current => ({ ...current, purchases: current.purchases.map(item => item.id === purchase.id ? updated : item),
      treasuryMovements: appendMovementIdempotently(current.treasuryMovements, movement).movements,
      supplierLedger: current.supplierLedger.some(item => item.idempotencyKey === idempotencyKey) ? current.supplierLedger : [ledger, ...current.supplierLedger] }));
    return updated;
  }, [requireOpenBusinessDay]);

  const transitionPayroll = useCallback((id: string, status: PayrollRun['status'], actor: User, approval?: ManagerApproval) => {
    const run=stateRef.current.payrollRuns.find(x=>x.id===id); if(!run) throw new Error('مسير الرواتب غير موجود.');
    assertPayrollTransition(run.status,status);
    if(status==='approved') authorizeCommand({actor,permission:'payroll_approve',approval,requireApproval:true});
    const timestamp=now();
    setState(s=>({...s,payrollRuns:s.payrollRuns.map(x=>x.id===id?{...x,status,approvedBy:status==='approved'?(approval?.approverName||actor.name):x.approvedBy,reviewedBy:status==='reviewed'?actor.name:x.reviewedBy,reviewedAt:status==='reviewed'?timestamp:x.reviewedAt}:x)}));
  },[]);

  const payPayroll = useCallback((input: { payrollRunId:string; paymentAccountId:'tre-main'|'tre-bank'|'tre-instapay'; actor:User; approval?:ManagerApproval; createdBy:string }) => {
    const run=stateRef.current.payrollRuns.find(x=>x.id===input.payrollRunId); if(!run) throw new Error('مسير الرواتب غير موجود.');
    const lines=stateRef.current.payrollLines.filter(x=>x.payrollRunId===run.id);
    const existing=movementsRef.current.find(x=>x.idempotencyKey===`payroll-payment-${run.id}`);
    if(existing){const settlements=buildPayrollSettlements({run,lines,operationId:existing.operationId,movementId:existing.id,paidBy:input.createdBy,timestamp:run.paidAt});return{movement:existing,settlements};}
    if(run.status!=='approved') throw new Error('يجب اعتماد مسير الرواتب قبل الدفع.');
    authorizeCommand({actor:input.actor,permission:'payroll_pay',approval:input.approval,requireApproval:true});
    const total=lines.reduce((sum,line)=>sum+line.netPayable,0); if(!Number.isInteger(total)||total<=0) throw new Error('إجمالي Payroll غير صالح للدفع.');
    assertProtectedSourceBalance(movementsRef.current,input.paymentAccountId,total);
    const day=requireOpenBusinessDay(),operationId=createOperationId('payroll'),idempotencyKey=`payroll-payment-${run.id}`,timestamp=now();
    startFinancialJournalEntry({operationId,idempotencyKey,kind:'payroll_payment'});
    const movement=buildCashLifecycleMovement({movementId:uid('tm'),idempotencyKey,operationId,type:'payroll_payment',sourceAccountId:input.paymentAccountId,amountPiastres:total,referenceType:'payroll',referenceId:run.id,businessDayId:day.id,reason:`صرف Payroll ${run.period}`,createdBy:input.createdBy,approvedBy:input.approval?.approverName||input.actor.name,createdAt:timestamp});
    const paidLines=lines.map(line=>({...line,status:'paid' as const,paidAt:timestamp,settlementEntryId:`payroll-settlement-${run.id}-${line.id}`}));
    const settlements=buildPayrollSettlements({run,lines:paidLines,operationId,movementId:movement.id,paidBy:input.createdBy,timestamp});
    completePayrollJournalEntry(idempotencyKey,movement,settlements);
    movementsRef.current=appendMovementIdempotently(movementsRef.current,movement).movements;
    setState(s=>({...s,treasuryMovements:appendMovementIdempotently(s.treasuryMovements,movement).movements,payrollRuns:s.payrollRuns.map(x=>x.id===run.id?{...x,status:'paid',paidAt:timestamp,operationId,treasuryMovementId:movement.id,paymentAccountId:input.paymentAccountId,paymentMethod:input.paymentAccountId==='tre-main'?'main_treasury':input.paymentAccountId==='tre-bank'?'bank':'instapay'}:x),payrollLines:s.payrollLines.map(x=>paidLines.find(p=>p.id===x.id)||x)}));
    return{movement,settlements};
  },[requireOpenBusinessDay]);

  const value: EnterpriseContextValue = useMemo(() => ({
    ...state, treasuryBalance, supplierBalance, stockBalance, transferTreasury, postSaleCollection, postRefund, postCashLifecycleMovement, postExternalFunding, postOwnerWithdrawal, receivePurchase, payPurchase, transitionPayroll, payPayroll, openBusinessDay, reopenBusinessDay, closeBusinessDay, saveCashCount, completeChecklistItem, setExceptionStatus, postSafeDrop,
    exportState: () => state,
    importState: snapshot => {
      if (!snapshot || typeof snapshot !== 'object') return false;
      const candidate = snapshot as Partial<EnterpriseState>;
      if (!Array.isArray(candidate.treasuryAccounts) || !Array.isArray(candidate.inventoryItems) || !Array.isArray(candidate.businessDays)) return false;
      let imported: EnterpriseState;
      try { imported = normalizeEnterpriseState(candidate, initial) as EnterpriseState; }
      catch { return false; }
      movementsRef.current = imported.treasuryMovements;
      setState(imported);
      return true;
    },
    createHandover: input => setState(s => s.cashHandovers.some(item => input.treasuryMovementId && item.treasuryMovementId === input.treasuryMovementId) ? s : ({ ...s, cashHandovers: [{ ...input, id: uid('handover'), createdAt: now() }, ...s.cashHandovers] })),
    addSupplier: input => setState(s => ({ ...s, suppliers: [{ ...input, id: uid('sup') }, ...s.suppliers] })),
    addSupplierEntry: input => setState(s => ({ ...s, supplierLedger: [{ ...input, id: uid('sl'), createdAt: now() }, ...s.supplierLedger] })),
    addInventoryItem: input => setState(s => ({ ...s, inventoryItems: [{ ...input, id: uid('inv') }, ...s.inventoryItems] })),
    postStockMovement: input => setState(s => ({ ...s, stockMovements: [{ ...input, id: uid('sm'), createdAt: now() }, ...s.stockMovements] })),
    createPayroll: (runInput, lines) => { const run: PayrollRun = { ...runInput, id: uid('payroll'), createdAt: now(), status: 'draft' }; setState(s => ({ ...s, payrollRuns: [run, ...s.payrollRuns], payrollLines: [...lines.map(line => ({ ...line, id: uid('pl'), payrollRunId: run.id, netPayable: line.base + line.overtime + line.extraShifts + line.bonuses - line.deductions - line.advanceDeductions })), ...s.payrollLines] })); },
    setPayrollStatus: (id, status) => { if(status==='approved'||status==='paid') throw new Error('الاعتماد والدفع يتطلبان أمرًا موثقًا واعتمادًا صالحًا.'); setState(s => ({ ...s, payrollRuns: s.payrollRuns.map(x => { if(x.id!==id)return x; assertPayrollTransition(x.status,status); return { ...x, status }; }) })); },
    addLeave: input => setState(s => ({ ...s, leaveRequests: [{ ...input, id: uid('leave'), status: 'pending' }, ...s.leaveRequests] })),
    decideLeave: (id, status, approvedBy) => setState(s => ({ ...s, leaveRequests: s.leaveRequests.map(x => x.id === id ? { ...x, status, approvedBy } : x) })),
    addTaxRule: input => setState(s => ({ ...s, taxRules: [{ ...input, id: uid('tax') }, ...s.taxRules] })),
    updateTaxRule: rule => setState(s => ({ ...s, taxRules: s.taxRules.map(x => x.id === rule.id ? rule : x) })),
    addDriver: input => setState(s => { const id=uid('driver'), accountId=`tre-${id}`; return { ...s, drivers:[{...input,id,custodyAccountId:accountId},...s.drivers], treasuryAccounts:[...s.treasuryAccounts,{id:accountId,name:`عهدة ${input.name}`,type:'driver_custody',active:true}] }; }),
    ensureEmployeeDriver, registerDeliveryOrder, markDeliveryReady, assignDelivery, reassignDelivery, createDeliveryTrip:createTripCommand,
    handoverDeliveryTrip, startDeliveryTrip, recordDeliveryResult, returnDeliveryTrip, settleDeliveryTrip,
    assertShiftDeliverySettled: shiftId => assertShiftHasNoUnsettledCustody(stateRef.current.driverCustodies,stateRef.current.deliveries,shiftId),
    deliveryReport: () => selectDeliveryReport(stateRef.current.deliveries,stateRef.current.deliveryTrips,stateRef.current.driverCustodies,stateRef.current.driverSettlements),
    deliveryExceptions: () => selectDeliveryExceptions(stateRef.current.deliveries,stateRef.current.deliveryTrips,stateRef.current.driverCustodies,stateRef.current.driverSettlements),
    updateTable: (id,status,orderId) => setState(s=>({...s,diningTables:s.diningTables.map(x=>x.id===id?{...x,status,orderId}:x)})),
  }), [state, treasuryBalance, supplierBalance, stockBalance, transferTreasury, postSaleCollection, postRefund, postCashLifecycleMovement, postExternalFunding, postOwnerWithdrawal, receivePurchase, payPurchase, transitionPayroll, payPayroll, openBusinessDay, reopenBusinessDay, closeBusinessDay, saveCashCount, completeChecklistItem, setExceptionStatus, postSafeDrop, ensureEmployeeDriver, registerDeliveryOrder, markDeliveryReady, assignDelivery, reassignDelivery, createTripCommand, handoverDeliveryTrip, startDeliveryTrip, recordDeliveryResult, returnDeliveryTrip, settleDeliveryTrip]);

  return <Context.Provider value={value}>{children}</Context.Provider>;
}

export function useEnterpriseStore() {
  const value = useContext(Context);
  if (!value) throw new Error('useEnterpriseStore must be used inside EnterpriseProvider');
  return value;
}
