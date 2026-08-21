const orderDate = order => String(order.createdAt || '').slice(0, 10);
const refundDate = refund => String(refund.createdAt || '').slice(0, 10);
export const selectSalesReport = (orders, filter = {}) => {
  const sales = orders.filter(order => order.status !== 'cancelled' && (!filter.shiftId || order.shiftId === filter.shiftId) && (!filter.date || orderDate(order) === filter.date));
  const grossSales = sales.reduce((sum, order) => sum + order.total, 0);
  const refunds = sales.reduce((sum, order) => sum + (order.refundAmount || 0), 0);
  const payment = method => sales.filter(order => order.paymentMethod === method).reduce((sum, order) => sum + order.total - (order.refundAmount || 0), 0);
  const orderType = type => sales.filter(order => order.orderType === type).reduce((sum, order) => sum + order.total - (order.refundAmount || 0), 0);
  const netSales = grossSales - refunds;
  return { orders: sales, grossSales, refunds, netSales, orderCount: sales.length, averageOrderValue: sales.length ? netSales / sales.length : 0,
    cash: payment('cash'), card: payment('card'), instapay: payment('instapay'), dineIn: orderType('dine_in'), takeaway: orderType('takeaway'), delivery: orderType('delivery') };
};
export const selectRefundActivity = (orders, filter = {}) => orders.flatMap(order => (order.refunds || []).map(refund => ({ ...refund, orderId: order.id, orderNumber: order.orderNumber, originalShiftId: order.shiftId, processingShiftId: refund.operationalShiftId }))).filter(refund => (!filter.date || refundDate(refund) === filter.date) && (!filter.shiftId || refund.processingShiftId === filter.shiftId));
export const selectExpenseTotal = (expenses, filter = {}) => expenses.filter(expense => (!filter.shiftId || expense.shiftId === filter.shiftId) && (!filter.date || expense.date === filter.date)).reduce((sum, expense) => sum + expense.amount, 0);
export const selectExpensesReport = (expenses, filter = {}) => {
  const rows = expenses.filter(expense => (!filter.shiftId || expense.shiftId === filter.shiftId) && (!filter.date || expense.date === filter.date));
  return { expenses: rows, total: rows.reduce((sum, expense) => sum + expense.amount, 0) };
};
