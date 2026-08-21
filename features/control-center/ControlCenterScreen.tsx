'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import {
  ShieldAlert,
  AlertTriangle,
  Flame,
  Receipt,
  RotateCcw,
  XCircle,
  Percent,
  Coins,
  KeyRound,
  Filter,
  CheckCircle2,
} from 'lucide-react';
import { buildControlExceptions } from '@/lib/control-layer-domain.mjs';
import { readFinancialJournal } from '@/lib/financial-journal.mjs';

export default function ControlCenterScreen() {
  const enterprise = useEnterpriseStore();
  const { auditEvents, orders, shifts, expenses, ledgerEntries, settings, currentUser } = useAppStore();
  const [severityFilter, setSeverityFilter] = useState<'all' | 'critical' | 'warning' | 'info'>('all');

  // Stats calculation
  const totalCancelled = orders.filter((o) => o.status === 'cancelled').length;
  const totalRefundedSum = orders
    .filter((o) => o.status === 'refunded')
    .reduce((sum, o) => sum + (o.refundAmount || o.total), 0);
  const totalDiscountsSum = orders.reduce((sum, o) => sum + (o.discount || 0), 0);
  const shiftDifferencesSum = shifts.reduce((sum, s) => sum + (s.cashDifference || 0), 0);
  const drawerOpeningsWithoutSale = auditEvents.filter((a) => a.action.includes('فتح الدرج بدون بيع')).length;
  const deliveryAlerts = enterprise.deliveryExceptions();
  const controlExceptions = buildControlExceptions({orders,shifts,expenses,driverCustodies:enterprise.driverCustodies,journalEntries:typeof window==='undefined'?[]:readFinancialJournal(localStorage),states:enterprise.exceptionStates});
  const changeException=(id:string,status:'acknowledged'|'resolved')=>{const reason=window.prompt('اكتب سبب إجراء المراجعة');if(reason)enterprise.setExceptionStatus(id,status,reason,currentUser)};

  const filteredEvents = auditEvents.filter((a) => {
    if (severityFilter === 'all') return true;
    return a.severity === severityFilter;
  });

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-red-500/10 text-red-600 border border-red-500/20 flex items-center justify-center">
            <ShieldAlert className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-black text-slate-900">مركز الاستثناءات والمراجعة التشغيلية</h1>
              <span className="text-xs bg-red-100 text-red-800 font-bold px-2 py-0.5 rounded-full border border-red-200">
                حماية الخزنة 24 ساعة
              </span>
            </div>
            <p className="text-xs text-slate-500">
              رصد وتوثيق كافة العمليات المالية الحساسة، الفروق النقدية، الإلغاءات، وفتح الدرج المشبوه
            </p>
          </div>
        </div>

        {/* Severity filter pills */}
        <div className="flex items-center gap-1.5 bg-slate-100 p-1.5 rounded-2xl border border-slate-200">
          <button
            type="button"
            onClick={() => setSeverityFilter('all')}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${
              severityFilter === 'all' ? 'bg-slate-900 text-white shadow-xs' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            الكل ({auditEvents.length})
          </button>
          <button
            type="button"
            onClick={() => setSeverityFilter('critical')}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1 ${
              severityFilter === 'critical'
                ? 'bg-red-600 text-white shadow-xs'
                : 'text-red-600 hover:bg-red-50'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-red-400"></span>
            <span>أحداث حرجة</span>
          </button>
          <button
            type="button"
            onClick={() => setSeverityFilter('warning')}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1 ${
              severityFilter === 'warning'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'text-amber-600 hover:bg-amber-50'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-amber-400"></span>
            <span>تنبيهات</span>
          </button>
        </div>
      </div>

      {/* 2. Surveillance Metrics Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
        {/* Card 1: Shift Cash Difference */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs space-y-1">
          <span className="text-[11px] font-bold text-slate-400 block">فروق الخزنة التراكمية</span>
          <div
            className={`text-xl font-black ${
              shiftDifferencesSum === 0
                ? 'text-emerald-600'
                : shiftDifferencesSum < 0
                ? 'text-red-600'
                : 'text-amber-600'
            }`}
          >
            {shiftDifferencesSum === 0 ? '0 ج (مطابق)' : `${shiftDifferencesSum} ج`}
          </div>
          <span className="text-[10px] text-slate-400">من إغلاق الورديات</span>
        </div>

        {/* Card 2: Cancelled Orders */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs space-y-1">
          <span className="text-[11px] font-bold text-slate-400 block">الطلبات الملغاة بعد الدفع</span>
          <div className="text-xl font-black text-red-600">{totalCancelled} طلبات</div>
          <span className="text-[10px] text-slate-400">تتطلب موافقة المدير</span>
        </div>

        {/* Card 3: Refunds */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs space-y-1">
          <span className="text-[11px] font-bold text-slate-400 block">المرتجعات المنفذة</span>
          <div className="text-xl font-black text-purple-600">{totalRefundedSum} ج.م</div>
          <span className="text-[10px] text-slate-400">استرجاع أموال كاش</span>
        </div>

        {/* Card 4: Total Discounts */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs space-y-1">
          <span className="text-[11px] font-bold text-slate-400 block">إجمالي الخصومات الممنوحة</span>
          <div className="text-xl font-black text-amber-600">{totalDiscountsSum} ج.م</div>
          <span className="text-[10px] text-slate-400">خصومات صالة وتيك أواي</span>
        </div>

        {/* Card 5: Unauthorized Drawer Openings */}
        <div className="bg-white p-4 rounded-3xl border border-slate-200 shadow-xs space-y-1">
          <span className="text-[11px] font-bold text-slate-400 block">فتح الدرج بدون بيع</span>
          <div className="text-xl font-black text-slate-800">{drawerOpeningsWithoutSale} مرات</div>
          <span className="text-[10px] text-slate-400">حركات مراقبة بالدرج</span>
        </div>
      </div>
      {deliveryAlerts.length > 0 && <div className="rounded-3xl border border-orange-200 bg-orange-50 p-4"><h3 className="font-black text-orange-900">استثناءات الدليفري ({deliveryAlerts.length})</h3><div className="mt-2 grid gap-2 md:grid-cols-2">{deliveryAlerts.map((item,index)=><div key={`${item.type}-${item.deliveryId||item.tripId||index}`} className="rounded-xl border border-orange-200 bg-white p-3 text-xs font-bold text-orange-900">{item.message}</div>)}</div></div>}
      <div className="rounded-3xl border bg-white p-5"><h3 className="font-black">الاستثناءات التي تحتاج مراجعة ({controlExceptions.filter(x=>x.status!=='resolved').length})</h3><div className="mt-3 grid gap-2">{controlExceptions.map(item=><div key={item.id} className={`flex items-center justify-between rounded-xl border p-3 text-xs ${item.severity==='critical'?'border-red-200 bg-red-50':'border-amber-200 bg-amber-50'}`}><div><b>{item.message}</b><p>{item.category} — {item.status}</p></div><div className="flex gap-2"><button onClick={()=>changeException(item.id,'acknowledged')} className="rounded-lg border bg-white px-2 py-1">تم الاطلاع</button><button onClick={()=>changeException(item.id,'resolved')} className="rounded-lg bg-emerald-700 px-2 py-1 text-white">تمت المعالجة</button></div></div>)}</div></div>

      {/* 3. Real-time Surveillance Stream */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldAlert className="w-4 h-4 text-red-600" />
            <h3 className="font-extrabold text-sm text-slate-800">شريط الأحداث الرقابية المباشرة</h3>
          </div>
          <span className="text-xs text-slate-400 font-semibold">{filteredEvents.length} حدث مسجل</span>
        </div>

        <div className="p-4 space-y-3">
          {filteredEvents.length === 0 ? (
            <div className="text-center py-8 text-slate-400 text-xs">لا توجد أحداث رقابية بهذا التصنيف.</div>
          ) : (
            filteredEvents.map((event) => (
              <div
                key={event.id}
                className={`p-4 rounded-2xl border flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs ${
                  event.severity === 'critical'
                    ? 'bg-red-50/80 border-red-200 text-red-950'
                    : event.severity === 'warning'
                    ? 'bg-amber-50/80 border-amber-200 text-amber-950'
                    : 'bg-slate-50 border-slate-200 text-slate-800'
                }`}
              >
                <div className="flex items-start gap-3">
                  <div
                    className={`p-2 rounded-xl shrink-0 mt-0.5 ${
                      event.severity === 'critical'
                        ? 'bg-red-200 text-red-700'
                        : event.severity === 'warning'
                        ? 'bg-amber-200 text-amber-700'
                        : 'bg-slate-200 text-slate-700'
                    }`}
                  >
                    <AlertTriangle className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-black text-sm text-slate-900">{event.action}</span>
                      <span
                        className={`text-[10px] font-bold px-2 py-0.2 rounded-full ${
                          event.severity === 'critical'
                            ? 'bg-red-600 text-white'
                            : event.severity === 'warning'
                            ? 'bg-amber-600 text-white'
                            : 'bg-slate-200 text-slate-700'
                        }`}
                      >
                        {event.severity === 'critical'
                          ? 'حرج جداً'
                          : event.severity === 'warning'
                          ? 'تنبيه'
                          : 'عادي'}
                      </span>
                    </div>
                    <p className="text-slate-700 mt-1 leading-snug">{event.details}</p>
                    {event.reason && (
                      <div className="text-[11px] font-bold text-red-700 mt-1 bg-white/70 px-2 py-0.5 rounded-md inline-block">
                        السبب الموثق: {event.reason}
                      </div>
                    )}
                  </div>
                </div>

                {/* Event Metadata */}
                <div className="text-left shrink-0 text-[11px] text-slate-500 space-y-0.5 pt-2 md:pt-0 border-t md:border-t-0 border-slate-200/60">
                  <div className="font-mono">{event.timestamp}</div>
                  <div>
                    المنفذ: <span className="font-bold text-slate-800">{event.userName}</span>
                  </div>
                  {event.authorizedBy && (
                    <div className="text-emerald-700 font-bold">
                      اعتماد المدير: {event.authorizedBy} ✓
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
