'use client';

import React, { useState } from 'react';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import { AlertTriangle, Boxes, ClipboardCheck, Plus, Trash2 } from 'lucide-react';

const qty = (m: number) => (m / 1000).toLocaleString('ar-EG', { maximumFractionDigits: 3 });

export default function InventoryScreen() {
  const s = useEnterpriseStore();
  const [tab, setTab] = useState<'balances'|'count'|'waste'>('balances');
  const [name, setName] = useState('');
  const [itemId, setItemId] = useState(s.inventoryItems[0]?.id || '');
  const [actual, setActual] = useState('');
  const [waste, setWaste] = useState('');
  const [reason, setReason] = useState('');
  const selected = s.inventoryItems.find(x => x.id === itemId);
  const expected = selected ? s.stockBalance(selected.id) : 0;
  const actualMilli = Math.round(Number(actual) * 1000);
  const variance = actualMilli - expected;

  const addItem = () => { if (!name.trim()) return; s.addInventoryItem({ name:name.trim(), category:'خامات', unit:'piece', minimumMilliUnits:0, active:true }); setName(''); };
  const approveCount = () => { if (!selected || !actual || variance===0 || !reason.trim()) return; s.postStockMovement({ inventoryItemId:selected.id, type:'count_adjustment', direction:variance>0?'in':'out', quantityMilliUnits:Math.abs(variance), reason, createdBy:'المستخدم الحالي', approvedBy:'مدير المخزون' }); setActual(''); setReason(''); };
  const recordWaste = () => { const amount=Math.round(Number(waste)*1000); if(!selected||amount<=0||!reason.trim())return; s.postStockMovement({inventoryItemId:selected.id,type:'waste',direction:'out',quantityMilliUnits:amount,reason,createdBy:'المستخدم الحالي',approvedBy:amount>=10000?'المدير':undefined});setWaste('');setReason('');};

  return <div className="space-y-5 bg-slate-100 p-6" dir="rtl">
    <div className="rounded-2xl border border-amber-200 bg-amber-50 p-3 text-xs font-bold text-amber-900">سجل المخزون المشترى والجرد اليدوي مُفعّل. الاستهلاك التلقائي من مبيعات POS غير مُعدّ حتى اعتماد الوصفات/BOM.</div>
    <div className="flex items-center justify-between rounded-3xl border bg-white p-5"><div className="flex items-center gap-3"><div className="rounded-2xl bg-emerald-50 p-3 text-emerald-700"><Boxes/></div><div><h1 className="text-xl font-black">المخزون والجرد والهالك</h1><p className="text-xs text-slate-500">لا تعديل مباشر للأرصدة؛ كل تغيير له حركة وسبب</p></div></div><div className="flex rounded-xl bg-slate-100 p-1 text-xs font-bold">{([['balances','الأرصدة'],['count','الجرد'],['waste','الهالك']] as const).map(([id,label])=><button key={id} onClick={()=>setTab(id)} className={`rounded-lg px-4 py-2 ${tab===id?'bg-white shadow':''}`}>{label}</button>)}</div></div>
    {tab==='balances'&&<><div className="flex gap-2 rounded-2xl border bg-white p-3"><input value={name} onChange={e=>setName(e.target.value)} placeholder="اسم خامة جديدة" className="flex-1 rounded-xl border px-3 text-sm"/><button onClick={addItem} className="flex items-center gap-2 rounded-xl bg-[#0F52BA] px-4 py-3 text-xs font-black text-white"><Plus className="h-4 w-4"/>إضافة</button></div><div className="overflow-hidden rounded-3xl border bg-white"><div className="grid grid-cols-5 bg-slate-50 p-4 text-xs font-black"><span>الخامة</span><span>التصنيف</span><span>الوحدة</span><span>الرصيد</span><span>الحالة</span></div>{s.inventoryItems.map(x=>{const balance=s.stockBalance(x.id),low=balance<=x.minimumMilliUnits;return <div key={x.id} className="grid grid-cols-5 border-t p-4 text-xs"><b>{x.name}</b><span>{x.category}</span><span>{x.unit}</span><b>{qty(balance)}</b><span className={low?'font-bold text-red-700':'font-bold text-emerald-700'}>{low?'تحت الحد الأدنى':'متاح'}</span></div>})}</div><div className="rounded-3xl border bg-white"><h2 className="border-b p-4 font-black">آخر حركات المخزون</h2>{s.stockMovements.slice(0,20).map(m=><div key={m.id} className="grid grid-cols-5 border-b p-3 text-xs"><b>{s.inventoryItems.find(x=>x.id===m.inventoryItemId)?.name}</b><span>{m.type}</span><span className={m.direction==='in'?'text-emerald-700':'text-red-700'}>{m.direction==='in'?'+':'-'}{qty(m.quantityMilliUnits)}</span><span>{m.reason}</span><span>{new Date(m.createdAt).toLocaleString('ar-EG')}</span></div>)}</div></>}
    {tab==='count'&&<div className="mx-auto max-w-2xl rounded-3xl border bg-white p-6"><h2 className="mb-4 flex items-center gap-2 font-black"><ClipboardCheck/>جرد فعلي واعتماد الفروق</h2><select value={itemId} onChange={e=>setItemId(e.target.value)} className="mb-3 w-full rounded-xl border p-3">{s.inventoryItems.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select><div className="grid grid-cols-3 gap-3 text-center"><div className="rounded-xl bg-slate-50 p-4"><small>المتوقع</small><b className="block text-xl">{qty(expected)}</b></div><div className="rounded-xl bg-slate-50 p-3"><small>الفعلي</small><input type="number" value={actual} onChange={e=>setActual(e.target.value)} className="mt-1 w-full bg-transparent text-center text-xl font-black outline-none"/></div><div className={`rounded-xl p-4 ${variance===0?'bg-emerald-50':variance<0?'bg-red-50':'bg-amber-50'}`}><small>الفرق</small><b className="block text-xl">{qty(variance)}</b></div></div><textarea value={reason} onChange={e=>setReason(e.target.value)} placeholder="سبب فرق الجرد (إلزامي)" className="mt-3 w-full rounded-xl border p-3 text-sm"/><button onClick={approveCount} className="mt-3 w-full rounded-xl bg-slate-900 p-3 text-sm font-black text-white">اعتماد قيد التسوية</button></div>}
    {tab==='waste'&&<div className="mx-auto max-w-xl rounded-3xl border bg-white p-6"><h2 className="mb-4 flex items-center gap-2 font-black text-red-800"><Trash2/>تسجيل هالك أو تلف</h2><select value={itemId} onChange={e=>setItemId(e.target.value)} className="mb-3 w-full rounded-xl border p-3">{s.inventoryItems.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select><input type="number" value={waste} onChange={e=>setWaste(e.target.value)} placeholder="الكمية التالفة" className="mb-3 w-full rounded-xl border p-3"/><textarea value={reason} onChange={e=>setReason(e.target.value)} placeholder="محروق، منتهي، سقط، خطأ تحضير..." className="w-full rounded-xl border p-3"/><div className="mt-3 flex items-center gap-2 rounded-xl bg-amber-50 p-3 text-xs text-amber-900"><AlertTriangle className="h-4 w-4"/>الهالك الكبير يتطلب اعتماد المدير ويظهر في مركز الرقابة.</div><button onClick={recordWaste} className="mt-3 w-full rounded-xl bg-red-600 p-3 text-sm font-black text-white">تسجيل الهالك وخفض المخزون</button></div>}
  </div>;
}
