export type Money = number; // integer Egyptian piastres; never store floating-point pounds
export type PaymentTreasuryAccountId = 'tre-drawer' | 'tre-bank' | 'tre-instapay';

export type EntityStatus = 'active' | 'inactive' | 'terminated' | 'archived';
export type ApprovalStatus = 'draft' | 'calculated' | 'reviewed' | 'approved' | 'paid' | 'cancelled';

export interface TreasuryAccount {
  id: string;
  name: string;
  type: 'main_safe' | 'cashier_drawer' | 'driver_custody' | 'bank' | 'instapay' | 'other_custody';
  active: boolean;
}

export type TreasuryMovementType =
  | 'sale_collection' | 'expense_payment' | 'supplier_payment' | 'purchase_payment'
  | 'employee_advance' | 'employee_withdrawal' | 'payroll_payment' | 'refund'
  | 'deposit' | 'withdrawal' | 'transfer' | 'driver_settlement'
  | 'owner_withdrawal' | 'owner_deposit' | 'capital_injection' | 'other_external_inflow' | 'cash_adjustment'
  | 'shift_opening_float' | 'cash_in_transfer' | 'cash_out_transfer'
  | 'cash_shortage' | 'cash_overage' | 'shift_handover' | 'safe_drop' | 'other';

export interface TreasuryMovement {
  id: string;
  idempotencyKey: string;
  operationId?: string;
  type: TreasuryMovementType;
  sourceAccountId?: string;
  destinationAccountId?: string;
  amount: Money;
  referenceType?: string;
  referenceId?: string;
  businessDayId: string;
  shiftId?: string;
  reason: string;
  createdBy: string;
  approvedBy?: string;
  createdAt: string;
  reversedMovementId?: string;
  status: 'posted' | 'reversed';
}

export interface CashHandover {
  id: string;
  fromAccountId: string;
  toAccountId: string;
  amount: Money;
  handedBy: string;
  receivedBy: string;
  shiftId?: string;
  notes?: string;
  createdAt: string;
  treasuryMovementId?: string;
}

export interface Supplier {
  id: string;
  name: string;
  phone: string;
  address: string;
  category: string;
  active: boolean;
  notes?: string;
}

export interface SupplierLedgerEntry {
  id: string;
  supplierId: string;
  type: 'purchase_invoice' | 'supplier_payment' | 'supplier_return' | 'payment' | 'return' | 'adjustment';
  debit: Money;
  credit: Money;
  referenceId?: string;
  reason: string;
  createdAt: string;
  createdBy: string;
  operationId?: string;
  idempotencyKey?: string;
  purchaseId?: string;
  treasuryMovementId?: string;
}

export interface PurchaseLine {
  id: string;
  inventoryItemId: string;
  quantityMilliUnits: number;
  unitCost: Money;
  lineTotal?: Money;
}

export interface Purchase {
  id: string;
  supplierId: string;
  lines: PurchaseLine[];
  total: Money;
  paid: Money;
  paymentMethod: 'cash' | 'instapay' | 'bank' | 'credit';
  invoiceNumber?: string;
  subtotal?: Money;
  remainingAmount?: Money;
  paymentAccountId?: 'tre-main' | 'tre-bank' | 'tre-instapay';
  status: 'draft' | 'received' | 'partially_paid' | 'paid' | 'cancelled';
  paymentStatus: 'unpaid' | 'partially_paid' | 'paid';
  receivedBy: string;
  createdAt: string;
  notes?: string;
  createdBy?: string;
  operationId?: string;
  idempotencyKey?: string;
  treasuryMovementId?: string;
  payments?: PurchasePayment[];
}

export interface PurchasePayment {
  id: string; purchaseId: string; supplierId: string; amount: Money;
  paymentAccountId: 'tre-main' | 'tre-bank' | 'tre-instapay';
  treasuryMovementId: string; operationId: string; idempotencyKey: string;
  createdBy: string; approvedBy?: string; createdAt: string;
}

export interface InventoryItem {
  id: string;
  name: string;
  category: string;
  unit: 'kg' | 'gram' | 'liter' | 'piece' | 'box' | 'bag' | 'bottle';
  minimumMilliUnits: number;
  active: boolean;
}

export interface StockMovement {
  id: string;
  inventoryItemId: string;
  type: 'purchase_receipt' | 'manual_receipt' | 'usage' | 'waste' | 'transfer' | 'count_adjustment' | 'supplier_return' | 'other';
  direction: 'in' | 'out';
  quantityMilliUnits: number;
  unitCost?: Money;
  referenceId?: string;
  reason: string;
  createdBy: string;
  approvedBy?: string;
  createdAt: string;
  supplierId?: string;
  operationId?: string;
  idempotencyKey?: string;
}

export interface Recipe {
  id: string;
  productId: string;
  name: string;
  active: boolean;
}

export interface RecipeItem {
  id: string;
  recipeId: string;
  inventoryItemId: string;
  quantityMilliUnits: number;
}

export interface PayrollRun {
  id: string;
  period: string;
  status: ApprovalStatus;
  createdAt: string;
  approvedBy?: string;
  paidAt?: string;
  reviewedAt?: string;
  reviewedBy?: string;
  operationId?: string;
  treasuryMovementId?: string;
  paymentAccountId?: 'tre-main' | 'tre-bank' | 'tre-instapay';
  paymentMethod?: 'main_treasury' | 'bank' | 'instapay';
}

export interface PayrollLine {
  id: string;
  payrollRunId: string;
  employeeId: string;
  base: Money;
  overtime: Money;
  extraShifts: Money;
  bonuses: Money;
  deductions: Money;
  advanceDeductions: Money;
  netPayable: Money;
  status?: 'unpaid' | 'paid';
  paidAt?: string;
  settlementEntryId?: string;
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  type: 'annual' | 'sick' | 'unpaid' | 'other';
  fromDate: string;
  toDate: string;
  reason: string;
  status: 'pending' | 'approved' | 'rejected' | 'cancelled';
  approvedBy?: string;
}

export interface TaxRule {
  id: string;
  name: string;
  code: string;
  rateBasisPoints: number;
  mode: 'inclusive' | 'exclusive';
  enabled: boolean;
  activeFrom: string;
  activeTo?: string;
  productIds: string[];
  categoryIds: string[];
}

export interface BusinessDay {
  id: string;
  date: string;
  businessDate?: string;
  openedAt?: string;
  openedBy?: string;
  status: 'open' | 'closing' | 'closed' | 'reopened';
  closingStartedAt?: string;
  closingStartedBy?: string;
  snapshotId?: string;
  closeOperationId?: string;
  actualTreasury?: Money;
  handedOver?: Money;
  handedBy?: string;
  receivedBy?: string;
  closedAt?: string;
  closedBy?: string;
  reopenReason?: string;
  closeRevision?: number; previousSnapshotId?:string; previousClosedAt?:string; previousClosedBy?:string;
  reopenedAt?:string; reopenedBy?:string; reopenApprovalId?:string; reopenApprovedBy?:string;
}

export interface DailyCloseSnapshot { id:string; snapshotVersion:1; closeRevision:number; authoritative:boolean; previousSnapshotId?:string; businessDayId:string; businessDate:string; generatedAt:string; closedAt:string; closedBy:string; operationId:string; reconciliation:Record<string,unknown>; cashCount?:CashDenominationCount|null; exceptions:ControlException[]; checklist:ChecklistCompletion[]; }
export interface CashDenominationCount { id:string; businessDayId:string; shiftId?:string; rows:{denominationPiastres:number;quantity:number;totalPiastres:number}[]; totalPiastres:Money; expectedPiastres:Money; differencePiastres:Money; countedBy:string; createdAt:string; finalizedAt?:string; }
export interface OperationalChecklistItem { id:string; label:string; phase:'opening'|'closing'; required:boolean; active:boolean; sortOrder:number; }
export interface ChecklistCompletion { itemId:string; businessDayId:string; completed:boolean; completedAt?:string; completedBy?:string; }
export interface ControlException { id:string; category:'financial'|'orders'|'expenses'|'delivery'|'inventory'|'security'|'system'; severity:'info'|'warning'|'critical'; message:string; entityType:string; entityId:string; status:'open'|'acknowledged'|'resolved'; actor?:string; reason?:string; timestamp?:string; }
export interface ControlAuditEvent { id:string; action:'cash_count_finalized'|'safe_drop_posted'|'business_day_close_started'|'business_day_close_blocked'|'business_day_closed'|'business_day_opened'|'business_day_reopen_attempted'|'business_day_reopen_blocked'|'business_day_reopened'|'exception_acknowledged'|'exception_resolved'; actor:string; entityId:string; amountPiastres?:Money; operationId?:string; approvedBy?:string; reason?:string; blockerCodes?:string[]; createdAt:string; }

export interface Driver {
  id: string;
  employeeId?: string;
  name: string;
  phone: string;
  active: boolean;
  custodyAccountId: string;
}

export interface DeliveryAssignment {
  id: string;
  orderId: string;
  driverId: string;
  status: 'new' | 'preparing' | 'ready' | 'assigned' | 'handed_to_driver' | 'out_for_delivery' | 'delivered' | 'failed' | 'returned' | 'settled' | 'cancelled';
  amountToCollect: Money;
  paymentMethod?: 'cash' | 'card' | 'instapay';
  paymentStatus?: 'paid' | 'unpaid';
  customerName?: string; customerPhone?: string; address?: string; landmark?: string; deliveryNotes?: string;
  foodSubtotalPiastres?: Money; deliveryFeePiastres?: Money; orderTotalPiastres?: Money;
  shiftId?: string; orderNumber?: string; tripId?: string; assignedBy?: string;
  createdAt?: string; readyAt?: string; assignedAt?: string; handedToDriverAt?: string; handedBy?: string;
  outForDeliveryAt?: string;
  deliveredAt?: string;
  collectedAmountPiastres?: Money; failedAt?: string; returnedAt?: string;
  settledAt?: string;
  failureReason?: 'customer_unreachable'|'customer_refused'|'wrong_address'|'order_problem'|'driver_problem'|'other';
  failureNotes?: string; returnReason?: string;
}

export interface DeliveryTrip { id:string; driverId:string; orderIds:string[]; status:'open'|'out'|'returned'|'settled'; createdAt:string; startedAt?:string; returnedAt?:string; settledAt?:string; expectedCash?:Money; actualCash?:Money; difference?:Money; createdBy:string; settledBy?:string; }
export interface DriverCustody { id:string; tripId:string; deliveryId:string; driverId:string; orderId:string; expectedAmountPiastres:Money; collectedAmountPiastres:Money; settledAmountPiastres:Money; differencePiastres:Money; status:'open'|'collected'|'settled'; createdAt:string; settledAt?:string; }
export interface DeliveryReassignment { id:string; orderId:string; oldDriverId:string; newDriverId:string; reason:string; changedBy:string; changedAt:string; }
export interface DeliveryEvent { id:string; deliveryId?:string; tripId?:string; orderId?:string; driverId?:string; type:string; reason?:string; actor:string; approver?:string; amountPiastres?:Money; operationId?:string; createdAt:string; }
export interface DriverSettlement { id:string; tripId:string; driverId:string; orderIds:string[]; expectedPiastres:Money; actualPiastres:Money; differencePiastres:Money; reason?:string; approvedBy?:string; operationId:string; idempotencyKey:string; treasuryMovementId?:string; receivedBy:string; createdAt:string; }

export interface DiningTable {
  id: string;
  name: string;
  area: string;
  status: 'available' | 'occupied' | 'reserved' | 'cleaning';
  orderId?: string;
}
