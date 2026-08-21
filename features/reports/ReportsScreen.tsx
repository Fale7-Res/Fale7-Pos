'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import { BarChart3, TrendingUp, DollarSign, Calendar, Users, ShoppingBag, PieChart, Download, Printer } from 'lucide-react';
import { selectExpensesReport, selectSalesReport } from '@/lib/reporting-domain.mjs';

export default function ReportsScreen() {
  const enterprise = useEnterpriseStore();
  const { orders, categories, products, expenses, shifts, employees, calculateEmployeeBalance } = useAppStore();

  const [activeReportTab, setActiveReportTab] = useState<'sales' | 'items' | 'employees' | 'expenses'>('sales');
  const [dateFilter, setDateFilter] = useState<string>('');
  const [shiftFilter, setShiftFilter] = useState<string>('');

  // Total sales
  const salesReport = selectSalesReport(orders, { date: dateFilter || undefined, shiftId: shiftFilter || undefined });
  const validOrders = salesReport.orders;
  const totalRevenue = salesReport.netSales;
  const expensesReport = selectExpensesReport(expenses, { date: dateFilter || undefined, shiftId: shiftFilter || undefined });
  const filteredExpenses = expensesReport.expenses;
  const totalExpenses = expensesReport.total;
  const supplierDebt = enterprise.suppliers.reduce((s, supplier) => s + enterprise.supplierBalance(supplier.id), 0) / 100;
  const payrollDue = enterprise.payrollLines.filter(line => enterprise.payrollRuns.find(run => run.id === line.payrollRunId)?.status !== 'paid').reduce((s, line) => s + line.netPayable, 0) / 100;
  const lowStock = enterprise.inventoryItems.filter(item => enterprise.stockBalance(item.id) <= item.minimumMilliUnits).length;

  // Sales by Category
  const categorySalesMap: Record<string, { name: string; count: number; total: number }> = {};
  categories.forEach((c) => {
    categorySalesMap[c.id] = { name: c.name, count: 0, total: 0 };
  });

  validOrders.forEach((o) => {
    o.items.forEach((item) => {
      const prod = products.find((p) => p.id === item.productId);
      if (prod && categorySalesMap[prod.categoryId]) {
        categorySalesMap[prod.categoryId].count += item.quantity;
        categorySalesMap[prod.categoryId].total += item.totalPrice;
      }
    });
  });

  // Top Selling Items
  const productSalesMap: Record<string, { name: string; qty: number; total: number }> = {};
  validOrders.forEach((o) => {
    o.items.forEach((i) => {
      if (!productSalesMap[i.productName]) {
        productSalesMap[i.productName] = { name: i.productName, qty: 0, total: 0 };
      }
      productSalesMap[i.productName].qty += i.quantity;
      productSalesMap[i.productName].total += i.totalPrice;
    });
  });

  const topItems = Object.values(productSalesMap).sort((a, b) => b.qty - a.qty);

  // Unique dates from orders for filter dropdown
  const orderDates = Array.from(new Set(orders.map(o => o.createdAt.slice(0, 10)))).sort().reverse();
  const shiftOptions = shifts.map(s => ({ id: s.id, name: s.shiftName, date: s.openedAt.slice(0, 10) }));

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-purple-500/10 text-purple-600 border border-purple-500/20 flex items-center justify-center">
            <BarChart3 className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">التقارير التحليلية والإحصاءات</h1>
            <p className="text-xs text-slate-500">
              تقارير تفصيلية للأصناف الأكثر مبيعاً، إيرادات الأقسام، المصروفات، وأرصدة الموظفين
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={() => window.print()}
          className="px-4 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1.5"
        >
          <Printer className="w-4 h-4" />
          <span>طباعة التقرير</span>
        </button>
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
        {[
          ['صافي تشغيلي مبدئي', `${totalRevenue - totalExpenses} ج.م`, 'text-emerald-700'],
          ['مديونية الموردين', `${supplierDebt.toLocaleString('ar-EG')} ج.م`, 'text-amber-700'],
          ['رواتب غير مسددة', `${payrollDue.toLocaleString('ar-EG')} ج.م`, 'text-violet-700'],
          ['تنبيهات الحد الأدنى', `${lowStock} صنف`, 'text-red-700'],
          ['طلبات دليفري مفتوحة', `${enterprise.deliveries.filter(x => x.status !== 'settled' && x.status !== 'returned').length} طلب`, 'text-blue-700'],
        ].map(([label, value, color]) => <div key={label} className="rounded-2xl border border-slate-200 bg-white p-4"><span className="text-[10px] font-bold text-slate-500">{label}</span><strong className={`mt-2 block text-base ${color}`}>{value}</strong></div>)}
      </div>

      {/* 2. Filters */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-xs flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
        <div className="flex items-center gap-3 flex-wrap">
          <label className="text-xs font-bold text-slate-600">التاريخ:</label>
          <input
            type="date"
            value={dateFilter}
            onChange={(e) => setDateFilter(e.target.value)}
            className="bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 focus:outline-none focus:border-[#0F52BA] focus:ring-2 focus:ring-blue-50"
          />
          {dateFilter ? (
            <button
              type="button"
              onClick={() => setDateFilter('')}
              className="text-xs text-red-600 hover:text-red-700 font-medium"
            >
              مسح
            </button>
          ) : null}
        </div>
        <div className="flex items-center gap-3 flex-wrap">
          <label className="text-xs font-bold text-slate-600">الوردية:</label>
          <select
            value={shiftFilter}
            onChange={(e) => setShiftFilter(e.target.value)}
            className="bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 focus:outline-none focus:border-[#0F52BA] focus:ring-2 focus:ring-blue-50"
          >
            <option value="">جميع الورديات</option>
            {shiftOptions.map(s => (
              <option key={s.id} value={s.id}>{s.name} ({s.date})</option>
            ))}
          </select>
          {shiftFilter ? (
            <button
              type="button"
              onClick={() => setShiftFilter('')}
              className="text-xs text-red-600 hover:text-red-700 font-medium"
            >
              مسح
            </button>
          ) : null}
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <span>{validOrders.length} طلب</span>
          <span>•</span>
          <span>إجمالي: {totalRevenue} ج</span>
          <span>•</span>
          <span>صافي: {salesReport.netSales} ج</span>
          <span>•</span>
          <span>مرتجعات: {salesReport.refunds} ج</span>
        </div>
      </div>

      <div className="flex items-center gap-2 bg-white p-2 rounded-2xl border border-slate-200 shadow-xs">
        <button
          type="button"
          onClick={() => setActiveReportTab('sales')}
          className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
            activeReportTab === 'sales' ? 'bg-red-600 text-white shadow-xs' : 'text-slate-700 hover:bg-slate-100'
          }`}
        >
          تقرير المبيعات والأقسام
        </button>
        <button
          type="button"
          onClick={() => setActiveReportTab('items')}
          className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
            activeReportTab === 'items' ? 'bg-red-600 text-white shadow-xs' : 'text-slate-700 hover:bg-slate-100'
          }`}
        >
          الأصناف الأكثر طلباً
        </button>
        <button
          type="button"
          onClick={() => setActiveReportTab('employees')}
          className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
            activeReportTab === 'employees' ? 'bg-red-600 text-white shadow-xs' : 'text-slate-700 hover:bg-slate-100'
          }`}
        >
          أرصدة ورواتب الموظفين
        </button>
        <button
          type="button"
          onClick={() => setActiveReportTab('expenses')}
          className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
            activeReportTab === 'expenses' ? 'bg-red-600 text-white shadow-xs' : 'text-slate-700 hover:bg-slate-100'
          }`}
        >
          تحليل المصروفات
        </button>
      </div>

      {activeReportTab === 'sales' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-4">
            <h3 className="text-sm font-extrabold text-slate-900">مبيعات أقسام المنيو</h3>
            <div className="space-y-2">
              {Object.values(categorySalesMap).map((cat, idx) => (
                <div key={idx} className="flex justify-between items-center p-3 bg-slate-50 rounded-2xl text-xs">
                  <span className="font-bold text-slate-800">{cat.name}</span>
                  <div className="text-left">
                    <span className="font-black text-slate-900 text-sm">{cat.total} ج.م</span>
                    <span className="text-[10px] text-slate-400 block font-normal">({cat.count} قطعة بيعت)</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-4">
            <h3 className="text-sm font-extrabold text-slate-900">طرق الدفع وقنوات البيع</h3>
            <div className="space-y-3 text-xs">
              <div className="p-3 bg-emerald-50 rounded-2xl border border-emerald-200 flex justify-between items-center">
                <span className="font-bold text-emerald-900">مبيعات نقدية (كاش بالدرج)</span>
                <span className="font-black text-emerald-900 text-base">
                  {salesReport.cash} ج.م
                </span>
              </div>
              <div className="p-3 bg-blue-50 rounded-2xl border border-blue-200 flex justify-between items-center">
                <span className="font-bold text-blue-900">مبيعات بطاقات بنكية (فيزا)</span>
                <span className="font-black text-blue-900 text-base">
                  {salesReport.card} ج.م
                </span>
              </div>
              <div className="p-3 bg-violet-50 rounded-2xl border border-violet-200 flex justify-between items-center">
                <span className="font-bold text-violet-900">مبيعات InstaPay</span>
                <span className="font-black text-violet-900 text-base">
                  {salesReport.instapay} ج.م
                </span>
              </div>
              <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200 flex justify-between items-center">
                <span className="font-bold text-slate-800">إجمالي المبيعات الشامل</span>
                <span className="font-black text-slate-900 text-base">{totalRevenue} ج.م</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeReportTab === 'items' && (
        <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
          <div className="p-4 bg-slate-50 border-b border-slate-200 font-bold text-xs text-slate-700">
            ترتيب الأصناف الأكثر مبيعاً وطلباً
          </div>
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3 px-4">#</th>
                <th className="py-3 px-4">الصنف</th>
                <th className="py-3 px-4 text-center">الكمية المباعة</th>
                <th className="py-3 px-4 text-center">إجمالي الإيراد المحقق</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {topItems.map((item, idx) => (
                <tr key={idx} className="hover:bg-slate-50">
                  <td className="py-3 px-4 font-bold text-slate-400">{idx + 1}</td>
                  <td className="py-3 px-4 font-black text-slate-900 text-sm">{item.name}</td>
                  <td className="py-3 px-4 text-center font-bold text-slate-800">{item.qty} قطعة</td>
                  <td className="py-3 px-4 text-center font-black text-emerald-600 text-sm">{item.total} ج.م</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeReportTab === 'employees' && (
        <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
          <div className="p-4 bg-slate-50 border-b border-slate-200 font-bold text-xs text-slate-700">
            كشف أرصدة ومستحقات الموظفين الحالية
          </div>
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3 px-4">الموظف</th>
                <th className="py-3 px-4">الوظيفة</th>
                <th className="py-3 px-4 text-center">اليومية</th>
                <th className="py-3 px-4 text-center">الرصيد الصافي</th>
                <th className="py-3 px-4 text-center">الحالة المالية</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {employees.map((emp) => {
                const bal = calculateEmployeeBalance(emp.id);
                return (
                  <tr key={emp.id} className="hover:bg-slate-50">
                    <td className="py-3 px-4 font-black text-slate-900">{emp.name}</td>
                    <td className="py-3 px-4 text-slate-600">{emp.jobTitle}</td>
                    <td className="py-3 px-4 text-center font-bold">{emp.dailyWage || 0} ج</td>
                    <td className="py-3 px-4 text-center font-black text-sm">{bal} ج</td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                          bal > 0
                            ? 'bg-emerald-100 text-emerald-800'
                            : bal < 0
                            ? 'bg-red-100 text-red-800'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {bal > 0 ? `مستحق للموظف (+${bal})` : bal < 0 ? `مطلوب من الموظف (${bal})` : 'مسدد بالكامل'}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {activeReportTab === 'expenses' && (
        <div className="bg-white rounded-3xl border border-slate-200 shadow-xs p-5 space-y-4">
          <h3 className="text-sm font-extrabold text-slate-900">سجل المصروفات والنثريات</h3>
          <div className="space-y-2">
            {filteredExpenses.map((e) => (
              <div key={e.id} className="flex justify-between items-center p-3 bg-slate-50 rounded-2xl text-xs">
                <div>
                  <span className="font-bold text-slate-900 block">{e.description}</span>
                  <span className="text-[10px] text-slate-400 font-medium">
                    {e.category} • {e.date} {e.time} • بواسطة {e.userName}
                  </span>
                </div>
                <span className="font-black text-red-600 text-sm">-{e.amount} ج.م</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
