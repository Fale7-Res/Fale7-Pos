'use client';
import { useEffect } from 'react';

export default function GlobalError({error,reset}:{error:Error & {digest?:string};reset:()=>void}) {
  useEffect(()=>{ console.error('Falah application route error', {message:error.message,digest:error.digest,stack:error.stack}); },[error]);
  return <main dir="rtl" className="flex min-h-screen items-center justify-center bg-slate-100 p-6"><section className="max-w-lg space-y-4 rounded-3xl border border-red-200 bg-white p-8 text-center"><h1 className="text-xl font-black">تعذر عرض هذه الشاشة بأمان</h1><p className="text-sm text-slate-600">تم إيقاف الشاشة لحماية بيانات التشغيل. لم يتم حذف أو استبدال أي بيانات محفوظة.</p><div className="flex justify-center gap-2"><button onClick={reset} className="rounded-xl bg-slate-900 px-4 py-2 text-white">إعادة المحاولة</button><button onClick={()=>location.reload()} className="rounded-xl border px-4 py-2">إعادة تحميل النظام</button></div></section></main>;
}
