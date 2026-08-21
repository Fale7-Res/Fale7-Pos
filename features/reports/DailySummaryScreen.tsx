'use client';

import React from 'react';
import { useAppStore } from '@/lib/store';
import { FileSpreadsheet, Printer, Calendar, TrendingUp, DollarSign, Wallet, Coins, ShieldAlert, CheckCircle2 } from 'lucide-react';
import { selectSalesReport } from '@/lib/reporting-domain.mjs';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import { selectBusinessDayRecords } from '@/lib/control-layer-domain.mjs';

export default function DailySummaryScreen() {
  const { orders, shifts, expenses, ledgerEntries, employees, settings } = useAppStore();
  const enterprise = useEnterpriseStore();
  const businessDay = enterprise.businessDays.find(day=>day.status!=='closed') || enterprise.businessDays[0];
  const dayShifts = selectBusinessDayRecords(shifts,businessDay,shifts);
  const dayOrders = selectBusinessDayRecords(orders,businessDay,dayShifts);
  const dayExpenses = selectBusinessDayRecords(expenses,businessDay,dayShifts);
  const dayLedger = selectBusinessDayRecords(ledgerEntries,businessDay,dayShifts);

  const todayStr = new Date().toLocaleDateString('ar-EG', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  // Sales totals
  const salesReport = selectSalesReport(dayOrders);
  const validOrders = salesReport.orders;
  const totalSales = salesReport.netSales;
  const cashSales = salesReport.cash;
  const cardSales = salesReport.card;
  const instapaySales = salesReport.instapay;
  const dineInSales = salesReport.dineIn;
  const takeawaySales = salesReport.takeaway;
  const deliverySales = salesReport.delivery;

  // Expenses total
  const totalExpenses = dayExpenses.reduce((s, e) => s + e.amount, 0);

  // Employee ledger totals
  const totalDailyWages = dayLedger.filter((l) => l.type === 'daily_wage').reduce((s, l) => s + l.credit, 0);
  const totalOvertime = dayLedger.filter((l) => l.type === 'overtime').reduce((s, l) => s + l.credit, 0);
  const totalAdvances = dayLedger.filter((l) => l.type === 'advance').reduce((s, l) => s + l.debit, 0);
  const totalWithdrawals = dayLedger.filter((l) => l.type === 'withdrawal').reduce((s, l) => s + l.debit, 0);

  // Shift discrepancies
  const totalDiscrepancies = dayShifts.reduce((s, sh) => s + (sh.cashDifference || 0), 0);

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Top Controls */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-500/10 text-emerald-600 border border-emerald-500/20 flex items-center justify-center">
            <FileSpreadsheet className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">ملخص اليوم التشغيلي الموحد (Daily Sheet)</h1>
            <p className="text-xs text-slate-500">
              تقرير إداري شامل يلخص المبيعات، حركات الخزنة، يوميات وسلف العمال، والمصروفات
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={() => window.print()}
          className="px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-2"
        >
          <Printer className="w-4 h-4" />
          <span>طباعة تقرير اليوم (A4 / حراري)</span>
        </button>
      </div>

      {/* 2. Printable Daily Sheet Document */}
      <div className="bg-white p-8 rounded-3xl border border-slate-200 shadow-sm max-w-4xl mx-auto space-y-6">
        {/* Document Header */}
        <div className="text-center pb-4 border-b border-slate-200 space-y-1">
          <h2 className="text-2xl font-black text-slate-900">{settings.restaurantName}</h2>
          <p className="text-xs font-bold text-red-600">{settings.subName} — خدمة 24 ساعة</p>
          <p className="text-xs text-slate-500">{settings.address}</p>
          <div className="inline-block mt-2 px-3 py-1 bg-slate-100 rounded-full text-xs font-bold text-slate-700">
            تقرير العمليات لليوم: {todayStr}
          </div>
        </div>

        {/* Section 1: Sales Summary */}
        <div className="space-y-3">
          <h3 className="text-sm font-black text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-1.5">
            <TrendingUp className="w-4 h-4 text-emerald-600" />
            <span>1. ملخص المبيعات والإيرادات</span>
          </h3>

          <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
            <div className="p-3 bg-emerald-50 rounded-2xl border border-emerald-200">
              <span className="text-[11px] font-bold text-emerald-800 block">إجمالي المبيعات</span>
              <span className="text-xl font-black text-emerald-900">{totalSales} ج.م</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-[11px] font-bold text-slate-600 block">مبيعات نقدية (كاش)</span>
              <span className="text-xl font-black text-slate-900">{cashSales} ج.م</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-[11px] font-bold text-slate-600 block">مبيعات بطاقات (فيزا)</span>
              <span className="text-xl font-black text-blue-700">{cardSales} ج.م</span>
            </div>
            <div className="p-3 bg-violet-50 rounded-2xl border border-violet-200">
              <span className="text-[11px] font-bold text-violet-700 block">مبيعات InstaPay</span>
              <span className="text-xl font-black text-violet-800">{instapaySales} ج.م</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-[11px] font-bold text-slate-600 block">عدد الطلبات المنفذة</span>
              <span className="text-xl font-black text-slate-900">{validOrders.length} طلب</span>
            </div>
          </div>

          <div className="flex justify-between text-xs bg-slate-50 p-2.5 rounded-xl border border-slate-100">
            <span>صالة: <strong className="text-slate-800">{dineInSales} ج</strong></span>
            <span>تيك أواي: <strong className="text-slate-800">{takeawaySales} ج</strong></span>
            <span>دليفري: <strong className="text-slate-800">{deliverySales} ج</strong></span>
          </div>
        </div>

        {/* Section 2: Cash & Drawer Movements */}
        <div className="space-y-3">
          <h3 className="text-sm font-black text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-1.5">
            <DollarSign className="w-4 h-4 text-blue-600" />
            <span>2. حركة الخزنة والورديات</span>
          </h3>

          <div className="grid grid-cols-3 gap-3 text-xs">
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">المصروفات النثرية:</span>
              <span className="text-base font-black text-red-600">-{totalExpenses} ج.م</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">السلف والسحوبات النقدية:</span>
              <span className="text-base font-black text-amber-700">-{totalAdvances + totalWithdrawals} ج.م</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">صافي الفروق النقدية بالورديات:</span>
              <span
                className={`text-base font-black ${
                  totalDiscrepancies === 0
                    ? 'text-emerald-700'
                    : totalDiscrepancies < 0
                    ? 'text-red-700'
                    : 'text-amber-700'
                }`}
              >
                {totalDiscrepancies === 0 ? '0 ج (مطابق)' : `${totalDiscrepancies} ج`}
              </span>
            </div>
          </div>
        </div>

        {/* Section 3: Employee Daily Wages & Advances */}
        <div className="space-y-3">
          <h3 className="text-sm font-black text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-1.5">
            <Wallet className="w-4 h-4 text-amber-600" />
            <span>3. يوميات وسلف العمال المسجلة</span>
          </h3>

          <div className="grid grid-cols-4 gap-3 text-xs">
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">إجمالي اليوميات المستحقة:</span>
              <span className="text-base font-black text-slate-900">+{totalDailyWages} ج</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">ساعات العمل الإضافية:</span>
              <span className="text-base font-black text-blue-700">+{totalOvertime} ج</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">السلف المنصرفة:</span>
              <span className="text-base font-black text-amber-700">-{totalAdvances} ج</span>
            </div>
            <div className="p-3 bg-slate-50 rounded-2xl border border-slate-200">
              <span className="text-slate-500 block">السحوبات من الرصيد:</span>
              <span className="text-base font-black text-red-700">-{totalWithdrawals} ج</span>
            </div>
          </div>
        </div>

        {/* Section 4: Signatures */}
        <div className="pt-8 border-t border-slate-200 grid grid-cols-3 text-center text-xs text-slate-600">
          <div>
            <p className="border-b border-slate-300 pb-8 mb-1">توقيع كاشير الوردية</p>
            <p className="font-bold">أحمد محمود</p>
          </div>
          <div>
            <p className="border-b border-slate-300 pb-8 mb-1">توقيع مدير الصالة</p>
            <p className="font-bold">سامح عبد الله</p>
          </div>
          <div>
            <p className="border-b border-slate-300 pb-8 mb-1">اعتماد المالك</p>
            <p className="font-bold">فالح أبو الغنية</p>
          </div>
        </div>
      </div>
    </div>
  );
}
