'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Employee, LedgerEntryType } from '@/lib/types';
import {
  Wallet,
  Clock,
  Plus,
  Coins,
  ArrowDownRight,
  ArrowUpRight,
  FileText,
  User,
  Check,
  X,
  AlertCircle,
  Sparkles,
  HelpCircle,
} from 'lucide-react';

export default function DailyWagesPanel() {
  const {
    employees,
    calculateEmployeeBalance,
    recordDailyWage,
    recordOvertime,
    recordExtraShift,
    recordAdvance,
    recordWithdrawal,
    currentUser,
    settings,
    activeShift,
    requestManagerAuth,
  } = useAppStore();

  // Quick Action Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedEmp, setSelectedEmp] = useState<Employee | null>(null);
  const [actionType, setActionType] = useState<LedgerEntryType>('advance');
  const [amountInput, setAmountInput] = useState<string>('');
  const [hoursInput, setHoursInput] = useState<string>('2');
  const [notesInput, setNotesInput] = useState<string>('');

  const activeEmployees = employees.filter((e) => e.isActive);

  // Open modal for specific quick action
  const handleOpenAction = (emp: Employee, type: LedgerEntryType) => {
    setSelectedEmp(emp);
    setActionType(type);
    setNotesInput('');

    if (type === 'daily_wage') {
      setAmountInput(String(emp.shiftValue || emp.dailyWage || 200));
    } else if (type === 'extra_shift') {
      setAmountInput(String(emp.dailyWage || 200));
    } else if (type === 'overtime') {
      setHoursInput('2');
      const hourly = settings.overtimeHourlyRate;
      setAmountInput(String(hourly * 2));
    } else if (type === 'advance') {
      setAmountInput('100');
    } else if (type === 'withdrawal') {
      setAmountInput('200');
    } else {
      setAmountInput('50');
    }

    setModalOpen(true);
  };

  // Submit quick action
  const handleConfirmAction = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedEmp) return;

    const amt = Number(amountInput) || 0;
    const hrs = Number(hoursInput) || 0;
    if (amt <= 0 || (actionType === 'overtime' && hrs <= 0)) return;
    if ((actionType === 'advance' || actionType === 'withdrawal') && !activeShift) {
      setNotesInput('يجب فتح وردية قبل صرف أي مبلغ نقدي.');
      return;
    }
    if (actionType === 'withdrawal' && amt > calculateEmployeeBalance(selectedEmp.id)) {
      setNotesInput('السحب لا يمكن أن يتجاوز الرصيد المتاح. استخدم «سلفة» إذا كان المطلوب إنشاء مديونية.');
      return;
    }

    const performSubmission = (authBy?: string) => {
      if (actionType === 'daily_wage') {
        recordDailyWage(selectedEmp.id, amt);
      } else if (actionType === 'overtime') {
        recordOvertime(selectedEmp.id, hrs, undefined, notesInput || undefined);
      } else if (actionType === 'extra_shift') {
        recordExtraShift(selectedEmp.id, 1, notesInput || undefined);
      } else if (actionType === 'advance') {
        recordAdvance(selectedEmp.id, amt, notesInput || 'سلفة نقدية', authBy);
      } else if (actionType === 'withdrawal') {
        recordWithdrawal(selectedEmp.id, amt, notesInput || 'سحب من الرصيد', authBy);
      }
      setModalOpen(false);
    };

    // Every cash payout requires explicit manager authorization for non-management users.
    if ((actionType === 'advance' || actionType === 'withdrawal') && !['owner', 'manager'].includes(currentUser.role)) {
      requestManagerAuth(
        `صرف ${actionType === 'advance' ? 'سلفة' : 'سحب'} للموظف ${selectedEmp.name} بقيمة ${amt} ج`,
        `طلب الكاشير صرف ${amt} ج نقداً من الخزنة للموظف ${selectedEmp.name} - السبب: ${notesInput || 'بدون سبب'}`,
        true,
        (managerName) => performSubmission(managerName)
      );
    } else {
      performSubmission(currentUser.name);
    }
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-amber-500/10 text-amber-600 border border-amber-500/20 flex items-center justify-center">
            <Wallet className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-black text-slate-900">لوحة يوميات وسلف العمال السريعة</h1>
              <span className="text-xs bg-amber-100 text-amber-800 font-bold px-2 py-0.5 rounded-full">
                دفتر اليوميات الرقمي
              </span>
            </div>
            <p className="text-xs text-slate-500">
              تسجيل اليوميات، الساعات الإضافية، السلف، والسحوبات بلمسة واحدة مع تحديث فوري لكشف الحساب
            </p>
          </div>
        </div>
      </div>

      {/* 2. Workers Table / Fast Touch Sheet */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <span className="text-xs font-bold text-slate-700">قائمة موظفي المطعم واليوميات المباشرة</span>
          <span className="text-xs text-slate-400">{activeEmployees.length} عمال وموظفين</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">الموظف</th>
                <th className="py-3.5 px-4">الوظيفة / القسم</th>
                <th className="py-3.5 px-4">الوردية</th>
                <th className="py-3.5 px-4 text-center">اليومية المقررة</th>
                <th className="py-3.5 px-4 text-center">الساعة الإضافية</th>
                <th className="py-3.5 px-4 text-center">الرصيد التراكمي المستحق</th>
                <th className="py-3.5 px-4 text-center">الإجراءات السريعة لليوم</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {activeEmployees.map((emp) => {
                const balance = calculateEmployeeBalance(emp.id);
                return (
                  <tr key={emp.id} className="hover:bg-slate-50 transition-colors">
                    {/* Employee info */}
                    <td className="py-3.5 px-4">
                      <div className="font-extrabold text-slate-900 text-sm">{emp.name}</div>
                      <div className="text-[10px] text-slate-400 font-mono" dir="ltr">
                        {emp.phone}
                      </div>
                    </td>

                    {/* Role */}
                    <td className="py-3.5 px-4">
                      <span className="inline-block px-2 py-0.5 bg-slate-100 text-slate-700 rounded-md font-semibold text-[11px]">
                        {emp.jobTitle}
                      </span>
                    </td>

                    {/* Shift */}
                    <td className="py-3.5 px-4 text-slate-600 font-medium">وردية 12 ساعة</td>

                    {/* Base Daily Wage */}
                    <td className="py-3.5 px-4 text-center font-bold text-slate-900">{emp.dailyWage || 0} ج</td>

                    {/* Overtime rate */}
                    <td className="py-3.5 px-4 text-center text-slate-600 font-medium">
                      {settings.overtimeHourlyRate} ج/ساعة
                    </td>

                    {/* Cumulative Balance */}
                    <td className="py-3.5 px-4 text-center">
                      <span
                        className={`inline-block px-2.5 py-1 rounded-xl text-xs font-black ${
                          balance > 0
                            ? 'bg-emerald-100 text-emerald-800'
                            : balance < 0
                            ? 'bg-red-100 text-red-800'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {balance > 0
                          ? `له: +${balance} ج`
                          : balance < 0
                          ? `عليه: ${balance} ج`
                          : 'خالص (0 ج)'}
                      </span>
                    </td>

                    {/* Action buttons (Touch-friendly pills) */}
                    <td className="py-3.5 px-4">
                      <div className="flex items-center justify-center gap-1.5 flex-wrap">
                        {/* + Daily Wage */}
                        <button
                          type="button"
                          onClick={() => handleOpenAction(emp, 'daily_wage')}
                          className="px-2.5 py-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-200 rounded-xl font-bold text-[11px] transition-colors flex items-center gap-1"
                          title="تسجيل يومية عمل كاملة"
                        >
                          <Plus className="w-3 h-3" />
                          <span>+يومية ({emp.dailyWage}ج)</span>
                        </button>

                        {/* + Overtime */}
                        <button
                          type="button"
                          onClick={() => handleOpenAction(emp, 'overtime')}
                          className="px-2.5 py-1.5 bg-blue-50 hover:bg-blue-100 text-blue-700 border border-blue-200 rounded-xl font-bold text-[11px] transition-colors flex items-center gap-1"
                          title="تسجيل ساعات عمل إضافية"
                        >
                          <Clock className="w-3 h-3" />
                          <span>+إضافي</span>
                        </button>

                        {/* + Extra Shift */}
                        <button
                          type="button"
                          onClick={() => handleOpenAction(emp, 'extra_shift')}
                          className="px-2.5 py-1.5 bg-purple-50 hover:bg-purple-100 text-purple-700 border border-purple-200 rounded-xl font-bold text-[11px] transition-colors flex items-center gap-1"
                          title="تسجيل تطبيق وردية ثانية كاملة"
                        >
                          <Sparkles className="w-3 h-3" />
                          <span>+تطبيق</span>
                        </button>

                        {/* - Advance (سلفة) */}
                        <button
                          type="button"
                          onClick={() => handleOpenAction(emp, 'advance')}
                          className="px-2.5 py-1.5 bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 rounded-xl font-bold text-[11px] transition-colors flex items-center gap-1"
                          title="صرف سلفة نقدية من الخزنة"
                        >
                          <Coins className="w-3 h-3" />
                          <span>-سلفة</span>
                        </button>

                        {/* - Withdrawal (سحب) */}
                        <button
                          type="button"
                          onClick={() => handleOpenAction(emp, 'withdrawal')}
                          className="px-2.5 py-1.5 bg-red-50 hover:bg-red-100 text-red-700 border border-red-200 rounded-xl font-bold text-[11px] transition-colors flex items-center gap-1"
                          title="سحب من رصيد الموظف"
                        >
                          <ArrowDownRight className="w-3 h-3" />
                          <span>-سحب</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* 3. MODAL FOR CONFIRMING WAGE / ADVANCE / OVERTIME ACTION */}
      {modalOpen && selectedEmp && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleConfirmAction}
            className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <div>
                <span className="text-xs font-bold text-amber-600 block">
                  {actionType === 'daily_wage'
                    ? 'تسجيل يومية عمل مستحقة (+)'
                    : actionType === 'overtime'
                    ? 'تسجيل ساعات إضافية (+)'
                    : actionType === 'extra_shift'
                    ? 'تسجيل تطبيق وردية (+)'
                    : actionType === 'advance'
                    ? 'صرف سلفة نقدية (-)'
                    : 'سحب نقدي من الرصيد (-)'}
                </span>
                <h3 className="font-extrabold text-base text-slate-900">{selectedEmp.name}</h3>
              </div>
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Overtime hours field */}
            {actionType === 'overtime' && (
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">عدد الساعات الإضافية:</label>
                <div className="flex gap-2">
                  {[1, 2, 3, 4, 5].map((h) => (
                    <button
                      key={h}
                      type="button"
                      onClick={() => {
                        setHoursInput(String(h));
                        const rate = settings.overtimeHourlyRate;
                        setAmountInput(String(rate * h));
                      }}
                      className={`flex-1 py-2 text-xs font-bold rounded-xl border transition-all ${
                        hoursInput === String(h)
                          ? 'bg-blue-600 text-white border-blue-600 shadow-xs'
                          : 'bg-slate-50 border-slate-200 text-slate-700 hover:bg-slate-100'
                      }`}
                    >
                      {h} ساعات
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Amount input */}
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">المبلغ المالي (بالجنيه):</label>
              <input
                type="number"
                value={amountInput}
                onChange={(e) => setAmountInput(e.target.value)}
                placeholder="أدخل المبلغ"
                className="w-full text-xl font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                autoFocus
              />
            </div>

            {/* Notes input */}
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">ملاحظة أو بيان الحركة (اختياري):</label>
              <input
                type="text"
                value={notesInput}
                onChange={(e) => setNotesInput(e.target.value)}
                placeholder="مثال: سلفة عاجلة، ضغط شغل مسائي..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800"
              />
            </div>

            {/* Hint message */}
            {(actionType === 'advance' || actionType === 'withdrawal') && (
              <p className="text-[11px] text-amber-800 bg-amber-50 p-2.5 rounded-xl border border-amber-200">
                💡 سيتم خصم هذا المبلغ من رصيد الموظف وصرفه نقداً من عهدة الدرج مع توثيق الحركة.
              </p>
            )}

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-amber-600 hover:bg-amber-700 text-white text-xs font-bold rounded-xl shadow-md flex items-center justify-center gap-1.5"
              >
                <Check className="w-4 h-4" />
                <span>حفظ الحركة فوراً</span>
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
