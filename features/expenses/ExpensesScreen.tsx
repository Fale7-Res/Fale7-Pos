'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Expense } from '@/lib/types';
import { Coins, Plus, DollarSign, Calendar, User, FileText, X, Check, ArrowDownRight, Tag } from 'lucide-react';

export default function ExpensesScreen() {
  const { expenses, addExpense, activeShift, currentUser, requestManagerAuth } = useAppStore();
  const [modalOpen, setModalOpen] = useState(false);
  const [category, setCategory] = useState('مستلزمات مطبخ وخضار');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [paymentAccountId, setPaymentAccountId] = useState<'tre-drawer' | 'tre-main' | 'tre-bank' | 'tre-instapay'>('tre-drawer');

  const handleAddExpense = (e: React.FormEvent) => {
    e.preventDefault();
    const amt = Number(amount) || 0;
    if (amt <= 0 || !description) return;

    const requestId = crypto.randomUUID();
    const save = (approvedBy?: string, approval?: Parameters<typeof addExpense>[6]) => {
      addExpense(category, amt, description, paymentAccountId, requestId, approvedBy, approval);
      setModalOpen(false);
      setAmount('');
      setDescription('');
    };
    if (!activeShift) return;
    if (!['owner', 'manager'].includes(currentUser.role)) {
      requestManagerAuth('اعتماد مصروف من الخزنة', `${description} — ${amt} ج.م`, true, (managerName, _reason, approval) => save(managerName, approval), 'expenses_add');
    } else save();
  };

  const totalExpenseSum = expenses.reduce((sum, e) => sum + e.amount, 0);

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-amber-500/10 text-amber-600 border border-amber-500/20 flex items-center justify-center">
            <Coins className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">سجل المصروفات النثرية والتشغيلية</h1>
            <p className="text-xs text-slate-500">
              تسجيل النثريات، المشتريات العاجلة، الغاز، والصيانة وخصمها من رصيد الخزنة والوردية
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={() => setModalOpen(true)}
          disabled={!activeShift}
          className="px-5 py-2.5 bg-red-600 hover:bg-red-700 active:bg-red-800 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-1.5"
        >
          <Plus className="w-4 h-4" />
          <span>{activeShift ? 'تسجيل مصروف جديد' : 'افتح وردية أولًا'}</span>
        </button>
      </div>

      {/* 2. Quick Summary Card */}
      <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs flex items-center justify-between">
        <div>
          <span className="text-xs font-bold text-slate-400">إجمالي المصروفات المسجلة اليوم:</span>
          <div className="text-3xl font-black text-red-600 mt-1">{totalExpenseSum} ج.م</div>
        </div>
        <div className="text-xs text-slate-500 text-left">
          <span>الوردية الحالية: </span>
          <span className="font-bold text-slate-800">{activeShift?.shiftName || 'لا توجد وردية'}</span>
        </div>
      </div>

      {/* 3. Expenses Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">التاريخ والوقت</th>
                <th className="py-3.5 px-4">التصنيف</th>
                <th className="py-3.5 px-4">البيان والتفاصيل</th>
                <th className="py-3.5 px-4 text-center">المبلغ</th>
                <th className="py-3.5 px-4 text-center">الخصم من الدرج</th>
                <th className="py-3.5 px-4">المسجل</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {expenses.map((exp) => (
                <tr key={exp.id} className="hover:bg-slate-50 transition-colors">
                  <td className="py-3.5 px-4 text-slate-500 font-mono text-[11px]">{exp.date} {exp.time}</td>
                  <td className="py-3.5 px-4">
                    <span className="inline-block px-2.5 py-0.5 rounded-lg text-[11px] font-bold bg-amber-50 text-amber-800 border border-amber-200">
                      {exp.category}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 font-bold text-slate-800">{exp.description}</td>
                  <td className="py-3.5 px-4 text-center font-black text-red-600 text-sm">{exp.amount} ج</td>
                  <td className="py-3.5 px-4 text-center font-bold">
                    {exp.paidFromDrawer ? (
                      <span className="text-emerald-700">نعم (من الخزنة)</span>
                    ) : (
                      <span className="text-slate-400">لا (حساب خارجي)</span>
                    )}
                  </td>
                  <td className="py-3.5 px-4 text-slate-600 font-medium">{exp.userName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4. ADD EXPENSE MODAL */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleAddExpense}
            className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-base text-slate-900">تسجيل مصروف جديد</h3>
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">تصنيف المصروف:</label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2.5"
              >
                <option value="مستلزمات مطبخ وخضار">مستلزمات مطبخ وخضار</option>
                <option value="غاز وفحم">غاز وفحم وشواية</option>
                <option value="أكياس وتغليف">أكياس وورق وتغليف</option>
                <option value="نظافة ومطهرات">نظافة ومطهرات</option>
                <option value="مواصلات ودليفري">مواصلات وبنزين</option>
                <option value="صيانة سريعة">صيانة سريعة وأدوات</option>
                <option value="أخرى">نثريات أخرى</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">المبلغ (بالجنيه):</label>
              <input
                type="number"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="أدخل المبلغ"
                className="w-full text-xl font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                required
                autoFocus
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">البيان والتفاصيل:</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="مثال: شراء كزبرة وطماطم من سنتر الأردنية..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800"
                required
              />
            </div>

            <div className="pt-1">
              <label className="mb-1 block text-xs font-bold text-slate-700">حساب الدفع:</label>
              <select value={paymentAccountId} onChange={(e) => setPaymentAccountId(e.target.value as typeof paymentAccountId)} className="w-full rounded-xl border border-slate-200 bg-slate-50 p-2.5 text-xs font-bold">
                <option value="tre-drawer">درج الكاشير</option>
                <option value="tre-main">الخزينة الرئيسية</option>
                <option value="tre-bank">الحساب البنكي</option>
                <option value="tre-instapay">InstaPay</option>
              </select>
            </div>

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
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                حفظ المصروف
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
