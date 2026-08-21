'use client';
import React, { useEffect } from 'react';
import { AlertTriangle, Download } from 'lucide-react';
import { usePersistenceIssue } from '@/lib/persistence-health';
import { reportPersistenceIssue } from '@/lib/persistence-health';
import { ensureBrowserWriter, refreshBrowserWriter, releaseBrowserWriter } from '@/lib/browser-writer.mjs';
export default function PersistenceGuard({children}:{children:React.ReactNode}) {
  const issue=usePersistenceIssue();
  useEffect(()=>{ try{ensureBrowserWriter(localStorage);}catch{reportPersistenceIssue({code:'READ_ONLY',message:'النظام مفتوح بالفعل على نافذة أخرى'});return;} const timer=window.setInterval(()=>{try{refreshBrowserWriter(localStorage);}catch{reportPersistenceIssue({code:'READ_ONLY',message:'النظام مفتوح بالفعل على نافذة أخرى'});}},5000); const onStorage=(event:StorageEvent)=>{if(event.key==='falah-single-writer-lock-v1'){try{refreshBrowserWriter(localStorage);}catch{reportPersistenceIssue({code:'READ_ONLY',message:'النظام مفتوح بالفعل على نافذة أخرى'});}}}; window.addEventListener('storage',onStorage); return()=>{clearInterval(timer);window.removeEventListener('storage',onStorage);releaseBrowserWriter(localStorage);};},[]);
  if(!issue) return children;
  const exportRaw=()=>{ const data:Record<string,string|null>={}; for(let i=0;i<localStorage.length;i++){const key=localStorage.key(i);if(key)data[key]=localStorage.getItem(key);} const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:'application/json'}));a.download=`FALAH_RECOVERY_${Date.now()}.json`;a.click();URL.revokeObjectURL(a.href); };
  return <main dir="rtl" className="min-h-screen bg-slate-100 flex items-center justify-center p-6"><section className="max-w-xl bg-white border border-red-200 rounded-3xl p-8 shadow-sm text-center space-y-4"><AlertTriangle className="w-12 h-12 text-red-600 mx-auto"/><h1 className="text-xl font-black text-slate-900">تم إيقاف العمليات لحماية البيانات</h1><p className="text-sm text-slate-600">{issue.message}</p><p className="text-xs text-slate-500">لا تُدخل عمليات مالية جديدة قبل استعادة نسخة سليمة أو مراجعة مسؤول النظام.</p><button onClick={exportRaw} className="mx-auto px-4 py-3 rounded-xl bg-slate-900 text-white text-sm font-bold flex items-center gap-2"><Download className="w-4 h-4"/>تصدير بيانات الاستعادة الخام</button></section></main>;
}
