'use client';

import React, { useMemo, useState } from 'react';
import { ArrowDownCircle, ArrowUpCircle, LockKeyhole, Plus, Vault, X } from 'lucide-react';
import { useAppStore } from '@/lib/store';

export default function CashScreen() {
  const { activeShift, cashMovements, expenses, calculateShiftExpectedCash, addCashMovement, requestManagerAuth } = useAppStore();
  const [open, setOpen] = useState(false);
  const [type, setType] = useState<'cash_in' | 'cash_out'>('cash_in');
  const [amount, setAmount] = useState('');
  const [reason, setReason] = useState('');
  const expected = activeShift ? calculateShiftExpectedCash(activeShift.id) : 0;
  const movements = useMemo(() => activeShift ? cashMovements.filter((m) => m.shiftId === activeShift.id) : [], [activeShift, cashMovements]);
  const drawerExpenses = useMemo(() => activeShift ? expenses.filter((e) => e.shiftId === activeShift.id && e.paidFromDrawer) : [], [activeShift, expenses]);

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const value = Number(amount);
    if (!activeShift || value <= 0 || !reason.trim()) return;
    requestManagerAuth(
      type === 'cash_in' ? 'اعتماد إيداع نقدية' : 'اعتماد سحب نقدية',
      `${reason} — ${value} ج.م`,
      true,
      (managerName, _approvalReason, approval) => {
        addCashMovement(type, value, reason.trim(), crypto.randomUUID(), managerName, approval);
        setOpen(false); setAmount(''); setReason('');
      },
      'cash_drawer'
    );
  };

  return <div className="h-full overflow-y-auto bg-slate-100 p-4 lg:p-6 space-y-5" dir="rtl">
    <section className="rounded-3xl bg-slate-950 text-white p-6 shadow-xl overflow-hidden relative">
      <div className="absolute -left-12 -top-12 w-48 h-48 rounded-full bg-emerald-500/15" />
      <div className="relative flex flex-col md:flex-row md:items-center justify-between gap-5">
        <div className="flex items-center gap-4"><div className="w-14 h-14 rounded-2xl bg-emerald-500 flex items-center justify-center"><Vault className="w-7 h-7" /></div><div><p className="text-slate-400 text-xs font-bold">العهدة النقدية المتوقعة الآن</p><h1 className="text-3xl font-black mt-1">{expected.toLocaleString('ar-EG')} ج.م</h1><p className="text-xs text-slate-400 mt-1">{activeShift ? activeShift.shiftName : 'لا توجد وردية مفتوحة'}</p></div></div>
        <button disabled={!activeShift} onClick={() => setOpen(true)} className="px-5 py-3 rounded-2xl bg-white text-slate-950 font-black text-sm disabled:opacity-40 flex items-center justify-center gap-2"><Plus className="w-4 h-4" /> حركة خزنة معتمدة</button>
      </div>
    </section>

    <div className="grid lg:grid-cols-2 gap-4">
      <section className="bg-white rounded-3xl border border-slate-200 overflow-hidden"><header className="p-4 border-b font-black">حركات الوردية</header><div className="divide-y max-h-[430px] overflow-y-auto">{movements.map(m => <div key={m.id} className="p-4 flex items-center justify-between"><div className="flex items-center gap-3">{m.type === 'cash_in' ? <ArrowUpCircle className="text-emerald-600" /> : <ArrowDownCircle className="text-red-600" />}<div><p className="font-bold text-sm">{m.reason}</p><p className="text-[11px] text-slate-400">{m.time} · {m.userName}</p></div></div><strong className={m.type === 'cash_in' ? 'text-emerald-700' : 'text-red-700'}>{m.type === 'cash_in' ? '+' : '-'}{m.amount} ج</strong></div>)}{movements.length === 0 && <p className="p-8 text-center text-slate-400 text-sm">لا توجد حركات خزنة في الوردية</p>}</div></section>
      <section className="bg-white rounded-3xl border border-slate-200 overflow-hidden"><header className="p-4 border-b font-black">مصروفات خُصمت من الدرج</header><div className="divide-y max-h-[430px] overflow-y-auto">{drawerExpenses.map(e => <div key={e.id} className="p-4 flex justify-between gap-3"><div><p className="font-bold text-sm">{e.description}</p><p className="text-[11px] text-slate-400">{e.category} · {e.time}</p></div><strong className="text-red-700">-{e.amount} ج</strong></div>)}{drawerExpenses.length === 0 && <p className="p-8 text-center text-slate-400 text-sm">لا توجد مصروفات نقدية</p>}</div></section>
    </div>

    {open && <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4"><form onSubmit={submit} className="w-full max-w-md bg-white rounded-3xl p-6 space-y-4 shadow-2xl"><div className="flex items-center justify-between"><div><h2 className="font-black text-lg">حركة خزنة جديدة</h2><p className="text-xs text-slate-500">تحتاج اعتماد مدير وسببًا واضحًا</p></div><button type="button" onClick={() => setOpen(false)}><X /></button></div><div className="grid grid-cols-2 gap-2"><button type="button" onClick={() => setType('cash_in')} className={`p-3 rounded-xl font-bold ${type === 'cash_in' ? 'bg-emerald-600 text-white' : 'bg-slate-100'}`}>إيداع</button><button type="button" onClick={() => setType('cash_out')} className={`p-3 rounded-xl font-bold ${type === 'cash_out' ? 'bg-red-600 text-white' : 'bg-slate-100'}`}>سحب</button></div><input type="number" min="0.01" step="0.01" value={amount} onChange={e => setAmount(e.target.value)} placeholder="المبلغ" className="w-full p-3 rounded-xl border bg-slate-50 text-xl font-black" required /><textarea value={reason} onChange={e => setReason(e.target.value)} placeholder="سبب الحركة" className="w-full p-3 rounded-xl border bg-slate-50 min-h-24" required /><button className="w-full p-3 bg-slate-950 text-white rounded-xl font-black flex items-center justify-center gap-2"><LockKeyhole className="w-4 h-4" /> طلب اعتماد وحفظ</button></form></div>}
  </div>;
}
