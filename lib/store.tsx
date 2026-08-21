'use client';

import React, { createContext, useContext, useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useEnterpriseStore } from './enterprise-store';
import { calculateTax } from './tax-engine';

import type { ManagerApproval } from './authorization.mjs';
import { authorizeCommand, authorizeShiftClose, issueManagerApproval, verifyUserCredentials } from './authorization.mjs';
import { ensureBrowserWriter, assertBrowserWriter } from './browser-writer.mjs';
import {
  User,
  Category,
  Product,
  ProductVariant,
  Order,
  OrderItem,
  OrderType,
  PaymentMethod,
  Employee,
  EmployeeLedgerEntry,
  Shift,
  GrillTicket,
  Expense,
  CashMovement,
  AttendanceRecord,
  AuditEvent,
  Device,
  SystemSettings,
  NotificationItem,
  LedgerEntryType,
  AttendanceStatus,
  OrderRefund,
} from './types';
import {
  allocateNextOrderSequence,
  assertOrderCanBeCancelled,
  completeFinancialJournalEntry,
  completeTreasuryJournalEntry,
  completedFinancialJournalEntries,
  clampRefundAmount,
  calculateExpectedDrawerCashPiastres,
  createOperationId,
  startFinancialJournalEntry,
  toPiastres,
  fromPiastres,
  discrepancyNeedsApproval,
  refundableRemaining,
  resolveRefundOperationalShift,
} from './financial-posting';
import { CORE_STORAGE_KEY, readJson, writeJson, assertCommittedSnapshot, PersistenceError } from './local-persistence.mjs';
import { reportPersistenceIssue } from './persistence-health';
import { assertUniqueDailyWage } from './payroll-domain.mjs';
import { normalizeCoreState } from './state-normalization.mjs';
import {
  INITIAL_SETTINGS,
  INITIAL_USERS,
  INITIAL_CATEGORIES,
  INITIAL_PRODUCTS,
  INITIAL_EMPLOYEES,
  INITIAL_LEDGER_ENTRIES,
  INITIAL_SHIFTS,
  INITIAL_ORDERS,
  INITIAL_GRILL_TICKETS,
  INITIAL_EXPENSES,
  INITIAL_CASH_MOVEMENTS,
  INITIAL_ATTENDANCE,
  INITIAL_AUDIT_EVENTS,
  INITIAL_DEVICES,
  INITIAL_NOTIFICATIONS,
} from './mock-data';

export interface CartTab {
  id: string;
  name: string;
  cart: OrderItem[];
  orderType: OrderType;
  tableNumber: string;
  customerName: string;
  customerPhone: string;
  deliveryAddress: string;
  discount: number;
  discountReason: string;
}

interface AppContextType {
  // Navigation & User
  activeModule: string;
  setActiveModule: (module: string) => void;
  currentUser: User;
  hasPermission: (permission: string) => boolean;
  users: User[];
  updateUser: (user: User) => void;
  setCurrentUser: (user: User) => void;
  switchUser: (userId: string, pin: string) => boolean;
  isLocked: boolean;
  lockScreen: () => void;
  unlockScreen: (pin: string) => boolean;

  // Settings & Devices
  settings: SystemSettings;
  updateSettings: (newSettings: Partial<SystemSettings>) => void;
  devices: Device[];
  updateDeviceStatus: (deviceId: string, status: 'connected' | 'disconnected' | 'warning') => void;
  notifications: NotificationItem[];
  markNotificationRead: (id: string) => void;

  // Menu & Products
  categories: Category[];
  products: Product[];
  addProduct: (product: Omit<Product, 'id'>) => void;
  updateProduct: (product: Product) => void;
  deleteProduct: (id: string) => void;
  addCategory: (category: Omit<Category, 'id'>) => void;
  updateCategory: (category: Category) => void;
  deleteCategory: (id: string) => void;

  // POS & Cart
  cartTabs: CartTab[];
  activeCartTabId: string;
  switchCartTab: (tabId: string) => void;
  addCartTab: () => void;
  removeCartTab: (tabId: string) => void;
  cart: OrderItem[];
  orderType: OrderType;
  setOrderType: (type: OrderType) => void;
  tableNumber: string;
  setTableNumber: (table: string) => void;
  customerName: string;
  setCustomerName: (name: string) => void;
  customerPhone: string;
  setCustomerPhone: (phone: string) => void;
  deliveryAddress: string;
  setDeliveryAddress: (address: string) => void;
  discount: number;
  discountReason: string;
  setDiscount: (amount: number, reason?: string) => void;
  addToCart: (product: Product, variant?: ProductVariant, notes?: string) => void;
  updateCartItemQty: (itemId: string, delta: number) => void;
  removeCartItem: (itemId: string) => void;
  updateCartItemNotes: (itemId: string, notes: string) => void;
  clearCart: () => void;
  cartSubtotal: number;
  cartTotal: number;
  checkout: (paymentMethod: PaymentMethod, amountPaid: number, checkoutRequestId: string, splitPayments?: { method: Exclude<PaymentMethod, 'multi'>, amount: number }[], customDeliveryFee?: number) => Order;

  // Orders
  orders: Order[];
  cancelPaidOrder: (orderId: string, reason: string, managerName: string, approval?: ManagerApproval) => void;
  refundPaidOrder: (orderId: string, amount: number, reason: string, managerName: string, refundRequestId: string, approval?: ManagerApproval) => OrderRefund;

  // Grill Station
  grillTickets: GrillTicket[];
  updateGrillTicketStatus: (ticketId: string, status: 'pending' | 'preparing' | 'ready') => void;

  // Employees & Ledger
  employees: Employee[];
  addEmployee: (employee: Omit<Employee, 'id'>) => void;
  updateEmployee: (employee: Employee) => void;
  ledgerEntries: EmployeeLedgerEntry[];
  recordPayrollSettlements: (entries: EmployeeLedgerEntry[]) => void;
  calculateEmployeeBalance: (employeeId: string) => number;
  recordDailyWage: (employeeId: string, customWage?: number) => void;
  recordOvertime: (employeeId: string, hours: number, customRate?: number, notes?: string) => void;
  recordExtraShift: (employeeId: string, shiftCount?: number, notes?: string) => void;
  recordAdvance: (employeeId: string, amount: number, reason: string, managerName?: string) => void;
  recordWithdrawal: (employeeId: string, amount: number, reason: string, managerName?: string) => void;
  recordManualLedgerEntry: (
    employeeId: string,
    type: LedgerEntryType,
    amount: number,
    isCredit: boolean,
    description: string,
    managerName?: string
  ) => void;

  // Attendance
  attendance: AttendanceRecord[];
  biometricCheckIn: (employeeId: string) => void;
  biometricCheckOut: (employeeId: string) => void;
  correctAttendance: (
    recordId: string,
    status: AttendanceStatus,
    lateMinutes: number,
    workHours: number,
    overtimeHours: number,
    tatbiqCount: number,
    reason: string,
    managerName: string
  ) => void;

  // Shifts & Blind Closing
  shifts: Shift[];
  activeShift: Shift | null;
  calculateShiftExpectedCash: (shiftId: string) => number;
  openNewShift: (shiftName: string, openingCash: number, requestId?: string) => void;
  closeShiftBlind: (
    actualCashCounted: number,
    reason?: string,
    managerName?: string,
    approval?: ManagerApproval
  ) => { expected: number; actual: number; difference: number };

  // Expenses & Cash movements
  expenses: Expense[];
  addExpense: (category: string, amount: number, description: string, paymentAccountId: 'tre-drawer' | 'tre-main' | 'tre-bank' | 'tre-instapay', requestId?: string, approvedBy?: string, approval?: ManagerApproval) => void;
  cashMovements: CashMovement[];
  addCashMovement: (type: 'cash_in' | 'cash_out' | 'drawer_open' | 'settlement', amount: number, reason: string, requestId?: string, managerName?: string, approval?: ManagerApproval) => void;

  // Audit Log & Control Center
  auditEvents: AuditEvent[];
  logAuditEvent: (event: Omit<AuditEvent, 'id' | 'timestamp'>) => void;

  // Modals & Authorization
  receiptModalOpen: boolean;
  setReceiptModalOpen: (open: boolean) => void;
  activeReceiptOrder: Order | null;
  activeReceiptType: 'customer' | 'grill' | 'both' | 'shift_close';
  activeShiftForReport: Shift | null;
  showReceipt: (order: Order, type?: 'customer' | 'grill' | 'both') => void;
  showShiftClosingReport: (shift: Shift) => void;

  // Manager Authorization Modal
  managerAuthOpen: boolean;
  managerAuthParams: {
    title: string;
    description: string;
    requireReason?: boolean;
    action: string;
    onSuccess: (managerName: string, reason: string, approval: ManagerApproval) => void;
  } | null;
  requestManagerAuth: (
    title: string,
    description: string,
    requireReason: boolean,
    onSuccess: (managerName: string, reason: string, approval: ManagerApproval) => void,
    action?: string
  ) => void;
  closeManagerAuth: () => void;
  verifyManagerPin: (pin: string, action: string, reason: string) => { valid: boolean; managerName?: string; approval?: ManagerApproval; lockedUntil?: number };
}

const AppContext = createContext<AppContextType | null>(null);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const enterprise = useEnterpriseStore();
  const STORAGE_KEY = CORE_STORAGE_KEY;
  const [isHydrated, setIsHydrated] = useState(false);
  const hydrationFailedRef = useRef(false);
  // Navigation & User State
  const [activeModule, setActiveModule] = useState<string>('pos');
  const [users, setUsers] = useState<User[]>(INITIAL_USERS);
  const [currentUser, setCurrentUser] = useState<User>(INITIAL_USERS[2]); // Default Cashier أحمد صبحي
  const [isLocked, setIsLocked] = useState<boolean>(true);

  // Settings & Hardware
  const [settings, setSettings] = useState<SystemSettings>(INITIAL_SETTINGS);
  const [devices, setDevices] = useState<Device[]>(INITIAL_DEVICES);
  const [notifications, setNotifications] = useState<NotificationItem[]>(INITIAL_NOTIFICATIONS);

  // Menu State
  const [categories, setCategories] = useState<Category[]>(INITIAL_CATEGORIES);
  const [products, setProducts] = useState<Product[]>(INITIAL_PRODUCTS);

  // POS State - Tabs
  const [cartTabs, setCartTabs] = useState<CartTab[]>([{
    id: 'tab-1', name: 'طلب 1', cart: [], orderType: 'dine_in', tableNumber: 'صالة 1', customerName: '', customerPhone: '', deliveryAddress: '', discount: 0, discountReason: ''
  }]);
  const [activeCartTabId, setActiveCartTabId] = useState<string>('tab-1');

  const activeTab = useMemo(() => cartTabs.find(t => t.id === activeCartTabId) || cartTabs[0], [cartTabs, activeCartTabId]);
  const cart = activeTab.cart;
  const orderType = activeTab.orderType;
  const tableNumber = activeTab.tableNumber;
  const customerName = activeTab.customerName;
  const customerPhone = activeTab.customerPhone;
  const deliveryAddress = activeTab.deliveryAddress;
  const discount = activeTab.discount;
  const discountReason = activeTab.discountReason;

  const switchCartTab = useCallback((tabId: string) => setActiveCartTabId(tabId), []);
  const addCartTab = useCallback(() => {
    const newId = `tab-${Date.now()}`;
    setCartTabs(prev => [...prev, { id: newId, name: `طلب ${prev.length + 1}`, cart: [], orderType: 'dine_in', tableNumber: 'صالة 1', customerName: '', customerPhone: '', deliveryAddress: '', discount: 0, discountReason: '' }]);
    setActiveCartTabId(newId);
  }, []);
  const removeCartTab = useCallback((tabId: string) => {
    setCartTabs(prev => {
      const filtered = prev.filter(t => t.id !== tabId);
      if (filtered.length === 0) return [{ id: 'tab-1', name: 'طلب 1', cart: [], orderType: 'dine_in', tableNumber: 'صالة 1', customerName: '', customerPhone: '', deliveryAddress: '', discount: 0, discountReason: '' }];
      return filtered;
    });
    setActiveCartTabId(prev => prev === tabId ? 'tab-1' : prev);
  }, []);

  const setCart = useCallback((updater: React.SetStateAction<OrderItem[]>) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, cart: typeof updater === 'function' ? updater(tab.cart) : updater } : tab));
  }, [activeCartTabId]);
  const setOrderType = useCallback((type: OrderType) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, orderType: type } : tab));
  }, [activeCartTabId]);
  const setTableNumber = useCallback((table: string) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, tableNumber: table } : tab));
  }, [activeCartTabId]);
  const setCustomerName = useCallback((name: string) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, customerName: name } : tab));
  }, [activeCartTabId]);
  const setCustomerPhone = useCallback((phone: string) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, customerPhone: phone } : tab));
  }, [activeCartTabId]);
  const setDeliveryAddress = useCallback((address: string) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, deliveryAddress: address } : tab));
  }, [activeCartTabId]);
  const setDiscountState = useCallback((amount: number) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, discount: amount } : tab));
  }, [activeCartTabId]);
  const setDiscountReason = useCallback((reason: string) => {
    setCartTabs(prev => prev.map(tab => tab.id === activeCartTabId ? { ...tab, discountReason: reason } : tab));
  }, [activeCartTabId]);

  // Operational Data State
  const [orders, setOrders] = useState<Order[]>(INITIAL_ORDERS);
  const ordersRef = useRef<Order[]>(INITIAL_ORDERS);
  const lastOrderSequenceRef = useRef<number>(
    INITIAL_ORDERS.reduce((max, order) => Math.max(max, order.sequenceNumber), 127)
  );
  const checkoutRequestsRef = useRef<Map<string, Order>>(new Map());
  const [grillTickets, setGrillTickets] = useState<GrillTicket[]>(INITIAL_GRILL_TICKETS);
  const [employees, setEmployees] = useState<Employee[]>(INITIAL_EMPLOYEES);
  const [ledgerEntries, setLedgerEntries] = useState<EmployeeLedgerEntry[]>(INITIAL_LEDGER_ENTRIES);
  const [shifts, setShifts] = useState<Shift[]>(INITIAL_SHIFTS);
  const [expenses, setExpenses] = useState<Expense[]>(INITIAL_EXPENSES);
  const [cashMovements, setCashMovements] = useState<CashMovement[]>(INITIAL_CASH_MOVEMENTS);
  const [attendance, setAttendance] = useState<AttendanceRecord[]>(INITIAL_ATTENDANCE);
  const [auditEvents, setAuditEvents] = useState<AuditEvent[]>(INITIAL_AUDIT_EVENTS);

  // Print & Receipt Modal State
  const [receiptModalOpen, setReceiptModalOpen] = useState<boolean>(false);
  const [activeReceiptOrder, setActiveReceiptOrder] = useState<Order | null>(null);
  const [activeReceiptType, setActiveReceiptType] = useState<'customer' | 'grill' | 'both' | 'shift_close'>('both');
  const [activeShiftForReport, setActiveShiftForReport] = useState<Shift | null>(null);

  // Manager Authorization Modal State
  const [managerAuthOpen, setManagerAuthOpen] = useState<boolean>(false);
  const [managerAuthParams, setManagerAuthParams] = useState<{
    title: string;
    description: string;
    requireReason?: boolean;
    action: string;
    onSuccess: (managerName: string, reason: string, approval: ManagerApproval) => void;
  } | null>(null);
  const failedAuthRef = useRef({ count: 0, lockedUntil: 0 });

  // Current active shift helper
  const activeShift = useMemo(() => {
    return shifts.find((s) => s.status === 'open') || null;
  }, [shifts]);

  const hasPermission = useCallback(
    (permission: string) =>
      currentUser.role === 'owner' ||
      currentUser.permissions.includes('all') ||
      currentUser.permissions.includes(permission),
    [currentUser]
  );

  // Prototype persistence: keeps all connected screens consistent after refresh.
  useEffect(() => {
    try {
      assertCommittedSnapshot(window.localStorage);
      const persisted = readJson<{version:number;data:any}>(window.localStorage, STORAGE_KEY);
      const raw = persisted.exists ? persisted.raw : null;
      if (raw) {
        const saved = persisted.value;
        if (saved.version !== 2 || !saved.data || typeof saved.data !== 'object') throw new PersistenceError('VERSION_MISMATCH', STORAGE_KEY);
        if (saved.version === 2 && saved.data) {
          const d = normalizeCoreState(saved.data, {
            users:INITIAL_USERS, settings:INITIAL_SETTINGS, devices:INITIAL_DEVICES,
            categories:INITIAL_CATEGORIES, products:INITIAL_PRODUCTS, orders:INITIAL_ORDERS,
            grillTickets:INITIAL_GRILL_TICKETS, employees:INITIAL_EMPLOYEES,
            ledgerEntries:INITIAL_LEDGER_ENTRIES, shifts:INITIAL_SHIFTS,
            expenses:INITIAL_EXPENSES, cashMovements:INITIAL_CASH_MOVEMENTS,
            attendance:INITIAL_ATTENDANCE, auditEvents:INITIAL_AUDIT_EVENTS, currentUserId:null as string|null,
          });
          // Hydration from the browser snapshot intentionally initializes client state after mount.
          // eslint-disable-next-line react-hooks/set-state-in-effect
          if (Array.isArray(d.users)) setUsers(d.users);
          if (d.currentUserId) {
            const savedUser = (d.users || INITIAL_USERS).find((u: User) => u.id === d.currentUserId && u.active);
            if (savedUser) setCurrentUser(savedUser);
          }
          if (d.settings) setSettings({ ...INITIAL_SETTINGS, ...d.settings });
          if (Array.isArray(d.devices)) setDevices(d.devices);
          if (Array.isArray(d.categories)) setCategories(d.categories);
          if (Array.isArray(d.products)) setProducts(d.products);
          if (Array.isArray(d.orders)) {
            const recoveredOrders = completedFinancialJournalEntries().reduce((current, entry) => {
              if (!entry.order) return current;
              return current.some((order: Order) => order.id === entry.order?.id)
                ? current.map((order: Order) => order.id === entry.order?.id ? entry.order as Order : order)
                : [entry.order, ...current];
            }, d.orders as Order[]);
            ordersRef.current = recoveredOrders;
            lastOrderSequenceRef.current = recoveredOrders.reduce(
              (max, order) => Math.max(max, order.sequenceNumber),
              127
            );
            checkoutRequestsRef.current = new Map(
              recoveredOrders
                .filter(order => order.checkoutRequestId)
                .map(order => [order.checkoutRequestId as string, order])
            );
            setOrders(recoveredOrders);
          }
          if (Array.isArray(d.grillTickets)) setGrillTickets(d.grillTickets);
          if (Array.isArray(d.employees)) setEmployees(d.employees);
          if (Array.isArray(d.ledgerEntries)) {
            const recoveredLedger = completedFinancialJournalEntries().flatMap(entry => entry.employeeSettlements || []).reduce((items, entry) => items.some((item: EmployeeLedgerEntry) => item.id === entry.id) ? items : [entry, ...items], d.ledgerEntries as EmployeeLedgerEntry[]);
            setLedgerEntries(recoveredLedger);
          }
          if (Array.isArray(d.shifts)) {
            setShifts(d.shifts.map((shift: Shift) => ({ ...shift, instapaySales: shift.instapaySales || 0 })));
          }
          if (Array.isArray(d.expenses)) setExpenses(d.expenses);
          if (Array.isArray(d.cashMovements)) setCashMovements(d.cashMovements);
          if (Array.isArray(d.attendance)) setAttendance(d.attendance);
          if (Array.isArray(d.auditEvents)) setAuditEvents(d.auditEvents);
        }
      } else {
        const recoveredOrders = completedFinancialJournalEntries().reduce((current, entry) => {
          if (!entry.order || current.some(order => order.id === entry.order?.id)) return current;
          return [entry.order, ...current];
        }, INITIAL_ORDERS as Order[]);
        ordersRef.current = recoveredOrders;
        lastOrderSequenceRef.current = recoveredOrders.reduce(
          (max, order) => Math.max(max, order.sequenceNumber),
          127
        );
        checkoutRequestsRef.current = new Map(
          recoveredOrders.filter(order => order.checkoutRequestId).map(order => [order.checkoutRequestId as string, order])
        );
        if (recoveredOrders.length !== INITIAL_ORDERS.length) setOrders(recoveredOrders);
      }
    } catch (error) {
      hydrationFailedRef.current = true;
      const message = error instanceof Error ? error.message : 'تعذر قراءة بيانات التشغيل المحفوظة.';
      reportPersistenceIssue({ code: error instanceof PersistenceError ? error.code : 'CORRUPT', message, rawKey: STORAGE_KEY });
      return;
    } finally {
      setIsHydrated(true);
    }
  }, [STORAGE_KEY]);

  useEffect(() => {
    if (!isHydrated || hydrationFailedRef.current) return;
    try { ensureBrowserWriter(window.localStorage); }
    catch {
      reportPersistenceIssue({ code: 'LOCK_FAILED', message: 'تعذر الحصول على قفل الكتابة. مثيل آخر نشط.' });
      return;
    }
    const snapshot = {
      version: 2,
      savedAt: new Date().toISOString(),
      data: {
        users,
        currentUserId: currentUser.id,
        settings,
        devices,
        categories,
        products,
        orders,
        grillTickets,
        employees,
        ledgerEntries,
        shifts,
        expenses,
        cashMovements,
        attendance,
        auditEvents,
      },
    };
    try { writeJson(window.localStorage, STORAGE_KEY, snapshot); }
    catch (error) { reportPersistenceIssue({ code:error instanceof PersistenceError?error.code:'WRITE_FAILED', message:error instanceof Error?error.message:'تعذر حفظ البيانات.' }); }
  }, [STORAGE_KEY, isHydrated, users, currentUser.id, settings, devices, categories, products, orders, grillTickets, employees, ledgerEntries, shifts, expenses, cashMovements, attendance, auditEvents]);

  // Lock / Unlock Screen
  const lockScreen = useCallback(() => {
    setIsLocked(true);
  }, []);

  const unlockScreen = useCallback(
    (pin: string) => {
      const matched = users.find((u) => u.pin === pin && u.active);
      if (matched) {
        setCurrentUser(matched);
        setIsLocked(false);
        return true;
      }
      return false;
    },
    [users]
  );

  // Audit Logging
  const logAuditEvent = useCallback(
    (event: Omit<AuditEvent, 'id' | 'timestamp'>) => {
      const now = new Date();
      const timestamp = now.toISOString().replace('T', ' ').slice(0, 19);
      const newEvent: AuditEvent = {
        ...event,
        id: `aud-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
        timestamp,
      };
      setAuditEvents((prev) => [newEvent, ...prev]);
    },
    []
  );

  const switchUser = useCallback(
    (userId: string, pin: string) => {
      const u = verifyUserCredentials(users, userId, pin);
      if (u) {
        const previousUser = currentUser;
        setCurrentUser(u);
        logAuditEvent({ userId: previousUser.id, userName: previousUser.name, userRole: previousUser.role, action: 'تبديل مستخدم', category: 'auth', details: `تبديل من ${previousUser.name} إلى ${u.name}`, severity: 'normal' });
        return true;
      }
      return false;
    },
    [users, currentUser, logAuditEvent]
  );

  const updateUser = useCallback((user: User) => {
    authorizeCommand({ actor: currentUser, permission: 'users_manage' });
    setUsers((prev) => prev.map((item) => item.id === user.id ? user : item));
    setCurrentUser((prev) => prev.id === user.id ? user : prev);
  }, [currentUser]);

  // Deterministic Math: Employee Balance Calculation
  const calculateEmployeeBalance = useCallback(
    (employeeId: string) => {
      const empEntries = ledgerEntries.filter((e) => e.employeeId === employeeId);
      const totalCredits = empEntries.reduce((sum, e) => sum + (e.credit || 0), 0);
      const totalDebits = empEntries.reduce((sum, e) => sum + (e.debit || 0), 0);
      return totalCredits - totalDebits;
    },
    [ledgerEntries]
  );

  // Deterministic Math: Expected Cash in Shift Drawer
  const calculateShiftExpectedCash = useCallback(
    (shiftId: string) => {
      const shift = shifts.find((s) => s.id === shiftId);
      if (!shift) return 0;
      const shiftOrders = orders.filter((o) => o.shiftId === shiftId && o.isPaid && o.status !== 'cancelled');
      const cashSales = shiftOrders
        .filter((o) => o.paymentMethod === 'cash')
        .reduce((sum, o) => sum + (o.total - (o.refundAmount || 0)), 0);

      const shiftExpenses = expenses
        .filter((e) => e.shiftId === shiftId && e.paidFromDrawer)
        .reduce((sum, e) => sum + e.amount, 0);

      const shiftCashIn = cashMovements
        .filter((cm) => cm.shiftId === shiftId && cm.type === 'cash_in' && cm.amount > 0)
        .reduce((sum, cm) => sum + cm.amount, 0);

      const shiftCashOut = cashMovements
        .filter((cm) => cm.shiftId === shiftId && cm.type === 'cash_out')
        .reduce((sum, cm) => sum + cm.amount, 0);
      const safeDrops = enterprise.treasuryMovements
        .filter((movement) => movement.shiftId === shiftId && movement.type === 'safe_drop' && movement.status === 'posted')
        .reduce((sum, movement) => sum + fromPiastres(movement.amount), 0);

      return fromPiastres(calculateExpectedDrawerCashPiastres({
        openingCashPiastres: toPiastres(shift.openingCash),
        cashSalesPiastres: toPiastres(cashSales),
        drawerExpensesPiastres: toPiastres(shiftExpenses),
        cashInPiastres: toPiastres(shiftCashIn),
        cashOutPiastres: toPiastres(shiftCashOut + safeDrops),
      }));
    },
    [shifts, orders, expenses, cashMovements, enterprise.treasuryMovements]
  );

  // Cart Calculations
  const cartSubtotal = useMemo(() => {
    return cart.reduce((sum, item) => sum + item.totalPrice, 0);
  }, [cart]);

  const activeTaxRule = useMemo(
    () => enterprise.taxRules.find((rule) => rule.enabled),
    [enterprise.taxRules]
  );

  const cartTotal = useMemo(() => {
    const beforeTaxPiastres = Math.round(Math.max(0, cartSubtotal - discount) * 100);
    return calculateTax(beforeTaxPiastres, activeTaxRule).final / 100;
  }, [cartSubtotal, discount, activeTaxRule]);

  // Cart Actions
  const addToCart = useCallback((product: Product, variant?: ProductVariant, notes?: string) => {
    setCart((prev) => {
      const unitPrice = variant ? variant.price : product.basePrice;
      const variantId = variant ? variant.id : undefined;
      const variantName = variant ? variant.name : undefined;

      const existingIndex = prev.findIndex(
        (item) => item.productId === product.id && item.variantId === variantId && (item.notes || '') === (notes || '')
      );

      if (existingIndex > -1) {
        const updated = [...prev];
        const item = updated[existingIndex];
        const newQty = item.quantity + 1;
        updated[existingIndex] = {
          ...item,
          quantity: newQty,
          totalPrice: newQty * item.unitPrice,
        };
        return updated;
      } else {
        const newItem: OrderItem = {
          id: `item-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
          productId: product.id,
          productName: product.name,
          variantId,
          variantName,
          unitPrice,
          quantity: 1,
          totalPrice: unitPrice,
          sendToGrill: product.sendToGrill,
          notes,
        };
        return [...prev, newItem];
      }
    });
  }, []);

  const updateCartItemQty = useCallback((itemId: string, delta: number) => {
    setCart((prev) => {
      return prev
        .map((item) => {
          if (item.id === itemId) {
            const newQty = item.quantity + delta;
            if (newQty <= 0) return null;
            return {
              ...item,
              quantity: newQty,
              totalPrice: newQty * item.unitPrice,
            };
          }
          return item;
        })
        .filter(Boolean) as OrderItem[];
    });
  }, []);

  const removeCartItem = useCallback((itemId: string) => {
    setCart((prev) => prev.filter((item) => item.id !== itemId));
  }, []);

  const updateCartItemNotes = useCallback((itemId: string, notes: string) => {
    setCart((prev) =>
      prev.map((item) => (item.id === itemId ? { ...item, notes } : item))
    );
  }, []);

  const clearCart = useCallback(() => {
    setCart([]);
    setDiscountState(0);
    setDiscountReason('');
  }, []);

  const setDiscount = useCallback((amount: number, reason?: string) => {
    setDiscountState(Math.max(0, Math.min(amount, cartSubtotal)));
    if (reason) setDiscountReason(reason);
  }, [cartSubtotal]);

  // Show Receipts
  const showReceipt = useCallback((order: Order, type: 'customer' | 'grill' | 'both' = 'both') => {
    setActiveReceiptOrder(order);
    setActiveReceiptType(type);
    setReceiptModalOpen(true);
  }, []);

  const showShiftClosingReport = useCallback((shift: Shift) => {
    setActiveShiftForReport(shift);
    setActiveReceiptType('shift_close');
    setReceiptModalOpen(true);
  }, []);

  // Manager Auth Requests
  const requestManagerAuth = useCallback(
    (
      title: string,
      description: string,
      requireReason: boolean,
      onSuccess: (managerName: string, reason: string, approval: ManagerApproval) => void,
      action = title
    ) => {
      setManagerAuthParams({
        title,
        description,
        requireReason,
        action,
        onSuccess,
      });
      setManagerAuthOpen(true);
    },
    []
  );

  const closeManagerAuth = useCallback(() => {
    setManagerAuthOpen(false);
    setManagerAuthParams(null);
  }, []);

  const verifyManagerPin = useCallback(
    (pin: string, action: string, reason: string) => {
      const attempt = failedAuthRef.current;
      if (attempt.lockedUntil > Date.now()) return { valid: false, lockedUntil: attempt.lockedUntil };
      const managerUser = users.find(
        (u) => u.pin === pin && u.active && (u.role === 'owner' || u.role === 'manager' || (u.role === 'supervisor' && u.permissions.includes(action)))
      );
      if (managerUser) {
        failedAuthRef.current = { count: 0, lockedUntil: 0 };
        const approval = issueManagerApproval({ approver: managerUser, action, reason });
        return { valid: true, managerName: managerUser.name, approval };
      }
      const count = attempt.count + 1;
      const lockedUntil = count >= 5 ? Date.now() + 60_000 : 0;
      failedAuthRef.current = { count: lockedUntil ? 0 : count, lockedUntil };
      if (lockedUntil) logAuditEvent({ userId: currentUser.id, userName: currentUser.name, userRole: currentUser.role,
        action: 'محاولات PIN إدارية فاشلة متكررة', category: 'auth', details: 'تم إيقاف محاولات الاعتماد لمدة دقيقة.', severity: 'critical' });
      return { valid: false };
    },
    [users, currentUser, logAuditEvent]
  );

  // POS Checkout Workflow (Connected State Engine)
  const checkout = useCallback(
    (paymentMethod: PaymentMethod, amountPaid: number, checkoutRequestId: string, splitPayments?: { method: Exclude<PaymentMethod, 'multi'>, amount: number }[], customDeliveryFee?: number) => {
      assertBrowserWriter(window.localStorage);
      if (!checkoutRequestId.trim()) {
        throw new Error('تعذر تحديد طلب الدفع. أعد فتح شاشة الدفع وحاول مرة أخرى.');
      }
      const completedRequest = checkoutRequestsRef.current.get(checkoutRequestId)
        || ordersRef.current.find(order => order.checkoutRequestId === checkoutRequestId);
      if (completedRequest) return completedRequest;
      if (cart.length === 0) {
        throw new Error('السلة فارغة. الرجاء إضافة أصناف قبل الدفع.');
      }
      if (!activeShift) {
        throw new Error('يجب فتح وردية قبل تسجيل أي عملية بيع.');
      }
      if (!hasPermission('pos')) {
        throw new Error('ليس لديك صلاحية تسجيل عملية بيع.');
      }
      if (paymentMethod === 'multi' && (!splitPayments || splitPayments.length === 0)) {
        throw new Error('يجب تحديد تفاصيل الدفع المتعدد.');
      }
      const deliveryFee = orderType === 'delivery' ? (customDeliveryFee !== undefined ? customDeliveryFee : settings.deliveryFee) : 0;
      const payableTotal = cartTotal + deliveryFee;
      const taxCalculation = calculateTax(
        Math.round(Math.max(0, cartSubtotal - discount) * 100),
        activeTaxRule
      );
      if (orderType === 'delivery' && (!customerPhone.trim() || !deliveryAddress.trim())) {
        throw new Error('رقم الهاتف والعنوان مطلوبان لطلب الدليفري.');
      }
      const cashOnDelivery = orderType === 'delivery' && paymentMethod === 'cash';
      if (!cashOnDelivery && amountPaid < payableTotal) {
        throw new Error('المبلغ المدفوع أقل من إجمالي الطلب.');
      }

      const existingMax = ordersRef.current.reduce((max, order) => Math.max(max, order.sequenceNumber), 127);
      const seq = allocateNextOrderSequence(lastOrderSequenceRef.current, existingMax);
      lastOrderSequenceRef.current = seq;
      const orderNumber = `#${String(seq).padStart(4, '0')}`;
      const operationId = createOperationId('sale');
      const orderId = `ord-${operationId}`;
      const now = new Date();
      const timeStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(
        now.getDate()
      ).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      const grillItems = cart.filter((item) => item.sendToGrill);
      const hasGrill = grillItems.length > 0;

      const changeDue = Math.max(0, amountPaid - payableTotal);

      const orderDraft: Order = {
        id: orderId,
        orderNumber,
        sequenceNumber: seq,
        createdAt: timeStr,
        cashierId: currentUser.id,
        cashierName: currentUser.name,
        shiftId: activeShift.id,
        checkoutRequestId,
        financialOperationId: operationId,
        orderType,
        tableNumber: orderType === 'dine_in' ? tableNumber : undefined,
        customerName: customerName || undefined,
        customerPhone: customerPhone || undefined,
        deliveryAddress: orderType === 'delivery' ? deliveryAddress : undefined,
        deliveryFee,
        items: [...cart],
        subtotal: cartSubtotal,
        discount,
        discountReason: discount > 0 ? discountReason : undefined,
        taxableAmount: taxCalculation.taxable / 100,
        taxAmount: taxCalculation.tax / 100,
        taxRateBasisPoints: activeTaxRule?.rateBasisPoints,
        taxMode: activeTaxRule?.mode,
        taxRuleName: activeTaxRule?.name,
        total: payableTotal,
        paymentMethod,
        splitPayments,
        amountPaid,
        changeDue,
        status: hasGrill ? 'preparing' : 'completed',
        isPaid: !cashOnDelivery,
        paidAt: cashOnDelivery ? undefined : timeStr,
        hasGrillItems: hasGrill,
        grillStatus: hasGrill ? 'pending' : undefined,
      };

      if (!cashOnDelivery) startFinancialJournalEntry({ operationId, idempotencyKey: `sale-${checkoutRequestId}`, kind: 'sale' });
      let paymentMovements: any[] = [];
      if (!cashOnDelivery) {
        if (paymentMethod === 'multi' && splitPayments) {
          paymentMovements = splitPayments.map((sp, idx) => enterprise.postSaleCollection({
            operationId,
            checkoutRequestId: `${checkoutRequestId}-${idx}`,
            orderId,
            shiftId: activeShift.id,
            paymentMethod: sp.method,
            amountPiastres: toPiastres(sp.amount),
            createdBy: currentUser.name,
          }));
        } else {
          paymentMovements = [enterprise.postSaleCollection({
            operationId,
            checkoutRequestId,
            orderId,
            shiftId: activeShift.id,
            paymentMethod: paymentMethod as Exclude<PaymentMethod, 'multi'>,
            amountPiastres: toPiastres(payableTotal),
            createdBy: currentUser.name,
          })];
        }
      }

      const primaryMovement = paymentMovements[0];
      const effectiveOperationId = primaryMovement?.operationId || operationId;
      const newOrder: Order = {
        ...orderDraft,
        businessDayId: enterprise.businessDays.find(day=>day.status!=='closed')?.id,
        id: primaryMovement?.referenceId || orderId,
        financialOperationId: primaryMovement ? effectiveOperationId : undefined,
        paymentTreasuryMovementId: primaryMovement?.id,
      };

      for (const movement of paymentMovements) {
        if (movement) {
          try {
            completeFinancialJournalEntry(`sale-${checkoutRequestId}`, movement, newOrder);
          } catch {}
        }
      }

      // 1. Deduct inventory based on recipes (Food Costing)
      for (const item of newOrder.items) {
        const targetRecipeId = item.variant?.recipeId || products.find(p => p.id === item.productId)?.recipeId;
        if (targetRecipeId) {
          const recipe = enterprise.recipes.find(r => r.id === targetRecipeId && r.active);
          if (recipe) {
            const rItems = enterprise.recipeItems.filter(ri => ri.recipeId === recipe.id);
            for (const rItem of rItems) {
              enterprise.postStockMovement({
                inventoryItemId: rItem.inventoryItemId,
                type: 'usage',
                direction: 'out',
                quantityMilliUnits: rItem.quantityMilliUnits * item.quantity,
                reason: `طلب ${newOrder.orderNumber}`,
                createdBy: currentUser.name,
                operationId: effectiveOperationId,
                idempotencyKey: `usage-${newOrder.id}-${item.id}-${rItem.inventoryItemId}`,
              });
            }
          }
        }
      }

      // 2. Add order to order history
      ordersRef.current = [newOrder, ...ordersRef.current];
      checkoutRequestsRef.current.set(checkoutRequestId, newOrder);
      setOrders((prev) => [newOrder, ...prev]);
      if (newOrder.orderType === 'delivery') enterprise.registerDeliveryOrder({
        orderId:newOrder.id, driverId:'', amountToCollect:toPiastres(cashOnDelivery ? newOrder.total : 0),
        paymentMethod:newOrder.paymentMethod as 'cash'|'card'|'instapay', paymentStatus:cashOnDelivery?'unpaid':'paid',
        customerName:newOrder.customerName, customerPhone:newOrder.customerPhone, address:newOrder.deliveryAddress,
        landmark:newOrder.deliveryLandmark, deliveryNotes:newOrder.deliveryNotes, foodSubtotalPiastres:toPiastres(newOrder.subtotal-newOrder.discount),
        deliveryFeePiastres:toPiastres(newOrder.deliveryFee||0), orderTotalPiastres:toPiastres(newOrder.total), shiftId:newOrder.shiftId,
        orderNumber:newOrder.orderNumber, createdAt:newOrder.createdAt, status:newOrder.hasGrillItems?'preparing':'ready', readyAt:newOrder.hasGrillItems?undefined:newOrder.createdAt,
      });

      // 3. If grill items exist, route to Grill Station KDS queue
      if (hasGrill) {
        const newGrillTicket: GrillTicket = {
          id: `gt-${newOrder.id}`,
          orderId: newOrder.id,
          orderNumber,
          createdAt: timeStr,
          orderType,
          items: grillItems.map((item) => ({
            productId: item.productId,
            productName: item.productName,
            variantName: item.variantName,
            quantity: item.quantity,
            notes: item.notes,
          })),
          status: 'pending',
          notes: orderType === 'dine_in' ? tableNumber : customerName,
        };
        setGrillTickets((prev) => [newGrillTicket, ...prev]);
      }

      // 3. Update Shift Totals in real-time
      setShifts((prev) =>
          prev.map((s) => {
            if (s.id === activeShift.id) {
              return {
                ...s,
                totalSales: s.totalSales + newOrder.total,
                cashSales: paymentMethod === 'cash' ? s.cashSales + newOrder.total : s.cashSales,
                cardSales: paymentMethod === 'card' ? s.cardSales + newOrder.total : s.cardSales,
                instapaySales: paymentMethod === 'instapay' ? s.instapaySales + newOrder.total : s.instapaySales,
                totalDiscounts: s.totalDiscounts + newOrder.discount,
                ordersCount: s.ordersCount + 1,
              };
            }
            return s;
          })
        );

      // 4. Log Audit Event
      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تحصيل ودفع طلب ${orderNumber}`,
        category: 'order',
        details: `تم تحصيل ${newOrder.total} ج.م ${paymentMethod === 'cash' ? 'نقدًا إلى درج الكاشير' : paymentMethod === 'instapay' ? 'إلى حساب InstaPay' : 'إلى الحساب البنكي'} للطلب ${orderNumber}. طلب ${orderType === 'dine_in' ? 'صالة' : orderType === 'takeaway' ? 'تيك أواي' : 'دليفري'}${
          hasGrill ? ' (تم إرساله لمحطة الفحم)' : ''
        }`,
        severity: 'normal',
        shiftId: activeShift?.id,
        device: 'كاشير رئيسي - POS 1',
        operationId: effectiveOperationId,
        referenceId: newOrder.id,
      });

      // 5. Trigger dual print modal
      showReceipt(newOrder, 'both');

      // 6. Clear Cart
      clearCart();

      return newOrder;
    },
    [
      cart,
      cartTotal,
      cartSubtotal,
      discount,
      discountReason,
      orderType,
      tableNumber,
      customerName,
      customerPhone,
      deliveryAddress,
      currentUser,
      activeShift,
      hasPermission,
      settings.deliveryFee,
      activeTaxRule,
      logAuditEvent,
      showReceipt,
      clearCart,
      enterprise,
    ]
  );

  // Update Grill Ticket Status
  const updateGrillTicketStatus = useCallback(
    (ticketId: string, status: 'pending' | 'preparing' | 'ready') => {
      const now = new Date();
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      setGrillTickets((prev) =>
        prev.map((ticket) => {
          if (ticket.id === ticketId) {
            return {
              ...ticket,
              status,
              startedAt: status === 'preparing' ? timeStr : ticket.startedAt,
              readyAt: status === 'ready' ? timeStr : ticket.readyAt,
            };
          }
          return ticket;
        })
      );

      // Sync with master order
      const targetTicket = grillTickets.find((t) => t.id === ticketId);
      if (targetTicket) {
        setOrders((prev) =>
          prev.map((order) => {
            if (order.id === targetTicket.orderId) {
              return {
                ...order,
                grillStatus: status,
                status: status === 'ready' ? 'ready' : order.status,
              };
            }
            return order;
          })
        );

        logAuditEvent({
          userId: currentUser.id,
          userName: currentUser.name,
          userRole: currentUser.role,
          action: `تحديث حالة طلب الفحم ${targetTicket.orderNumber}`,
          category: 'order',
          details: `تم تغيير حالة تجهيز المشويات إلى: ${status === 'preparing' ? 'قيد التحضير' : 'جاهز للتقديم'}`,
          severity: 'normal',
          device: 'شاشة الفحم - Grill KDS',
        });
      }
    },
    [grillTickets, currentUser, logAuditEvent]
  );

  // Cancel Paid Order (Manager Protected)
  const cancelPaidOrder = useCallback(
    (orderId: string, reason: string, managerName: string, approval?: ManagerApproval) => {
      void approval;
      throw new Error('لا يمكن إلغاء طلب مدفوع مباشرة. استخدم الاسترجاع المالي للحفاظ على تطابق الخزينة.');
      /* paid cancellation intentionally blocked; historical implementation retained below for migration safety */
      const now = new Date();
      const timeStr = now.toISOString().replace('T', ' ').slice(0, 19);

      const targetOrder = ordersRef.current.find((o) => o.id === orderId) as Order;
      if (!targetOrder || targetOrder.status === 'cancelled' || targetOrder.status === 'refunded') return;
      assertOrderCanBeCancelled(targetOrder.refundAmount || 0);
      ordersRef.current = ordersRef.current.map(order => order.id === orderId ? {
        ...order,
        status: 'cancelled',
        cancelledAt: timeStr,
        cancelReason: reason,
        cancelledBy: currentUser.name,
        cancelAuthorizedBy: managerName,
      } : order);
      setOrders((prev) =>
        prev.map((order) => {
          if (order.id === orderId) {
            return {
              ...order,
              status: 'cancelled',
              cancelledAt: timeStr,
              cancelReason: reason,
              cancelledBy: currentUser.name,
              cancelAuthorizedBy: managerName,
            };
          }
          return order;
        })
      );

      // Reverse Shift Totals
      setGrillTickets((prev) => prev.filter((ticket) => ticket.orderId !== orderId));
      if (targetOrder && activeShift) {
        setShifts((prev) =>
          prev.map((s) => {
            if (s.id === targetOrder.shiftId) {
              return {
                ...s,
                totalSales: Math.max(0, s.totalSales - targetOrder.total),
                cashSales:
                  targetOrder.paymentMethod === 'cash'
                    ? Math.max(0, s.cashSales - targetOrder.total)
                    : s.cashSales,
                cardSales:
                  targetOrder.paymentMethod === 'card'
                    ? Math.max(0, s.cardSales - targetOrder.total)
                    : s.cardSales,
                instapaySales:
                  targetOrder.paymentMethod === 'instapay'
                    ? Math.max(0, s.instapaySales - targetOrder.total)
                    : s.instapaySales,
              };
            }
            return s;
          })
        );
      }

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `إلغاء أوردر مدفوع ${targetOrder?.orderNumber || orderId}`,
        category: 'order',
        details: `تم إلغاء أوردر بقيمة ${targetOrder?.total || 0} ج.م - السبب: ${reason} - اعتماد: ${managerName}`,
        severity: 'critical',
        reason,
        authorizedBy: managerName,
        shiftId: targetOrder?.shiftId,
      });
    },
    [activeShift, currentUser, logAuditEvent]
  );

  // Refund Paid Order (Manager Protected)
  const refundPaidOrder = useCallback(
    (orderId: string, amount: number, reason: string, managerName: string, refundRequestId: string, approval?: ManagerApproval): OrderRefund => {
      if (!refundRequestId.trim()) throw new Error('تعذر تحديد طلب المرتجع. حاول مرة أخرى.');
      const existingRefund = ordersRef.current
        .flatMap(order => order.refunds || [])
        .find(refund => refund.idempotencyKey === `refund-${refundRequestId}`);
      if (existingRefund) return existingRefund;
      const authorization = authorizeCommand({ actor: currentUser, permission: 'orders_refund', approval, requireApproval: true });
      const verifiedManagerName = authorization.approver?.approverName || currentUser.name;

      const targetOrder = ordersRef.current.find((o) => o.id === orderId);
      if (!targetOrder) throw new Error('الطلب غير موجود.');
      if (targetOrder.status === 'cancelled') throw new Error('لا يمكن استرجاع طلب ملغي.');
      if (targetOrder.paymentMethod === 'multi') {
        throw new Error('مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.');
      }
      const alreadyRefunded = targetOrder.refundAmount || 0;
      const refundable = refundableRemaining(targetOrder.total, alreadyRefunded);
      const safeAmount = clampRefundAmount(amount, refundable);
      if (safeAmount <= 0) throw new Error('لا يوجد مبلغ متبقٍ للاسترجاع.');
      const originalShift = shifts.find(shift => shift.id === targetOrder.shiftId);
      const shiftPolicy = resolveRefundOperationalShift({
        originalShiftId: targetOrder.shiftId,
        originalShiftStatus: originalShift?.status,
        activeShiftId: activeShift?.id,
        paymentMethod: targetOrder.paymentMethod,
      });
      const originalShiftClosed = shiftPolicy.historicalImmutable;
      const operationalShiftId = shiftPolicy.operationalShiftId;
      const operationId = createOperationId('refund');
      const now = new Date();
      const timeStr = now.toISOString().replace('T', ' ').slice(0, 19);

      startFinancialJournalEntry({
        operationId,
        idempotencyKey: `refund-${refundRequestId}`,
        kind: 'refund',
      });
      const refundMovement = enterprise.postRefund({
        operationId,
        refundRequestId,
        orderId: targetOrder.id,
        originalShiftId: targetOrder.shiftId,
        operationalShiftId,
        paymentMethod: targetOrder.paymentMethod,
        amountPiastres: toPiastres(safeAmount),
        createdBy: currentUser.name,
        approvedBy: verifiedManagerName,
        reason,
      });
      const effectiveOperationId = refundMovement.operationId || operationId;
      const refundRecord: OrderRefund = {
        id: `order-refund-${effectiveOperationId}`,
        operationId: effectiveOperationId,
        idempotencyKey: `refund-${refundRequestId}`,
        amount: safeAmount,
        paymentMethod: targetOrder.paymentMethod,
        treasuryMovementId: refundMovement.id,
        createdAt: timeStr,
        createdBy: currentUser.name,
        authorizedBy: verifiedManagerName,
        reason,
        originalShiftId: targetOrder.shiftId,
        operationalShiftId,
      };
      const updatedOrder: Order = {
        ...targetOrder,
        status: safeAmount === refundable ? 'refunded' : targetOrder.status,
        refundedAt: timeStr,
        refundAmount: alreadyRefunded + safeAmount,
        refundReason: reason,
        refundAuthorizedBy: verifiedManagerName,
        refunds: [...(targetOrder.refunds || []), refundRecord],
      };
      try {
        completeFinancialJournalEntry(`refund-${refundRequestId}`, refundMovement, updatedOrder);
      } catch {
        // Do not abort after Treasury accepted the refund; commit the matching order state.
      }
      ordersRef.current = ordersRef.current.map(order => order.id === orderId ? updatedOrder : order);
      setOrders((prev) => prev.map(order => order.id === orderId ? updatedOrder : order));

      // Closed historical shifts remain immutable. A cash refund from a closed shift
      // is represented as cash-out in the currently open operational shift so the
      // transitional expected-cash calculation remains correct without using Treasury twice.
      if (operationalShiftId) {
        setShifts((prev) =>
          prev.map((s) => {
            if (s.id === operationalShiftId) {
              return {
                ...s,
                totalRefunds: s.totalRefunds + safeAmount,
                totalCashOut:
                  originalShiftClosed && targetOrder.paymentMethod === 'cash'
                    ? s.totalCashOut + safeAmount
                    : s.totalCashOut,
              };
            }
            return s;
          })
        );
      }
      if (originalShiftClosed && targetOrder.paymentMethod === 'cash' && operationalShiftId) {
        const refundCashMovement: CashMovement = {
          id: `cm-refund-${effectiveOperationId}`,
          date: now.toISOString().slice(0, 10),
          time: timeStr.slice(11, 16),
          type: 'cash_out',
          amount: safeAmount,
          reason: `مرتجع الطلب ${targetOrder.orderNumber}: ${reason}`,
          userId: currentUser.id,
          userName: currentUser.name,
          shiftId: operationalShiftId,
          operationId: effectiveOperationId,
          treasuryMovementId: refundMovement.id,
          idempotencyKey: refundMovement.idempotencyKey,
          sourceAccountId: 'tre-drawer',
          treasuryPostingMode: 'linked_non_posting',
        };
        setCashMovements(prev => prev.some(item => item.id === refundCashMovement.id) ? prev : [refundCashMovement, ...prev]);
      }

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `إرجاع مالي لأوردر ${targetOrder?.orderNumber || orderId}`,
        category: 'order',
        details: `تم استرجاع ${safeAmount} ج.م ${targetOrder.paymentMethod === 'cash' ? 'نقدًا من درج الكاشير' : targetOrder.paymentMethod === 'card' ? 'من الحساب البنكي' : 'من حساب InstaPay'} للطلب ${targetOrder.orderNumber} - السبب: ${reason} - اعتماد: ${managerName}`,
        severity: 'critical',
        reason,
        authorizedBy: managerName,
        shiftId: operationalShiftId,
        operationId: effectiveOperationId,
        referenceId: targetOrder.id,
      });
      return refundRecord;
    },
    [shifts, activeShift, currentUser, logAuditEvent, enterprise]
  );

  // Employee Management Actions
  const addEmployee = useCallback((employee: Omit<Employee, 'id'>) => {
    const newEmp: Employee = {
      ...employee,
      id: `emp-${Date.now()}`,
    };
    setEmployees((prev) => [...prev, newEmp]);
  }, []);

  const updateEmployee = useCallback((employee: Employee) => {
    setEmployees((prev) => prev.map((e) => (e.id === employee.id ? employee : e)));
  }, []);

  // Employee Financial Ledger Actions
  const recordManualLedgerEntry = useCallback(
    (
      employeeId: string,
      type: LedgerEntryType,
      amount: number,
      isCredit: boolean,
      description: string,
      managerName?: string,
      metadata?: Partial<EmployeeLedgerEntry>
    ) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp || !Number.isFinite(amount) || amount <= 0) return;

      const currentBal = calculateEmployeeBalance(employeeId);
      const debit = !isCredit ? amount : 0;
      const credit = isCredit ? amount : 0;
      const newBal = currentBal + credit - debit;

      const now = new Date();
      const dateStr = now.toISOString().slice(0, 10);
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      const newEntry: EmployeeLedgerEntry = {
        id: `led-${Date.now()}`,
        employeeId,
        date: dateStr,
        time: timeStr,
        type,
        description,
        debit,
        credit,
        balanceAfter: newBal,
        recordedBy: currentUser.name,
        authorizedBy: managerName,
        shiftId: activeShift?.id,
        ...metadata,
      };

      setLedgerEntries((prev) => [newEntry, ...prev]);

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تسجيل حركة كشف حساب للموظف ${emp.name}`,
        category: 'employee',
        details: `${description} بمبلغ ${amount} ج.م — الرصيد السابق: ${currentBal} ج.م، الرصيد الجديد: ${newBal} ج.م`,
        severity: type === 'advance' || newBal < 0 ? 'warning' : 'normal',
        authorizedBy: managerName,
      });
    },
    [employees, calculateEmployeeBalance, currentUser, activeShift, logAuditEvent]
  );

  // 1. Daily Wage (يومية)
  const recordDailyWage = useCallback(
    (employeeId: string, customWage?: number) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp) return;
      const wage = customWage !== undefined ? customWage : emp.dailyWage;
      assertUniqueDailyWage(ledgerEntries, { employeeId, date: new Date().toISOString().slice(0, 10), shiftId: activeShift?.id });
      recordManualLedgerEntry(
        employeeId,
        'daily_wage',
        wage,
        true,
        `تسجيل يومية عمل (${wage} ج.م)`, undefined, { valueSnapshot: wage }
      );
    },
    [employees, ledgerEntries, activeShift?.id, recordManualLedgerEntry]
  );

  // 2. Overtime (ساعات إضافية × 25 ج/ساعة)
  const recordOvertime = useCallback(
    (employeeId: string, hours: number, customRate?: number, notes?: string) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp) return;
      const rate = customRate !== undefined ? customRate : settings.overtimeHourlyRate; // Default 25 EGP
      const totalAmount = hours * rate;

      recordManualLedgerEntry(
        employeeId,
        'overtime',
        totalAmount,
        true,
        `ساعات إضافية (${hours} ساعة × ${rate} ج/ساعة = ${totalAmount} ج.م)${notes ? ` - ${notes}` : ''}`, undefined, { hours, rateSnapshot: rate }
      );
    },
    [employees, settings.overtimeHourlyRate, recordManualLedgerEntry]
  );

  // 3. Extra Shift (تطبيق = normal shift value)
  const recordExtraShift = useCallback(
    (employeeId: string, shiftCount: number = 1, notes?: string) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp) return;
      const shiftVal = emp.shiftValue || emp.dailyWage;
      const totalAmount = shiftCount * shiftVal;

      recordManualLedgerEntry(
        employeeId,
        'extra_shift',
        totalAmount,
        true,
        `تطبيق وردية إضافية (${shiftCount} شيفت × ${shiftVal} ج = ${totalAmount} ج.م)${notes ? ` - ${notes}` : ''}`, undefined, { valueSnapshot: shiftVal }
      );
    },
    [employees, recordManualLedgerEntry]
  );

  const recordEmployeeCashOut = useCallback(
    (employeeId: string, amount: number, reason: string) => {
      if (!activeShift) return;
      const emp = employees.find((e) => e.id === employeeId);
      const now = new Date();
      setCashMovements((prev) => [{
        id: `cm-employee-${Date.now()}`,
        date: now.toISOString().slice(0, 10),
        time: `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`,
        type: 'cash_out',
        amount,
        reason: `${emp?.name || 'موظف'} — ${reason}`,
        userId: currentUser.id,
        userName: currentUser.name,
        shiftId: activeShift.id,
      }, ...prev]);
      setShifts((prev) => prev.map((shift) => shift.id === activeShift.id
        ? { ...shift, totalCashOut: shift.totalCashOut + amount }
        : shift));
    },
    [activeShift, employees, currentUser]
  );

  // 4. Employee Advance (سلفة - Allows Negative Balance)
  const recordAdvance = useCallback(
    (employeeId: string, amount: number, reason: string, managerName?: string) => {
      if (!activeShift) throw new Error('يجب فتح وردية قبل صرف سلفة.');
      recordManualLedgerEntry(
        employeeId,
        'advance',
        amount,
        false,
        `سلفة نقدية عاجلة: ${reason}`,
        managerName
      );
      recordEmployeeCashOut(employeeId, amount, `سلفة موظف: ${reason}`);
    },
    [activeShift, recordManualLedgerEntry, recordEmployeeCashOut]
  );

  // 5. Employee Withdrawal (سحب من الرصيد)
  const recordWithdrawal = useCallback(
    (employeeId: string, amount: number, reason: string, managerName?: string) => {
      if (!activeShift) throw new Error('يجب فتح وردية قبل سحب مبلغ من رصيد موظف.');
      if (amount > calculateEmployeeBalance(employeeId)) return;
      recordManualLedgerEntry(
        employeeId,
        'withdrawal',
        amount,
        false,
        `سحب نقدي من الرصيد: ${reason}`,
        managerName
      );
      recordEmployeeCashOut(employeeId, amount, `سحب موظف من رصيده: ${reason}`);
    },
    [activeShift, recordManualLedgerEntry, recordEmployeeCashOut, calculateEmployeeBalance]
  );

  // Attendance Actions (Simulated Fingerprint + Manual Correct)
  const biometricCheckIn = useCallback(
    (employeeId: string) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp) return;
      const now = new Date();
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
      const dateStr = now.toISOString().slice(0, 10);

      const existing = attendance.find((a) => a.employeeId === employeeId && a.date === dateStr);
      if (existing) {
        setAttendance((prev) =>
          prev.map((a) => (a.id === existing.id ? { ...a, checkIn: timeStr, status: 'present' } : a))
        );
      } else {
        const newRecord: AttendanceRecord = {
          id: `att-${Date.now()}`,
          employeeId,
          employeeName: emp.name,
          date: dateStr,
          checkIn: timeStr,
          shiftName: activeShift?.shiftName || 'وردية صباحية',
          status: 'present',
          lateMinutes: 0,
          workHours: 8,
          overtimeHours: 0,
          tatbiqCount: 0,
          isManualCorrection: false,
        };
        setAttendance((prev) => [newRecord, ...prev]);
      }

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تسجيل بصمة حضور للموظف ${emp.name}`,
        category: 'attendance',
        details: `تم تسجيل بصمة الحضور في تمام الساعة ${timeStr}`,
        severity: 'normal',
        device: 'جهاز البصمة Biometric',
      });
    },
    [employees, attendance, activeShift, currentUser, logAuditEvent]
  );

  const biometricCheckOut = useCallback(
    (employeeId: string) => {
      const emp = employees.find((e) => e.id === employeeId);
      if (!emp) return;
      const now = new Date();
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
      const dateStr = now.toISOString().slice(0, 10);

      const mockLateMins = Math.floor(Math.random() * 30); // Mock up to 30 mins late
      const mockOvertime = Math.random() > 0.5 ? Math.floor(Math.random() * 3) + 1 : 0; // 50% chance of 1-3 hrs overtime

      setAttendance((prev) =>
        prev.map((a) =>
          a.employeeId === employeeId && a.date === dateStr ? { 
            ...a, 
            checkOut: timeStr, 
            lateMinutes: a.lateMinutes || mockLateMins, 
            overtimeHours: mockOvertime,
            workHours: 8 + mockOvertime 
          } : a
        )
      );

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تسجيل بصمة انصراف للموظف ${emp.name}`,
        category: 'attendance',
        details: `تم تسجيل بصمة الانصراف في تمام الساعة ${timeStr}`,
        severity: 'normal',
        device: 'جهاز البصمة Biometric',
      });
    },
    [employees, currentUser, logAuditEvent]
  );

  const correctAttendance = useCallback(
    (
      recordId: string,
      status: AttendanceStatus,
      lateMinutes: number,
      workHours: number,
      overtimeHours: number,
      tatbiqCount: number,
      reason: string,
      managerName: string
    ) => {
      setAttendance((prev) =>
        prev.map((a) => {
          if (a.id === recordId) {
            return {
              ...a,
              status,
              lateMinutes,
              workHours,
              overtimeHours,
              tatbiqCount,
              isManualCorrection: true,
              correctedBy: managerName,
              correctionReason: reason,
            };
          }
          return a;
        })
      );

      const target = attendance.find((a) => a.id === recordId);

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تعديل يدوي لحضور الموظف ${target?.employeeName || ''}`,
        category: 'attendance',
        details: `تعديل الحالة إلى: ${status}، تأخير: ${lateMinutes} دقيقة، إضافي: ${overtimeHours} س - السبب: ${reason}`,
        severity: 'warning',
        reason,
        authorizedBy: managerName,
      });
    },
    [attendance, currentUser, logAuditEvent]
  );

  // Expenses Actions
  const addExpense = useCallback(
    (category: string, amount: number, description: string, paymentAccountId: 'tre-drawer' | 'tre-main' | 'tre-bank' | 'tre-instapay', requestId = createOperationId('expense-request'), approvedBy?: string, approval?: ManagerApproval) => {
      authorizeCommand({ actor: currentUser, permission: 'expenses_add', approval, requireApproval: true });
      const paidFromDrawer = paymentAccountId === 'tre-drawer';
      if (!activeShift) throw new Error('يجب فتح وردية قبل تسجيل مصروف.');
      const now = new Date();
      const dateStr = now.toISOString().slice(0, 10);
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      const expenseId = `exp-${requestId}`;
      const operationId = createOperationId('expense');
      const idempotencyKey = `expense-${expenseId}`;
      startFinancialJournalEntry({ operationId, idempotencyKey, kind: 'expense' });
      const treasuryMovement = enterprise.postCashLifecycleMovement({
        operationId, idempotencyKey, type: 'expense_payment', sourceAccountId: paymentAccountId,
        amountPiastres: toPiastres(amount), referenceType: 'expense', referenceId: expenseId,
        shiftId: activeShift.id, reason: description, createdBy: currentUser.name, approvedBy,
      });
      completeTreasuryJournalEntry(idempotencyKey, treasuryMovement);
      const newExpense: Expense = {
        id: expenseId,
        businessDayId: enterprise.businessDays.find(day=>day.status!=='closed')?.id,
        date: dateStr,
        time: timeStr,
        category,
        amount,
        description,
        userId: currentUser.id,
        userName: currentUser.name,
        shiftId: activeShift.id,
        paidFromDrawer,
        paymentAccountId,
        paymentSource: paidFromDrawer ? 'drawer' : paymentAccountId === 'tre-main' ? 'main_treasury' : paymentAccountId === 'tre-bank' ? 'bank' : 'instapay',
        financialOperationId: operationId,
        operationId,
        idempotencyKey,
        treasuryMovementId: treasuryMovement?.id,
        treasuryPostingMode: 'posted',
      };

      setExpenses((prev) => prev.some(item => item.id === expenseId) ? prev : [newExpense, ...prev]);

      if (activeShift && paidFromDrawer) {
        setShifts((prev) =>
          prev.map((s) =>
            s.id === activeShift.id ? { ...s, totalExpenses: s.totalExpenses + amount } : s
          )
        );
      }

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تسجيل مصروف ${category}`,
        category: 'cash',
        details: `مصروف بقيمة ${amount} ج.م - البيان: ${description} - حساب الدفع: ${paymentAccountId}`,
        severity: 'normal',
        shiftId: activeShift?.id,
        operationId,
        referenceId: expenseId,
      });
    },
    [currentUser, activeShift, logAuditEvent, enterprise]
  );

  // Cash Movements Actions
  const addCashMovement = useCallback(
    (type: 'cash_in' | 'cash_out' | 'drawer_open' | 'settlement', amount: number, reason: string, requestId = createOperationId('cash-request'), managerName?: string, approval?: ManagerApproval) => {
      authorizeCommand({ actor: currentUser, permission: 'cash_drawer', approval, requireApproval: true });
      if (!activeShift) throw new Error('يجب فتح وردية قبل تسجيل حركة خزنة.');
      const now = new Date();
      const dateStr = now.toISOString().slice(0, 10);
      const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      const movementId = `cm-${requestId}`;
      const operationId = createOperationId('cash');
      const idempotencyKey = `${type.replace('_', '-')}-${movementId}`;
      if (type === 'cash_in' || type === 'cash_out') startFinancialJournalEntry({ operationId, idempotencyKey, kind: type });
      const financial = type === 'cash_in' || type === 'cash_out' ? enterprise.postCashLifecycleMovement({
        operationId,
        idempotencyKey,
        type: type === 'cash_in' ? 'cash_in_transfer' : 'cash_out_transfer',
        sourceAccountId: type === 'cash_in' ? 'tre-main' : 'tre-drawer',
        destinationAccountId: type === 'cash_in' ? 'tre-drawer' : 'tre-main',
        amountPiastres: toPiastres(amount), referenceType: 'cash_movement', referenceId: movementId,
        shiftId: activeShift.id, reason, createdBy: currentUser.name, approvedBy: managerName,
      }) : undefined;
      if (financial) completeTreasuryJournalEntry(idempotencyKey, financial);
      const newMovement: CashMovement = {
        id: movementId,
        date: dateStr,
        time: timeStr,
        type,
        amount,
        reason,
        userId: currentUser.id,
        userName: currentUser.name,
        shiftId: activeShift.id,
        authorizedBy: managerName,
        operationId,
        treasuryMovementId: financial?.id,
        idempotencyKey: financial?.idempotencyKey,
        sourceAccountId: financial?.sourceAccountId,
        destinationAccountId: financial?.destinationAccountId,
        treasuryPostingMode: financial ? 'posted' : 'legacy_operational',
      };

      setCashMovements((prev) => prev.some(item => item.id === movementId) ? prev : [newMovement, ...prev]);
      if (type === 'cash_in' || type === 'cash_out') {
        setShifts((prev) => prev.map((shift) => shift.id === activeShift.id ? {
          ...shift,
          totalCashIn: type === 'cash_in' ? shift.totalCashIn + amount : shift.totalCashIn,
          totalCashOut: type === 'cash_out' ? shift.totalCashOut + amount : shift.totalCashOut,
        } : shift));
      }

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action:
          type === 'drawer_open'
            ? 'فتح درج النقدية'
            : type === 'cash_in'
            ? 'إيداع نقدية في الدرج'
            : 'سحب نقدية من الدرج',
        category: 'cash',
        details: `${reason} - المبلغ: ${amount} ج.م`,
        severity: type === 'drawer_open' ? 'warning' : 'normal',
        shiftId: activeShift?.id,
        operationId,
        referenceId: movementId,
      });
    },
    [currentUser, activeShift, logAuditEvent, enterprise]
  );

  // Shifts & Blind Closing Workflow
  const openNewShift = useCallback(
    (shiftName: string, openingCash: number, requestId = createOperationId('shift-request')) => {
      authorizeCommand({ actor: currentUser, permission: 'shifts_manage' });
      if (activeShift) throw new Error('توجد وردية مفتوحة بالفعل. أغلقها قبل فتح وردية جديدة.');
      const now = new Date();
      const timeStr = now.toISOString().replace('T', ' ').slice(0, 16);
      const shiftId = `shift-${requestId}`;
      const operationId = createOperationId('shift-open');
      if (openingCash > 0) startFinancialJournalEntry({ operationId, idempotencyKey: `shift-open-${shiftId}`, kind: 'shift_open' });
      const treasuryMovement = openingCash > 0 ? enterprise.postCashLifecycleMovement({
        operationId, idempotencyKey: `shift-open-${shiftId}`, type: 'shift_opening_float',
        sourceAccountId: 'tre-main', destinationAccountId: 'tre-drawer', amountPiastres: toPiastres(openingCash),
        referenceType: 'shift_opening', referenceId: shiftId, shiftId,
        reason: `عهدة افتتاح ${shiftName}`, createdBy: currentUser.name,
      }) : undefined;
      if (treasuryMovement) completeTreasuryJournalEntry(`shift-open-${shiftId}`, treasuryMovement);
      const newShift: Shift = {
        id: shiftId,
        businessDayId: enterprise.businessDays.find(day=>day.status!=='closed')?.id,
        shiftName,
        openedByUserId: currentUser.id,
        openedByUserName: currentUser.name,
        openedAt: timeStr,
        openingCash,
        status: 'open',
        totalSales: 0,
        cashSales: 0,
        cardSales: 0,
        instapaySales: 0,
        totalExpenses: 0,
        totalCashIn: 0,
        totalCashOut: 0,
        totalRefunds: 0,
        totalDiscounts: 0,
        ordersCount: 0,
        financialLifecycleVersion: 1,
        openingFinancialOperationId: operationId,
        openingTreasuryMovementId: treasuryMovement?.id,
      };

      setShifts((prev) => [newShift, ...prev]);

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `افتتاح ${shiftName}`,
        category: 'shift',
        details: `تم فتح وردية جديدة برصيد افتتاح درج: ${openingCash} ج.م`,
        severity: 'normal',
        operationId,
        referenceId: shiftId,
      });
    },
    [activeShift, currentUser, logAuditEvent, enterprise]
  );

  // Crucial Feature: BLIND SHIFT CASH CLOSING
  const closeShiftBlind = useCallback(
    (actualCashCounted: number, reason?: string, managerName?: string, approval?: ManagerApproval) => {
      if (!activeShift) {
        throw new Error('لا توجد وردية مفتوحة حالياً للتقفيل.');
      }
      enterprise.assertShiftDeliverySettled(activeShift.id);

      const expected = calculateShiftExpectedCash(activeShift.id);
      const difference = actualCashCounted - expected; // Negative = عجز, Positive = زيادة
      const actualPiastres = toPiastres(actualCashCounted);
      const expectedPiastres = toPiastres(expected);
      const differencePiastres = actualPiastres - expectedPiastres;
      if (actualPiastres < 0) throw new Error('لا يمكن أن يكون العد الفعلي سالبًا.');
      const requiresDiscrepancyApproval = discrepancyNeedsApproval(differencePiastres, toPiastres(settings.cashDiscrepancyThreshold));
      if (requiresDiscrepancyApproval && (!managerName || !reason?.trim())) {
        throw new Error('الفارق يتجاوز الحد المسموح ويتطلب اعتماد مدير وسببًا واضحًا.');
      }
      authorizeShiftClose({ actor: currentUser, approval, discrepancyNeedsManager: requiresDiscrepancyApproval });
      const reconciliationOperationId = createOperationId('shift-reconcile');
      const handoverOperationId = createOperationId('shift-handover');
      if (activeShift.financialLifecycleVersion === 1 && differencePiastres !== 0) startFinancialJournalEntry({
        operationId: reconciliationOperationId, idempotencyKey: `shift-reconcile-${activeShift.id}`, kind: 'reconciliation',
      });
      const reconciliationMovement = activeShift.financialLifecycleVersion === 1 && differencePiastres !== 0
        ? enterprise.postCashLifecycleMovement({
          operationId: reconciliationOperationId, idempotencyKey: `shift-reconcile-${activeShift.id}`,
          type: differencePiastres < 0 ? 'cash_shortage' : 'cash_overage',
          sourceAccountId: differencePiastres < 0 ? 'tre-drawer' : undefined,
          destinationAccountId: differencePiastres > 0 ? 'tre-drawer' : undefined,
          amountPiastres: Math.abs(differencePiastres), referenceType: 'shift_reconciliation', referenceId: activeShift.id,
          shiftId: activeShift.id, reason: reason?.trim() || 'فرق نقدية ضمن الحد المسموح',
          createdBy: currentUser.name, approvedBy: managerName,
        }) : undefined;
      if (reconciliationMovement) completeTreasuryJournalEntry(`shift-reconcile-${activeShift.id}`, reconciliationMovement);
      if (activeShift.financialLifecycleVersion === 1 && actualPiastres > 0) startFinancialJournalEntry({
        operationId: handoverOperationId, idempotencyKey: `shift-close-handover-${activeShift.id}`, kind: 'handover',
      });
      const handoverMovement = activeShift.financialLifecycleVersion === 1 && actualPiastres > 0
        ? enterprise.postCashLifecycleMovement({
          operationId: handoverOperationId, idempotencyKey: `shift-close-handover-${activeShift.id}`,
          type: 'shift_handover', sourceAccountId: 'tre-drawer', destinationAccountId: 'tre-main',
          amountPiastres: actualPiastres, referenceType: 'shift_handover', referenceId: activeShift.id,
          shiftId: activeShift.id, reason: `تسليم إغلاق ${activeShift.shiftName}`,
          createdBy: currentUser.name, approvedBy: managerName,
        }) : undefined;
      if (handoverMovement) completeTreasuryJournalEntry(`shift-close-handover-${activeShift.id}`, handoverMovement);
      if (handoverMovement) enterprise.createHandover({
        fromAccountId: 'tre-drawer', toAccountId: 'tre-main', amount: actualPiastres,
        handedBy: currentUser.name, receivedBy: managerName || currentUser.name, shiftId: activeShift.id,
        treasuryMovementId: handoverMovement.id, notes: reason,
      });

      const now = new Date();
      const timeStr = now.toISOString().replace('T', ' ').slice(0, 16);

      const updatedShift: Shift = {
        ...activeShift,
        status: 'closed',
        closedByUserId: currentUser.id,
        closedByUserName: currentUser.name,
        closedAt: timeStr,
        actualCash: actualCashCounted,
        expectedCash: expected,
        cashDifference: difference,
        differenceReason: reason,
        differenceAuthorizedBy: managerName,
        reconciliationFinancialOperationId: reconciliationMovement ? reconciliationOperationId : undefined,
        reconciliationTreasuryMovementId: reconciliationMovement?.id,
        handoverFinancialOperationId: handoverMovement ? handoverOperationId : undefined,
        handoverTreasuryMovementId: handoverMovement?.id,
      };

      setShifts((prev) => prev.map((s) => (s.id === activeShift.id ? updatedShift : s)));

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تقفيل الوردية (${activeShift.shiftName}) - تقفيل أعمى`,
        category: 'shift',
        details: `المتوقع: ${expected} ج.م، الفعلي المعدود: ${actualCashCounted} ج.م، الفارق: ${difference} ج.م (${
          difference === 0 ? 'مطابق' : difference < 0 ? `عجز ${Math.abs(difference)} ج` : `زيادة ${difference} ج`
        })${reason ? ` - السبب: ${reason}` : ''}`,
        severity: discrepancyNeedsApproval(differencePiastres, toPiastres(settings.cashDiscrepancyThreshold)) ? 'critical' : 'normal',
        shiftId: activeShift.id,
        reason,
        authorizedBy: managerName,
        operationId: handoverMovement?.operationId || reconciliationMovement?.operationId,
        referenceId: activeShift.id,
      });

      // Show printable report
      showShiftClosingReport(updatedShift);

      return { expected, actual: actualCashCounted, difference };
    },
    [activeShift, calculateShiftExpectedCash, currentUser, logAuditEvent, showShiftClosingReport, settings.cashDiscrepancyThreshold, enterprise]
  );

  // Menu Management Actions
  const addProduct = useCallback(
    (product: Omit<Product, 'id'>) => {
      const newProd: Product = {
        ...product,
        id: `p-${Date.now()}`,
      };
      setProducts((prev) => [...prev, newProd]);

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `إضافة صنف جديد بالمنيو: ${newProd.name}`,
        category: 'menu',
        details: `السعر الأساسي: ${newProd.basePrice} ج.م - يرسل للفحم: ${newProd.sendToGrill ? 'نعم' : 'لا'}`,
        severity: 'normal',
      });
    },
    [currentUser, logAuditEvent]
  );

  const updateProduct = useCallback(
    (product: Product) => {
      setProducts((prev) => prev.map((p) => (p.id === product.id ? product : p)));

      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: `تعديل صنف بالمنيو: ${product.name}`,
        category: 'menu',
        details: `السعر: ${product.basePrice} ج.م، متاح: ${product.isAvailable ? 'نعم' : 'لا'}، فحم: ${
          product.sendToGrill ? 'نعم' : 'لا'
        }`,
        severity: 'normal',
      });
    },
    [currentUser, logAuditEvent]
  );

  const deleteProduct = useCallback((id: string) => {
    setProducts((prev) => prev.map((p) => (p.id === id ? { ...p, isAvailable: false } : p)));
  }, []);

  const addCategory = useCallback((category: Omit<Category, 'id'>) => {
    const newCat: Category = {
      ...category,
      id: `cat-${Date.now()}`,
    };
    setCategories((prev) => [...prev, newCat]);
  }, []);

  const updateCategory = useCallback((category: Category) => {
    setCategories((prev) => prev.map((c) => (c.id === category.id ? category : c)));
  }, []);

  const deleteCategory = useCallback((id: string) => {
    setCategories((prev) => prev.map((c) => (c.id === id ? { ...c, active: false } : c)));
  }, []);

  // System Settings Update
  const updateSettings = useCallback(
    (newSettings: Partial<SystemSettings>) => {
      if (!Object.keys(newSettings).every(key => key === 'grillScreenSoundAlert')) authorizeCommand({ actor: currentUser, permission: 'settings_manage' });
      setSettings((prev) => ({ ...prev, ...newSettings }));
      logAuditEvent({
        userId: currentUser.id,
        userName: currentUser.name,
        userRole: currentUser.role,
        action: 'تحديث إعدادات النظام وقواعد التشغيل',
        category: 'settings',
        details: `تم تحديث الإعدادات (سعر الساعة الإضافية: ${
          newSettings.overtimeHourlyRate || settings.overtimeHourlyRate
        } ج.م)`,
        severity: 'warning',
      });
    },
    [currentUser, settings.overtimeHourlyRate, logAuditEvent]
  );

  // Hardware Status
  const updateDeviceStatus = useCallback(
    (deviceId: string, status: 'connected' | 'disconnected' | 'warning') => {
      setDevices((prev) =>
        prev.map((d) => (d.id === deviceId ? { ...d, status, lastSeen: 'الآن' } : d))
      );
    },
    []
  );

  const markNotificationRead = useCallback((id: string) => {
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, read: true } : n)));
  }, []);
  const recordPayrollSettlements = useCallback((entries: EmployeeLedgerEntry[]) => {
    setLedgerEntries(current => entries.reduce((items, entry) => items.some(item => item.id === entry.id) ? items : [{ ...entry, balanceAfter: calculateEmployeeBalance(entry.employeeId) - entry.debit }, ...items], current));
    entries.forEach(entry => {
      logAuditEvent({ userId: currentUser.id, userName: currentUser.name, userRole: currentUser.role, action: 'تسوية Payroll للموظف', category: 'employee', details: `تسوية راتب بقيمة ${entry.debit} ج.م للموظف ${entry.employeeId} ضمن المسير ${entry.payrollRunId}`, severity: 'normal', referenceId: entry.payrollRunId || entry.id, operationId: entry.operationId });
    });
  }, [calculateEmployeeBalance, currentUser, logAuditEvent]);

  return (
    <AppContext.Provider
      value={{
        activeModule,
        setActiveModule,
        currentUser,
        hasPermission,
        users,
        updateUser,
        setCurrentUser,
        switchUser,
        isLocked,
        lockScreen,
        unlockScreen,
        settings,
        updateSettings,
        devices,
        updateDeviceStatus,
        notifications,
        markNotificationRead,
        categories,
        products,
        addProduct,
        updateProduct,
        deleteProduct,
        addCategory,
        updateCategory,
        deleteCategory,
        cartTabs,
        activeCartTabId,
        switchCartTab,
        addCartTab,
        removeCartTab,
        cart,
        orderType,
        setOrderType,
        tableNumber,
        setTableNumber,
        customerName,
        setCustomerName,
        customerPhone,
        setCustomerPhone,
        deliveryAddress,
        setDeliveryAddress,
        discount,
        discountReason,
        setDiscount,
        addToCart,
        updateCartItemQty,
        removeCartItem,
        updateCartItemNotes,
        clearCart,
        cartSubtotal,
        cartTotal,
        checkout,
        orders,
        cancelPaidOrder,
        refundPaidOrder,
        grillTickets,
        updateGrillTicketStatus,
        employees,
        addEmployee,
        updateEmployee,
        ledgerEntries,
        calculateEmployeeBalance,
        recordDailyWage,
        recordOvertime,
        recordExtraShift,
        recordAdvance,
        recordWithdrawal,
        recordManualLedgerEntry,
        recordPayrollSettlements,
        attendance,
        biometricCheckIn,
        biometricCheckOut,
        correctAttendance,
        shifts,
        activeShift,
        calculateShiftExpectedCash,
        openNewShift,
        closeShiftBlind,
        expenses,
        addExpense,
        cashMovements,
        addCashMovement,
        auditEvents,
        logAuditEvent,
        receiptModalOpen,
        setReceiptModalOpen,
        activeReceiptOrder,
        activeReceiptType,
        activeShiftForReport,
        showReceipt,
        showShiftClosingReport,
        managerAuthOpen,
        managerAuthParams,
        requestManagerAuth,
        closeManagerAuth,
        verifyManagerPin,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export function useAppStore() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error('useAppStore must be used within an AppProvider');
  }
  return context;
}
