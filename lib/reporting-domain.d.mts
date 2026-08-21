import type { Expense, Order, OrderRefund } from './types';
export function selectSalesReport(orders: Order[], filter?: { date?: string; shiftId?: string }): { orders: Order[]; grossSales: number; refunds: number; netSales: number; orderCount: number; averageOrderValue: number; cash: number; card: number; instapay: number; dineIn: number; takeaway: number; delivery: number };
export function selectRefundActivity(orders: Order[], filter?: { date?: string; shiftId?: string }): Array<OrderRefund & { orderId: string; orderNumber: string; processingShiftId?: string }>;
export function selectExpenseTotal(expenses: Expense[], filter?: { date?: string; shiftId?: string }): number;
export function selectExpensesReport(expenses: Expense[], filter?: { date?: string; shiftId?: string }): { expenses: Expense[]; total: number };
