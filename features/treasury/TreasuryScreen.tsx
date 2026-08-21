'use client';

import React, { useMemo, useRef, useState } from 'react';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import { useAppStore } from '@/lib/store';
import { createOperationId } from '@/lib/financial-posting';
import type { ManagerApproval } from '@/lib/authorization.mjs';
import { ArrowLeftRight, Landmark, Plus, ShieldCheck, WalletCards } from 'lucide-react';

const pounds = (value: number) => `${(value / 100).toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج`;

export default function TreasuryScreen() {
  const store = useEnterpriseStore(); const app = useAppStore();
  const [source, setSource] = useState('tre-drawer'); const [destination, setDestination] = useState('tre-main');
  const [amount, setAmount] = useState(''); const [reason, setReason] = useState('تسليم نقدية الوردية');
  const [mode, setMode] = useState<'deposit' | 'withdrawal'>('deposit'); const [fundAmount, setFundAmount] = useState('');
  const [fundReason, setFundReason] = useState(''); const [reference, setReference] = useState(''); const [error, setError] = useState('');
  const openDay = store.businessDays.find(x => x.status !== 'closed');
  const total = useMemo(() => store.treasuryAccounts.reduce((sum, account) => sum + store.treasuryBalance(account.id), 0), [store]);
  const mainBalance = store.treasuryBalance('tre-main'); const fundingPiastres = Math.round(Number(fundAmount || 0) * 100);
  const transferRequestId = useRef(crypto.randomUUID());

  const transfer = () => {
    const value = Math.round(Number(amount) * 100); 
    if (!openDay || source === destination || value <= 0 || !reason.trim()) return;
    const execute = (approval?: ManagerApproval) => { try { const movement = store.transferTreasury({ requestId: transferRequestId.current, sourceAccountId: source, destinationAccountId: destination, amountPiastres: value, reason, actor: app.currentUser, approval });
      store.createHandover({ fromAccountId: source, toAccountId: destination, amount: value, handedBy: app.currentUser.name, receivedBy: approval?.approverName || app.currentUser.name, notes: reason, treasuryMovementId: movement.id });
      app.logAuditEvent({ userId: app.currentUser.id, userName: app.currentUser.name, userRole: app.currentUser.role, action: 'تحويل خزنة', category: 'cash', details: `${value / 100} ج.م من ${source} إلى ${destination} - ${reason}`, severity: 'normal', operationId: movement.operationId, referenceId: movement.id, authorizedBy: approval?.approverName });
      transferRequestId.current = crypto.randomUUID(); setAmount(''); setError('');
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'تعذر تسجيل تحويل الخزينة.'); } };
    if (['owner', 'manager'].includes(app.currentUser.role)) execute(); else app.requestManagerAuth('اعتماد تحويل خزينة', `${value / 100} جنيه - ${reason}`, true, (_name, _reason, approval) => execute(approval), 'treasury_manage');
  };

  const submitFunding = () => { if (fundingPiastres <= 0 || !fundReason.trim()) return; const commandId = createOperationId(mode);
    const execute = (approval?: ManagerApproval) => { const approvedBy=approval?.approverName; try { const movement = mode === 'deposit'
      ? store.postExternalFunding({ operationId: createOperationId('funding'), depositId: commandId, type: 'owner_deposit', amountPiastres: fundingPiastres, destinationAccountId: 'tre-main', reason: fundReason.trim(), reference: reference.trim() || undefined, createdBy: app.currentUser.name, approvedBy, actor: app.currentUser, approval })
      : store.postOwnerWithdrawal({ operationId: createOperationId('withdrawal'), withdrawalId: commandId, amountPiastres: fundingPiastres, sourceAccountId: 'tre-main', reason: fundReason.trim(), reference: reference.trim() || undefined, createdBy: app.currentUser.name, approvedBy, actor: app.currentUser, approval });
      app.logAuditEvent({ userId: app.currentUser.id, userName: app.currentUser.name, userRole: app.currentUser.role, action: mode === 'deposit' ? `${app.currentUser.name} أودع ${fundAmount} جنيه في الخزينة الرئيسية` : `${app.currentUser.name} سحب ${fundAmount} جنيه من الخزينة الرئيسية`, category: 'cash', details: `${fundReason}${reference ? ` - المرجع: ${reference}` : ''}`, severity: mode === 'deposit' ? 'normal' : 'warning', operationId: movement.operationId, referenceId: movement.referenceId, authorizedBy: approvedBy });
      setFundAmount(''); setFundReason(''); setReference(''); setError(''); } catch (cause) { setError(cause instanceof Error ? cause.message : 'تعذر تسجيل الحركة المالية.'); } };
    if (['owner', 'manager'].includes(app.currentUser.role)) execute(); else app.requestManagerAuth(mode === 'deposit' ? 'اعتماد إيداع مالك' : 'اعتماد سحب مالك', `${fundAmount} جنيه - ${fundReason}`, true, (_managerName,_reason,approval) => execute(approval), 'treasury_manage'); };

  return <div className="space-y-5 bg-slate-100 p-6" dir="rtl">
    <div className="flex items-center justify-between rounded-3xl border bg-white p-5 shadow-xs"><div className="flex items-center gap-3"><div className="rounded-2xl bg-emerald-50 p-3 text-emerald-700"><Landmark /></div><div><h1 className="text-xl font-black">الخزينة والعُهد</h1><p className="text-xs text-slate-500">كل مبلغ مرتبط بمصدر ووجهة ومسؤول ومرجع</p></div></div><div className="text-left"><p className="text-xs text-slate-500">صافي الأموال المسجلة</p><p className="text-2xl font-black text-emerald-700">{pounds(total)}</p></div></div>
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">{store.treasuryAccounts.map(account => <div key={account.id} className="rounded-2xl border bg-white p-4"><WalletCards className="mb-3 h-5 w-5 text-blue-600"/><p className="text-xs font-bold text-slate-500">{account.name}</p><p className="mt-1 text-xl font-black">{pounds(store.treasuryBalance(account.id))}</p></div>)}</div>
    <div className="grid gap-4 lg:grid-cols-[380px_1fr]"><div className="space-y-4">
      <div className="space-y-3 rounded-3xl border bg-white p-5"><h2 className="flex items-center gap-2 font-black"><ArrowLeftRight className="h-4 w-4"/>تحويل وتسليم عهدة</h2><select value={source} onChange={e => setSource(e.target.value)} className="w-full rounded-xl border p-3 text-sm">{store.treasuryAccounts.map(x => <option key={x.id} value={x.id}>من: {x.name}</option>)}</select><select value={destination} onChange={e => setDestination(e.target.value)} className="w-full rounded-xl border p-3 text-sm">{store.treasuryAccounts.map(x => <option key={x.id} value={x.id}>إلى: {x.name}</option>)}</select><input type="number" value={amount} onChange={e => setAmount(e.target.value)} placeholder="المبلغ بالجنيه" className="w-full rounded-xl border p-3"/><input value={reason} onChange={e => setReason(e.target.value)} placeholder="السبب" className="w-full rounded-xl border p-3 text-sm"/><button onClick={transfer} className="flex w-full items-center justify-center gap-2 rounded-xl bg-[#0F52BA] p-3 text-sm font-black text-white"><Plus className="h-4 w-4"/>تسجيل التحويل والتسليم</button></div>
      <div className="space-y-3 rounded-3xl border bg-white p-5"><h2 className="font-black">تمويل الخزينة وحقوق المالك</h2><div className="grid grid-cols-2 gap-2"><button onClick={() => setMode('deposit')} className={`rounded-xl p-2 text-sm font-bold ${mode === 'deposit' ? 'bg-emerald-600 text-white' : 'bg-slate-100'}`}>إيداع مالك</button><button onClick={() => setMode('withdrawal')} className={`rounded-xl p-2 text-sm font-bold ${mode === 'withdrawal' ? 'bg-red-600 text-white' : 'bg-slate-100'}`}>سحب مالك</button></div>{mode === 'withdrawal' && <div className="rounded-xl bg-slate-50 p-3 text-xs"><p>الرصيد الحالي: <b>{pounds(mainBalance)}</b></p><p>الرصيد بعد العملية: <b className={mainBalance - fundingPiastres < 0 ? 'text-red-600' : ''}>{pounds(mainBalance - fundingPiastres)}</b></p></div>}<input type="number" min="0.01" step="0.01" value={fundAmount} onChange={e => setFundAmount(e.target.value)} placeholder="المبلغ بالجنيه" className="w-full rounded-xl border p-3"/><input value={fundReason} onChange={e => setFundReason(e.target.value)} placeholder="السبب" className="w-full rounded-xl border p-3 text-sm"/><input value={reference} onChange={e => setReference(e.target.value)} placeholder="المرجع (اختياري)" className="w-full rounded-xl border p-3 text-sm"/>{error && <p className="text-xs font-bold text-red-600">{error}</p>}<button onClick={submitFunding} className={`w-full rounded-xl p-3 text-sm font-black text-white ${mode === 'deposit' ? 'bg-emerald-600' : 'bg-red-600'}`}>{mode === 'deposit' ? 'تسجيل إيداع المالك' : 'تسجيل سحب المالك'}</button></div>
    </div><div className="overflow-hidden rounded-3xl border bg-white"><div className="flex items-center gap-2 border-b bg-slate-50 p-4 font-black"><ShieldCheck className="h-4 w-4"/>دفتر الحركات غير القابل للمسح</div><div className="divide-y">{store.treasuryMovements.length === 0 ? <p className="p-10 text-center text-sm text-slate-400">لا توجد حركات بعد</p> : store.treasuryMovements.map(m => <div key={m.id} className="grid grid-cols-4 gap-2 p-4 text-xs"><b>{m.type}</b><span>{pounds(m.amount)}</span><span className="text-slate-500">{m.reason}</span><span dir="ltr" className="text-left text-slate-400">{new Date(m.createdAt).toLocaleString('ar-EG')}</span></div>)}</div></div></div>
  </div>;
}
