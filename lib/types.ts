export type UserRole = 'owner' | 'manager' | 'cashier' | 'supervisor' | 'grill';

export interface User {
  id: string;
  name: string;
  username: string;
  role: UserRole;
  pin: string;
  active: boolean;
  avatar?: string;
  permissions: string[];
}

export type OrderType = 'dine_in' | 'takeaway' | 'delivery';
export type PaymentMethod = 'cash' | 'card' | 'instapay' | 'multi';
export type OrderStatus = 'pending' | 'preparing' | 'ready' | 'completed' | 'cancelled' | 'refunded';
export type FinancialOperationId = string;

export interface OrderRefund {
  id: string;
  operationId: FinancialOperationId;
  idempotencyKey: string;
  amount: number;
  paymentMethod: Exclude<PaymentMethod, 'multi'>;
  treasuryMovementId: string;
  createdAt: string;
  createdBy: string;
  authorizedBy: string;
  reason: string;
  originalShiftId: string;
  operationalShiftId?: string;
}

export interface ProductVariant {
  id: string;
  name: string; // e.g. 'صمون', 'صاج', 'فرنساوي' or 'صغير', 'وسط', 'كبير'
  price: number;
  recipeId?: string;
}

export interface Product {
  id: string;
  name: string;
  categoryId: string;
  basePrice: number;
  variants: ProductVariant[];
  isAvailable: boolean;
  sendToGrill: boolean; // يرسل إلى الفحم
  notes?: string;
  sortOrder: number;
  popular?: boolean;
  image?: string;
  recipeId?: string;
}

export interface Category {
  id: string;
  name: string;
  sortOrder: number;
  active: boolean;
  icon?: string;
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  variantId?: string;
  variantName?: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  sendToGrill: boolean;
  notes?: string;
}

export interface Order {
  businessDayId?: string;
  id: string;
  orderNumber: string; // e.g. '#0128'
  sequenceNumber: number;
  createdAt: string;
  cashierId: string;
  cashierName: string;
  shiftId: string;
  checkoutRequestId?: string;
  financialOperationId?: FinancialOperationId;
  paymentTreasuryMovementId?: string;
  orderType: OrderType;
  tableNumber?: string;
  customerName?: string;
  customerPhone?: string;
  deliveryAddress?: string;
  deliveryLandmark?: string;
  deliveryNotes?: string;
  deliveryFee?: number;
  items: OrderItem[];
  subtotal: number;
  discount: number;
  discountReason?: string;
  discountAuthorizedBy?: string;
  taxableAmount?: number;
  taxAmount?: number;
  taxRateBasisPoints?: number;
  taxMode?: 'inclusive' | 'exclusive';
  taxRuleName?: string;
  total: number;
  paymentMethod: PaymentMethod;
  splitPayments?: { method: Exclude<PaymentMethod, 'multi'>; amount: number }[];
  amountPaid: number;
  changeDue: number;
  status: OrderStatus;
  isPaid: boolean;
  paidAt?: string;
  cancelledAt?: string;
  cancelReason?: string;
  cancelledBy?: string;
  cancelAuthorizedBy?: string;
  refundedAt?: string;
  refundAmount?: number;
  refundReason?: string;
  refundAuthorizedBy?: string;
  refunds?: OrderRefund[];
  hasGrillItems: boolean;
  grillStatus?: 'pending' | 'preparing' | 'ready';
}

export type CompensationType = 'daily' | 'monthly' | 'hourly';

export interface Employee {
  id: string;
  name: string;
  employeeNumber: string;
  jobTitle: string;
  phone: string;
  hireDate: string;
  compensationType: CompensationType;
  dailyWage: number; // قيمة اليومية
  shiftValue: number; // قيمة الشيفت العادي
  monthlySalary?: number;
  pin: string;
  role: UserRole;
  isActive: boolean;
  notes?: string;
  address?: string;
  nationalId?: string;
  department?: string;
  employmentType?: 'daily' | 'monthly' | 'part_time' | 'contract';
  fingerprintId?: string;
  employmentStatus?: 'active' | 'inactive' | 'terminated' | 'archived';
  overtimeRate?: number;
}

export type LedgerEntryType =
  | 'daily_wage' // يومية
  | 'salary' // مرتب
  | 'advance' // سلفة
  | 'withdrawal' // سحب من الرصيد
  | 'overtime' // ساعات إضافية
  | 'extra_shift' // تطبيق
  | 'deduction' // خصم
  | 'bonus'
  | 'other_earning'
  | 'payroll_settlement'
  | 'adjustment'
  | 'settlement';

export interface EmployeeLedgerEntry {
  id: string;
  employeeId: string;
  date: string;
  time: string;
  type: LedgerEntryType;
  description: string;
  debit: number; // مدين (سلفة / سحب / خصم)
  credit: number; // دائن (يومية / مرتب / تطبيق / إضافي / مكافأة)
  balanceAfter: number; // الرصيد بعد الحركة (قد يكون سالباً)
  recordedBy: string; // المستخدم الذي سجل الحركة
  authorizedBy?: string; // المعتمد بواسطة
  shiftId?: string;
  referenceId?: string;
  hours?: number;
  rateSnapshot?: number;
  valueSnapshot?: number;
  payrollRunId?: string;
  payrollLineId?: string;
  amountPiastres?: number;
  advanceDeduction?: number;
  operationId?: string;
  treasuryMovementId?: string;
  paidBy?: string;
}

export type ShiftStatus = 'open' | 'closing_pending' | 'closed' | 'under_review';

export interface Shift {
  businessDayId?: string;
  id: string;
  shiftName: string; // e.g. 'وردية صباحية', 'وردية ليلية'
  openedByUserId: string;
  openedByUserName: string;
  openedAt: string;
  openingCash: number;
  status: ShiftStatus;
  closedByUserId?: string;
  closedByUserName?: string;
  closedAt?: string;
  actualCash?: number;
  expectedCash?: number;
  cashDifference?: number; // actual - expected
  differenceReason?: string;
  differenceAuthorizedBy?: string;
  totalSales: number;
  cashSales: number;
  cardSales: number;
  instapaySales: number;
  totalExpenses: number;
  totalCashIn: number;
  totalCashOut: number;
  totalRefunds: number;
  totalDiscounts: number;
  ordersCount: number;
  notes?: string;
  financialLifecycleVersion?: 1;
  openingFinancialOperationId?: string;
  openingTreasuryMovementId?: string;
  reconciliationFinancialOperationId?: string;
  reconciliationTreasuryMovementId?: string;
  handoverFinancialOperationId?: string;
  handoverTreasuryMovementId?: string;
}

export type CashMovementType = 'cash_in' | 'cash_out' | 'drawer_open' | 'settlement';

export interface CashMovement {
  id: string;
  date: string;
  time: string;
  type: CashMovementType;
  amount: number;
  reason: string;
  userId: string;
  userName: string;
  shiftId: string;
  authorizedBy?: string;
  operationId?: string;
  treasuryMovementId?: string;
  idempotencyKey?: string;
  sourceAccountId?: string;
  destinationAccountId?: string;
  treasuryPostingMode?: 'posted' | 'linked_non_posting' | 'legacy_operational';
}

export interface Expense {
  businessDayId?: string;
  id: string;
  date: string;
  time: string;
  category: string; // مستلزمات, مواصلات, صيانة, غاز, نظافة, مشتريات عاجلة, أخرى
  amount: number;
  description: string;
  userId: string;
  userName: string;
  shiftId: string;
  paidFromDrawer: boolean;
  paymentAccountId?: 'tre-drawer' | 'tre-main' | 'tre-bank' | 'tre-instapay';
  paymentSource?: 'drawer' | 'main_treasury' | 'bank' | 'instapay' | 'legacy_unspecified';
  financialOperationId?: string;
  receiptNumber?: string;
  operationId?: string;
  treasuryMovementId?: string;
  idempotencyKey?: string;
  treasuryPostingMode?: 'posted' | 'operational_only';
}

export type AttendanceStatus = 'present' | 'late' | 'absent' | 'leave' | 'missing_fingerprint';

export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  checkIn?: string;
  checkOut?: string;
  shiftName: string;
  status: AttendanceStatus;
  lateMinutes: number;
  workHours: number;
  overtimeHours: number;
  tatbiqCount: number;
  isManualCorrection: boolean;
  correctedBy?: string;
  correctionReason?: string;
}

export type AuditSeverity = 'normal' | 'info' | 'warning' | 'critical';

export interface AuditEvent {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  userRole: UserRole;
  action: string;
  category: 'order' | 'cash' | 'shift' | 'employee' | 'menu' | 'auth' | 'settings' | 'attendance' | 'inventory';
  details: string;
  severity: AuditSeverity;
  shiftId?: string;
  device?: string;
  reason?: string;
  authorizedBy?: string;
  previousValue?: string;
  newValue?: string;
  operationId?: FinancialOperationId;
  referenceId?: string;
}

export interface GrillTicket {
  id: string;
  orderId: string;
  orderNumber: string;
  createdAt: string;
  orderType: OrderType;
  items: {
    productId: string;
    productName: string;
    variantName?: string;
    quantity: number;
    notes?: string;
  }[];
  status: 'pending' | 'preparing' | 'ready';
  startedAt?: string;
  readyAt?: string;
  notes?: string;
}

export interface Device {
  id: string;
  name: string;
  type: 'pos' | 'printer' | 'grill' | 'fingerprint' | 'server';
  ipAddress: string;
  status: 'connected' | 'disconnected' | 'warning';
  lastSeen: string;
  details?: string;
}

export interface SystemSettings {
  restaurantName: string;
  subName: string;
  address: string;
  phone1: string;
  phone2: string;
  phoneLandline: string;
  complaintsPhone: string;
  receiptFooter: string;
  overtimeHourlyRate: number; // 25 EGP
  deliveryFee: number;
  currency: string;
  cashDiscrepancyThreshold: number; // 50 EGP trigger manager
  autoLockMinutes: number;
  activeShiftName: string;
  printerWidth: '80mm' | '58mm';
  autoPrintReceipt: boolean;
  autoPrintPreparationTicket: boolean;
  numberOfReceiptCopies: number;
  grillScreenSoundAlert: boolean;
}

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  severity: 'info' | 'warning' | 'danger';
  timestamp: string;
  read: boolean;
  linkModule?: string;
}
