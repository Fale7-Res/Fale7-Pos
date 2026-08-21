'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Shift } from '@/lib/types';
import {
  Clock,
  Lock,
  Unlock,
  CheckCircle2,
  AlertTriangle,
  FileText,
  DollarSign,
  Plus,
  Printer,
  Calendar,
  User,
  History,
  Coins,
  ShieldCheck,
  Receipt,
  X,
  Check,
} from 'lucide-react';

export default function ShiftsScreen() {
  const {
    shifts,
    activeShift,
    openNewShift,
    closeShiftBlind,
    currentUser,
    showShiftClosingReport,
    requestManagerAuth,
    settings,
    calculateShiftExpectedCash,
  } = useAppStore();

  // Open Shift Modal State
  const [openModalVisible, setOpenModalVisible] = useState(false);
  const [newShiftName, setNewShiftName] = useState('وردية صباحية');
  const [newOpeningCash, setNewOpeningCash] = useState('500');

  // Blind Close Modal State
  const [closeModalVisible, setCloseModalVisible] = useState(false);
  const [step, setStep] = useState<'count' | 'result'>('count');
  const [countedCashInput, setCountedCashInput] = useState('');
  const [reasonInput, setReasonInput] = useState('');
  const [discrepancyData, setDiscrepancyData] = useState<{
    expected: number;
    counted: number;
    difference: number;
  } | null>(null);
  const [errorMsg, setErrorMsg] = useState('');

  // Start Blind Close Flow
  const handleStartCloseShift = () => {
    if (!activeShift) return;
    setStep('count');
    setCountedCashInput('');
    setReasonInput('');
    setDiscrepancyData(null);
    setErrorMsg('');
    setCloseModalVisible(true);
  };

  // Step 1: Submit Blind Cash Count
  const handleCalculateDifference = () => {
    if (!activeShift) return;
    const counted = Number(countedCashInput);
    if (isNaN(counted) || counted < 0) {
      setErrorMsg('الرجاء إدخال مبلغ نقدي صحيح.');
      return;
    }

    const expected = calculateShiftExpectedCash(activeShift.id);
    const diff = counted - expected;

    setDiscrepancyData({
      expected,
      counted,
      difference: diff,
    });
    setStep('result');
  };

  // Step 2: Finalize Closing (with Manager PIN if discrepancy exists)
  const handleFinalizeClose = () => {
    if (!activeShift || !discrepancyData) return;

    const { counted, difference } = discrepancyData;

    const needsApproval = Math.round(Math.abs(difference) * 100) > Math.round(settings.cashDiscrepancyThreshold * 100);
    if (needsApproval && !reasonInput.trim()) {
      setErrorMsg('يجب إدخال سبب الفارق النقدي (العجز أو الزيادة) للمتابعة.');
      return;
    }

    if (needsApproval) {
      // Require manager authorization
      requestManagerAuth(
        `اعتماد تقفيل وردية بفارق نقدي (${difference > 0 ? `زيادة +${difference}` : `عجز ${difference}`} ج)`,
        `الوردية: ${activeShift.shiftName} - الكاشير: ${activeShift.openedByUserName} - المتوقع: ${discrepancyData.expected} ج - الفعلي: ${counted} ج - السبب: ${reasonInput || 'غير محدد'}`,
        true,
        (managerName, _approvalReason, approval) => {
          closeShiftBlind(counted, reasonInput, managerName, approval);
          setCloseModalVisible(false);
        },
        'shifts_manage'
      );
    } else {
      closeShiftBlind(counted, reasonInput, undefined);
      setCloseModalVisible(false);
    }
  };

  // Handle Open New Shift
  const handleCreateShift = (e: React.FormEvent) => {
    e.preventDefault();
    const opening = Number(newOpeningCash) || 0;
    try {
      openNewShift(newShiftName, opening, crypto.randomUUID());
      setOpenModalVisible(false);
      setErrorMsg('');
    } catch (error) {
      setErrorMsg(error instanceof Error ? error.message : 'تعذر فتح الوردية.');
    }
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header & Active Shift Status */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-amber-500/10 text-amber-600 border border-amber-500/20 flex items-center justify-center">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">إدارة الورديات وتقفيل الخزنة (التقفيل الأعمى)</h1>
            <p className="text-xs text-slate-500">
              نظام جرد وعد الخزنة بدون إظهار الرصيد المتوقع مسبقاً لمنع التلاعب وضمان الشفافية
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {activeShift ? (
            <button
              type="button"
              onClick={handleStartCloseShift}
              className="px-5 py-2.5 bg-red-600 hover:bg-red-700 active:bg-red-800 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-2"
            >
              <Lock className="w-4 h-4" />
              <span>تقفيل الوردية الحالية (جرد أعمى)</span>
            </button>
          ) : (
            <button
              type="button"
              onClick={() => setOpenModalVisible(true)}
              className="px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-2"
            >
              <Plus className="w-4 h-4" />
              <span>فتح وردية جديدة</span>
            </button>
          )}
        </div>
      </div>

      {/* 2. Active Shift Live Card (If open) */}
      {activeShift && (
        <div className="bg-linear-to-br from-slate-900 to-slate-800 text-white p-6 rounded-3xl border border-slate-700 shadow-lg space-y-4">
          <div className="flex items-center justify-between border-b border-slate-700/80 pb-3">
            <div className="flex items-center gap-3">
              <span className="w-3 h-3 rounded-full bg-emerald-400 animate-ping"></span>
              <div>
                <h2 className="text-lg font-black text-white">{activeShift.shiftName} (الوردية النشطة)</h2>
                <p className="text-xs text-slate-400">
                  تم الفتح بواسطة: {activeShift.openedByUserName} • تاريخ الفتح: {activeShift.openedAt}
                </p>
              </div>
            </div>
            <span className="bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 text-xs font-bold px-3 py-1 rounded-full">
              مفتوحة وقيد العمل
            </span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-7 gap-3 pt-1">
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">رصيد الافتتاح</span>
              <span className="text-lg font-black text-slate-200">{activeShift.openingCash} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">إجمالي المبيعات</span>
              <span className="text-lg font-black text-emerald-400">{activeShift.totalSales} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">مبيعات نقدية (كاش)</span>
              <span className="text-lg font-black text-slate-200">{activeShift.cashSales} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">مبيعات فيزا / بنك</span>
              <span className="text-lg font-black text-blue-400">{activeShift.cardSales} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">مبيعات InstaPay</span>
              <span className="text-lg font-black text-violet-400">{activeShift.instapaySales} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">المصروفات المسجلة</span>
              <span className="text-lg font-black text-red-400">{activeShift.totalExpenses} ج</span>
            </div>
            <div className="bg-slate-800/80 p-3 rounded-2xl border border-slate-700/60">
              <span className="text-[11px] text-slate-400 block font-medium">عدد الطلبات</span>
              <span className="text-lg font-black text-amber-400">{activeShift.ordersCount} طلب</span>
            </div>
          </div>
        </div>
      )}

      {/* 3. Shifts History Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <History className="w-4 h-4 text-slate-600" />
            <h3 className="font-extrabold text-sm text-slate-800">سجل إغلاق وتسوية الورديات السابقة</h3>
          </div>
          <span className="text-xs text-slate-500 font-semibold">{shifts.length} ورديات مسجلة</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3 px-4">الوردية</th>
                <th className="py-3 px-4">تاريخ الفتح / الإغلاق</th>
                <th className="py-3 px-4">الكاشير المغلق</th>
                <th className="py-3 px-4 text-center">الافتتاح</th>
                <th className="py-3 px-4 text-center">المبيعات</th>
                <th className="py-3 px-4 text-center">المصروفات</th>
                <th className="py-3 px-4 text-center">المتوقع بالدرج</th>
                <th className="py-3 px-4 text-center">الفعلي المعدود</th>
                <th className="py-3 px-4 text-center">الفارق النقدي</th>
                <th className="py-3 px-4 text-center">الحالة / الإجراء</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {shifts.map((shift) => (
                <tr key={shift.id} className="hover:bg-slate-50 transition-colors">
                  <td className="py-3.5 px-4 font-bold text-slate-900">
                    <div>{shift.shiftName}</div>
                    <div className="text-[10px] text-slate-400 font-normal">#{shift.id}</div>
                  </td>
                  <td className="py-3.5 px-4 text-slate-600">
                    <div>فتح: {shift.openedAt}</div>
                    <div className="text-slate-400">إغلاق: {shift.closedAt || 'مستمرة'}</div>
                  </td>
                  <td className="py-3.5 px-4 font-medium text-slate-800">
                    {shift.closedByUserName || shift.openedByUserName}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold">{shift.openingCash} ج</td>
                  <td className="py-3.5 px-4 text-center font-bold text-emerald-600">{shift.totalSales} ج</td>
                  <td className="py-3.5 px-4 text-center font-bold text-red-600">{shift.totalExpenses} ج</td>
                  <td className="py-3.5 px-4 text-center font-bold">
                    {shift.expectedCash !== undefined ? `${shift.expectedCash} ج` : '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-slate-900">
                    {shift.actualCash !== undefined ? `${shift.actualCash} ج` : '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    {shift.status === 'closed' ? (
                      <span
                        className={`inline-block px-2.5 py-0.5 rounded-full text-[11px] font-black ${
                          (shift.cashDifference || 0) === 0
                            ? 'bg-emerald-100 text-emerald-800'
                            : (shift.cashDifference || 0) < 0
                            ? 'bg-red-100 text-red-800'
                            : 'bg-amber-100 text-amber-800'
                        }`}
                      >
                        {(shift.cashDifference || 0) === 0
                          ? 'مطابق (0 ج)'
                          : (shift.cashDifference || 0) < 0
                          ? `عجز ${shift.cashDifference} ج`
                          : `زيادة +${shift.cashDifference} ج`}
                      </span>
                    ) : (
                      <span className="text-slate-400 font-normal">قيد العمل</span>
                    )}
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    {shift.status === 'closed' ? (
                      <button
                        type="button"
                        onClick={() => showShiftClosingReport(shift)}
                        className="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-bold transition-colors inline-flex items-center gap-1"
                      >
                        <Printer className="w-3.5 h-3.5" />
                        <span>طباعة البون</span>
                      </button>
                    ) : (
                      <span className="text-emerald-600 font-bold">نشطة</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4. BLIND SHIFT CLOSING MODAL */}
      {closeModalVisible && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200 text-slate-800 flex flex-col">
            {/* Header */}
            <div className="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-red-600/30 rounded-xl text-red-400">
                  <Lock className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-extrabold text-base">تقفيل الوردية (الجرد الأعمى للخزنة)</h3>
                  <p className="text-xs text-slate-400">{activeShift?.shiftName}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setCloseModalVisible(false)}
                className="p-1 text-slate-400 hover:text-white rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-6 space-y-5">
              {/* STEP 1: BLIND COUNT INPUT */}
              {step === 'count' && (
                <div className="space-y-4">
                  <div className="bg-amber-50 border border-amber-200 rounded-2xl p-4 text-amber-900 text-xs leading-relaxed space-y-1">
                    <div className="font-bold flex items-center gap-1.5 text-amber-800 text-sm">
                      <AlertTriangle className="w-4 h-4" />
                      <span>تنبيه أمان للخزنة:</span>
                    </div>
                    <p>
                      قم بعد كافة المبالغ النقدية الموجودة بالدرج فعلياً، واكتب المبلغ الإجمالي دون أي تقريب. سيقوم
                      النظام بمقارنته مع المبيعات والمصروفات المسجلة وحساب الفارق آلياً.
                    </p>
                  </div>

                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1.5">
                      المبلغ النقدي الفعلي الموجود بالدرج (بالجنيه):
                    </label>
                    <input
                      type="number"
                      value={countedCashInput}
                      onChange={(e) => setCountedCashInput(e.target.value)}
                      placeholder="أدخل ناتج عد الدرج (مثال: 5350)"
                      className="w-full text-2xl font-black text-slate-900 bg-slate-50 border-2 border-slate-300 rounded-2xl px-4 py-3 focus:outline-hidden focus:border-red-500 font-mono"
                      autoFocus
                    />
                  </div>

                  {errorMsg && <p className="text-xs text-red-600 font-bold text-center">{errorMsg}</p>}

                  <div className="flex gap-3 pt-2">
                    <button
                      type="button"
                      onClick={() => setCloseModalVisible(false)}
                      className="flex-1 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
                    >
                      إلغاء
                    </button>
                    <button
                      type="button"
                      onClick={handleCalculateDifference}
                      disabled={!countedCashInput}
                      className="flex-2 py-3 bg-red-600 hover:bg-red-700 disabled:opacity-40 text-white text-sm font-black rounded-xl shadow-md flex items-center justify-center gap-2"
                    >
                      <span>متابعة وحساب الفارق</span>
                    </button>
                  </div>
                </div>
              )}

              {/* STEP 2: DISCREPANCY REVELATION & APPROVAL */}
              {step === 'result' && discrepancyData && (
                <div className="space-y-4">
                  {/* Results comparison card */}
                  <div className="bg-slate-50 border border-slate-200 rounded-2xl p-4 space-y-2.5 text-xs">
                    <div className="flex justify-between items-center text-slate-600">
                      <span>الكاش المتوقع بالدرج (السيستم):</span>
                      <span className="font-bold text-slate-900 text-sm">{discrepancyData.expected} ج.م</span>
                    </div>
                    <div className="flex justify-between items-center text-slate-600">
                      <span>الكاش الفعلي المعدود (الكاشير):</span>
                      <span className="font-bold text-slate-900 text-sm">{discrepancyData.counted} ج.م</span>
                    </div>

                    <div className="pt-2 border-t border-slate-200 flex justify-between items-center">
                      <span className="font-extrabold text-sm text-slate-800">الفارق النقدي:</span>
                      <span
                        className={`text-base font-black px-3 py-1 rounded-xl ${
                          discrepancyData.difference === 0
                            ? 'bg-emerald-100 text-emerald-800'
                            : discrepancyData.difference < 0
                            ? 'bg-red-100 text-red-800 animate-pulse'
                            : 'bg-amber-100 text-amber-800'
                        }`}
                      >
                        {discrepancyData.difference === 0
                          ? 'مطابق تماماً (0 ج) ✓'
                          : discrepancyData.difference < 0
                          ? `عجز نقدي: ${Math.abs(discrepancyData.difference)} ج`
                          : `زيادة نقدية: +${discrepancyData.difference} ج`}
                      </span>
                    </div>
                  </div>

                  {/* Reason input for difference */}
                  {discrepancyData.difference !== 0 && (
                    <div className="space-y-1.5">
                      <label className="block text-xs font-bold text-red-700">
                        سبب الفارق النقدي * (مطلوب للتوثيق والاعتماد):
                      </label>
                      <textarea
                        value={reasonInput}
                        onChange={(e) => setReasonInput(e.target.value)}
                        placeholder="اكتب تفاصيل سبب العجز أو الزيادة (مثال: نسيان إدخال مصروف سريع 50 ج، فكة...)"
                        rows={2}
                        className="w-full text-xs bg-slate-50 border border-slate-300 rounded-xl p-2.5 text-slate-900 focus:outline-hidden focus:ring-2 focus:ring-red-500"
                      />
                    </div>
                  )}

                  {discrepancyData.difference !== 0 && (
                    <div className="bg-red-50 text-red-800 text-[11px] p-2.5 rounded-xl border border-red-200 flex items-center gap-2">
                      <ShieldCheck className="w-4 h-4 text-red-600 shrink-0" />
                      <span>يتطلب حفظ هذا الإغلاق موافقة المدير وإدخال رمز PIN.</span>
                    </div>
                  )}

                  {errorMsg && <p className="text-xs text-red-600 font-bold text-center">{errorMsg}</p>}

                  {/* Action buttons */}
                  <div className="flex gap-3 pt-2">
                    <button
                      type="button"
                      onClick={() => setStep('count')}
                      className="flex-1 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
                    >
                      إعادة العد
                    </button>
                    <button
                      type="button"
                      onClick={handleFinalizeClose}
                      className="flex-2 py-3 bg-red-600 hover:bg-red-700 text-white text-sm font-black rounded-xl shadow-md flex items-center justify-center gap-2"
                    >
                      <Check className="w-4 h-4" />
                      <span>تأكيد إغلاق الوردية وطباعة التقرير</span>
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* 5. OPEN NEW SHIFT MODAL */}
      {openModalVisible && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleCreateShift}
            className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-base text-slate-900">افتتاح وردية جديدة</h3>
              <button
                type="button"
                onClick={() => setOpenModalVisible(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">اسم الوردية:</label>
              <select
                value={newShiftName}
                onChange={(e) => setNewShiftName(e.target.value)}
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl p-2.5 font-bold text-slate-800"
              >
                <option value="وردية صباحية">وردية صباحية (08:00 ص - 04:00 م)</option>
                <option value="وردية مسائية">وردية مسائية (04:00 م - 12:00 ص)</option>
                <option value="وردية ليلية">وردية ليلية (12:00 ص - 08:00 ص)</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">
                رصيد العهدة النقدية الافتتاحية (الفكة بالدرج):
              </label>
              <input
                type="number"
                value={newOpeningCash}
                onChange={(e) => setNewOpeningCash(e.target.value)}
                placeholder="500"
                className="w-full text-base font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-slate-900"
              />
            </div>

            <div className="bg-slate-50 p-2.5 rounded-xl text-xs text-slate-600">
              الكاشير الحالي المسؤول: <span className="font-bold text-slate-900">{currentUser.name}</span>
            </div>
            {errorMsg && <p className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-xl p-3 font-bold">{errorMsg}</p>}

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setOpenModalVisible(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                فتح الوردية
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
