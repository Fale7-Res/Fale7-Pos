'use client';

import React from 'react';
import { selectExpenseTotal, selectSalesReport } from '@/lib/reporting-domain.mjs';
import { useAppStore } from '@/lib/store';
import {
  DollarSign,
  ShoppingBag,
  TrendingUp,
  Wallet,
  Coins,
  Receipt,
  Flame,
  AlertTriangle,
  Users,
  Clock,
  ShieldAlert,
  ArrowUpRight,
  ArrowDownRight,
  Sparkles,
  ChevronLeft,
} from 'lucide-react';

export default function DashboardScreen() {
  const {
    orders,
    activeShift,
    shifts,
    expenses,
    grillTickets,
    attendance,
    auditEvents,
    setActiveModule,
    settings,
  } = useAppStore();

  // Metrics for today
  const dashboardSales = selectSalesReport(orders, { date: new Date().toISOString().slice(0, 10) });
  const todayOrders = dashboardSales.orders;
  const todaySales = dashboardSales.netSales;
  const totalOrdersCount = todayOrders.length;
  const avgOrderValue = totalOrdersCount > 0 ? Math.round(todaySales / totalOrdersCount) : 0;

  const totalCashSales = dashboardSales.cash;
  const totalCardSales = dashboardSales.card;
  const totalInstapaySales = dashboardSales.instapay;
  const totalExpenses = selectExpenseTotal(expenses, { date: new Date().toISOString().slice(0, 10) });
  const totalRefunds = dashboardSales.refunds;
  const totalCancelledCount = orders.filter((o) => o.status === 'cancelled').length;

  const activeGrillQueue = grillTickets.filter((t) => t.status !== 'ready').length;
  const presentEmployeesCount = attendance.filter((a) => a.status === 'present' || a.status === 'late').length;

  // Cash discrepancy sum across shifts
  const totalDiscrepancies = shifts.reduce((sum, s) => sum + (s.cashDifference || 0), 0);

  // Critical alerts from audit log
  const suspiciousEvents = auditEvents.filter(
    (a) => a.severity === 'critical' || a.severity === 'warning'
  ).slice(0, 4);

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header & Operational Status Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-red-600 text-white flex items-center justify-center font-black text-xl shadow-sm">
              ف
            </div>
            <div>
              <h1 className="text-xl font-black text-slate-900">لوحة المتابعة والعمليات التشغيلية</h1>
              <p className="text-xs text-slate-500">
                مطعم {settings.restaurantName} — متابعة المبيعات والورديات والخزنة لحظة بلحظة
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setActiveModule(activeShift ? 'pos' : 'shifts')}
            className={`px-5 py-2.5 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-2 ${activeShift ? 'bg-red-600 hover:bg-red-700 active:bg-red-800' : 'bg-emerald-600 hover:bg-emerald-700'}`}
          >
            <ShoppingBag className="w-4 h-4" />
            <span>{activeShift ? 'فتح شاشة الكاشير (POS)' : 'فتح وردية لبدء التشغيل'}</span>
          </button>
          <button
            type="button"
            onClick={() => setActiveModule('daily_wages')}
            className="px-4 py-2.5 bg-slate-800 hover:bg-slate-900 text-white text-xs font-bold rounded-xl transition-all flex items-center gap-2"
          >
            <Wallet className="w-4 h-4 text-amber-400" />
            <span>يوميات الموظفين</span>
          </button>
        </div>
      </div>

      {/* 2. Key Operational Metrics (Cards Grid) */}
      <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-4 gap-4">
        {/* Metric 1: Today Sales */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs hover:border-slate-300 transition-all space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500">إجمالي مبيعات اليوم</span>
            <div className="w-8 h-8 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <TrendingUp className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-black text-slate-900">{todaySales.toLocaleString()} ج.م</div>
          <div className="text-[11px] text-slate-400 flex items-center gap-1.5">
            <span>كاش: {totalCashSales} ج</span>
            <span>•</span>
            <span>فيزا: {totalCardSales} ج</span>
            <span>InstaPay: {totalInstapaySales} ج</span>
          </div>
        </div>

        {/* Metric 2: Orders Count */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs hover:border-slate-300 transition-all space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500">عدد الطلبات المنفذة</span>
            <div className="w-8 h-8 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <Receipt className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-black text-slate-900">{totalOrdersCount} طلب</div>
          <div className="text-[11px] text-slate-400">متوسط قيمة الأوردر: {avgOrderValue} ج.م</div>
        </div>

        {/* Metric 3: Expenses */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs hover:border-slate-300 transition-all space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500">إجمالي المصروفات</span>
            <div className="w-8 h-8 rounded-xl bg-red-50 text-red-600 flex items-center justify-center">
              <Coins className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-black text-red-600">{totalExpenses} ج.م</div>
          <div className="text-[11px] text-slate-400">{expenses.length} حركات مصروف مسجلة</div>
        </div>

        {/* Metric 4: Cash Difference / Discrepancy */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs hover:border-slate-300 transition-all space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500">فروق الخزنة المسجلة</span>
            <div className="w-8 h-8 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
              <ShieldAlert className="w-4 h-4" />
            </div>
          </div>
          <div
            className={`text-2xl font-black ${
              totalDiscrepancies === 0
                ? 'text-emerald-600'
                : totalDiscrepancies < 0
                ? 'text-red-600'
                : 'text-amber-600'
            }`}
          >
            {totalDiscrepancies === 0
              ? 'مطابق (0 ج)'
              : totalDiscrepancies < 0
              ? `عجز ${Math.abs(totalDiscrepancies)} ج`
              : `زيادة ${totalDiscrepancies} ج`}
          </div>
          <div className="text-[11px] text-slate-400">الوردية السابقة أغلقت بعجز -150 ج</div>
        </div>
      </div>

      {/* 3. Secondary Metrics: Active Shift & Grill Queue */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Active Shift Overview Card */}
        <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Clock className="w-4 h-4 text-slate-600" />
              <h3 className="font-extrabold text-sm text-slate-800">الوردية الحالية</h3>
            </div>
            <span className="text-[11px] font-bold bg-emerald-100 text-emerald-800 px-2 py-0.5 rounded-full">
              {activeShift ? 'نشطة ومفتوحة' : 'مغلقة'}
            </span>
          </div>

          {activeShift ? (
            <div className="space-y-2 text-xs pt-1">
              <div className="flex justify-between text-slate-600">
                <span>اسم الوردية:</span>
                <span className="font-bold text-slate-800">{activeShift.shiftName}</span>
              </div>
              <div className="flex justify-between text-slate-600">
                <span>الكاشير المسؤول:</span>
                <span className="font-bold text-slate-800">{activeShift.openedByUserName}</span>
              </div>
              <div className="flex justify-between text-slate-600">
                <span>وقت الافتتاح:</span>
                <span>{activeShift.openedAt}</span>
              </div>
              <div className="flex justify-between text-slate-600">
                <span>رصيد الافتتاح:</span>
                <span className="font-bold">{activeShift.openingCash} ج</span>
              </div>
              <div className="flex justify-between text-slate-600">
                <span>مبيعات الوردية:</span>
                <span className="font-bold text-emerald-600">{activeShift.totalSales} ج</span>
              </div>
            </div>
          ) : (
            <div className="text-center py-4 text-xs text-slate-400">لا توجد وردية مفتوحة حالياً</div>
          )}

          <button
            type="button"
            onClick={() => setActiveModule('shifts')}
            className="w-full py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1 mt-2"
          >
            <span>إدارة وتقفيل الوردية (التقفيل الأعمى)</span>
            <ChevronLeft className="w-3.5 h-3.5" />
          </button>
        </div>

        {/* Grill Station Real-time Status Card */}
        <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Flame className="w-4 h-4 text-amber-600" />
              <h3 className="font-extrabold text-sm text-slate-800">محطة الفحم والشواية</h3>
            </div>
            <span className="text-[11px] font-bold bg-amber-100 text-amber-800 px-2 py-0.5 rounded-full">
              {activeGrillQueue} قيد الانتظار
            </span>
          </div>

          <div className="space-y-2 pt-1 text-xs">
            {grillTickets.slice(0, 3).map((ticket) => (
              <div
                key={ticket.id}
                className="bg-slate-50 p-2.5 rounded-xl border border-slate-200 flex items-center justify-between"
              >
                <div>
                  <div className="font-black text-slate-900">{ticket.orderNumber}</div>
                  <div className="text-[11px] text-slate-500">
                    {ticket.items.map((i) => `${i.productName} (${i.quantity})`).join(', ')}
                  </div>
                </div>
                <span
                  className={`text-[10px] font-bold px-2 py-0.5 rounded-md ${
                    ticket.status === 'ready'
                      ? 'bg-emerald-100 text-emerald-800'
                      : ticket.status === 'preparing'
                      ? 'bg-blue-100 text-blue-800'
                      : 'bg-amber-100 text-amber-800'
                  }`}
                >
                  {ticket.status === 'ready' ? 'جاهز' : ticket.status === 'preparing' ? 'قيد التحضير' : 'جديد'}
                </span>
              </div>
            ))}
          </div>

          <button
            type="button"
            onClick={() => setActiveModule('grill')}
            className="w-full py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1 mt-2"
          >
            <span>فتح شاشة الفحم المباشرة</span>
            <ChevronLeft className="w-3.5 h-3.5" />
          </button>
        </div>

        {/* Staff & Biometric Attendance Card */}
        <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Users className="w-4 h-4 text-blue-600" />
              <h3 className="font-extrabold text-sm text-slate-800">حضور الموظفين اليوم</h3>
            </div>
            <span className="text-[11px] font-bold bg-blue-100 text-blue-800 px-2 py-0.5 rounded-full">
              {presentEmployeesCount} حاضرين
            </span>
          </div>

          <div className="space-y-1.5 pt-1 text-xs">
            {attendance.slice(0, 4).map((att) => (
              <div key={att.id} className="flex justify-between items-center py-1 border-b border-slate-100">
                <span className="font-semibold text-slate-700">{att.employeeName}</span>
                <span
                  className={`text-[10px] font-bold px-1.5 py-0.2 rounded-sm ${
                    att.status === 'present'
                      ? 'bg-emerald-100 text-emerald-800'
                      : att.status === 'late'
                      ? 'bg-amber-100 text-amber-800'
                      : 'bg-red-100 text-red-800'
                  }`}
                >
                  {att.status === 'present'
                    ? `حاضر (${att.checkIn})`
                    : att.status === 'late'
                    ? `متأخر ${att.lateMinutes} د`
                    : 'غائب'}
                </span>
              </div>
            ))}
          </div>

          <button
            type="button"
            onClick={() => setActiveModule('attendance')}
            className="w-full py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1 mt-2"
          >
            <span>سجل الحضور والبصمة</span>
            <ChevronLeft className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* 4. Suspicious & Fraud Alerts Section */}
      <div className="bg-white p-5 rounded-3xl border border-slate-200 shadow-xs space-y-3">
        <div className="flex items-center justify-between pb-2 border-b border-slate-100">
          <div className="flex items-center gap-2 text-red-600">
            <ShieldAlert className="w-5 h-5" />
            <h3 className="font-black text-sm text-slate-900">مركز الرقابة: تنبيهات العمليات المشبوهة والحرجة</h3>
          </div>
          <button
            type="button"
            onClick={() => setActiveModule('control_center')}
            className="text-xs font-bold text-red-600 hover:underline flex items-center gap-1"
          >
            <span>فتح مركز الرقابة الكامل</span>
            <ChevronLeft className="w-3.5 h-3.5" />
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {suspiciousEvents.map((event) => (
            <div
              key={event.id}
              className={`p-3 rounded-2xl border flex items-start gap-3 text-xs ${
                event.severity === 'critical'
                  ? 'bg-red-50/70 border-red-200 text-red-900'
                  : 'bg-amber-50/70 border-amber-200 text-amber-900'
              }`}
            >
              <AlertTriangle
                className={`w-4 h-4 shrink-0 mt-0.5 ${
                  event.severity === 'critical' ? 'text-red-600' : 'text-amber-600'
                }`}
              />
              <div className="space-y-0.5 flex-1">
                <div className="flex justify-between items-center">
                  <span className="font-black">{event.action}</span>
                  <span className="text-[10px] text-slate-500">{event.timestamp}</span>
                </div>
                <p className="text-slate-700 text-[11px] leading-snug">{event.details}</p>
                {event.reason && (
                  <div className="text-[10px] text-slate-500 font-semibold mt-1">السبب الموثق: {event.reason}</div>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
