'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { useAppStore } from '@/lib/store';
import { LayoutDashboard, ShoppingCart, Receipt, Flame, Clock, Users, Wallet, Coins, ShieldAlert, History, UtensilsCrossed, UserCog, BarChart3, HardDrive, Settings, Database, Lock, Bell, ChevronDown, CalendarCheck, FileSpreadsheet, Vault, PackageCheck, Boxes, Bike } from 'lucide-react';

export default function AppShell({ children }: { children: React.ReactNode }) {
  const { activeModule, setActiveModule, currentUser, activeShift, notifications, markNotificationRead, lockScreen, grillTickets, auditEvents, settings } = useAppStore();
  const [clock, setClock] = useState({ time: '', date: '' });
  const [userOpen, setUserOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);

  useEffect(() => {
    const update = () => { const now = new Date(); setClock({ time: now.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' }), date: now.toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' }) }); };
    update(); const timer = setInterval(update, 60_000); return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (settings.autoLockMinutes <= 0) return;
    let timer: ReturnType<typeof setTimeout>;
    const resetTimer = () => {
      clearTimeout(timer);
      timer = setTimeout(lockScreen, settings.autoLockMinutes * 60_000);
    };
    const events: Array<keyof WindowEventMap> = ['pointerdown', 'keydown', 'touchstart'];
    events.forEach(event => window.addEventListener(event, resetTimer, { passive: true }));
    resetTimer();
    return () => {
      clearTimeout(timer);
      events.forEach(event => window.removeEventListener(event, resetTimer));
    };
  }, [lockScreen, settings.autoLockMinutes]);

  const grillCount = grillTickets.filter(ticket => ticket.status !== 'ready').length;
  const unreadCount = notifications.filter(notification => !notification.read).length;
  const alertCount = auditEvents.filter(event => event.severity === 'critical').length;
  const groups = useMemo(() => [
    { title: 'العمليات', items: [
      ['dashboard', 'مركز القيادة', LayoutDashboard, ['owner','manager','supervisor']], ['pos', 'نقطة البيع', ShoppingCart, ['owner','manager','cashier','supervisor']], ['orders', 'الطلبات', Receipt, ['owner','manager','cashier','supervisor']], ['grill', 'محطة الفحم', Flame, ['owner','manager','cashier','supervisor','grill'], grillCount],
      ['delivery', 'الدليفري والصالة', Bike, ['owner','manager','cashier','supervisor']],
    ]},
    { title: 'المالية', items: [
      ['treasury', 'الخزينة والعُهد', Vault, ['owner','manager']], ['cash', 'درج الكاشير', Wallet, ['owner','manager']], ['expenses', 'المصروفات', Coins, ['owner','manager','cashier','supervisor']], ['shifts', 'الشيفتات', Clock, ['owner','manager','cashier','supervisor']], ['settlement', 'التسوية', FileSpreadsheet, ['owner','manager']], ['daily_summary', 'ملخص اليوم', FileSpreadsheet, ['owner','manager','supervisor']],
      ['tax', 'الضرائب', Receipt, ['owner','manager']],
    ]},
    { title: 'الموظفون', items: [
      ['employees', 'الحسابات', Users, ['owner','manager']], ['attendance', 'الحضور', CalendarCheck, ['owner','manager','supervisor']], ['daily_wages', 'اليوميات', Wallet, ['owner','manager','supervisor']],
      ['payroll_hr', 'Payroll والإجازات', FileSpreadsheet, ['owner','manager']],
    ]},
    { title: 'المشتريات', items: [
      ['purchasing', 'الموردون والمشتريات', PackageCheck, ['owner','manager']],
      ['inventory', 'المخزون والجرد', Boxes, ['owner','manager','supervisor']],
    ]},
    { title: 'الإدارة', items: [
      ['reports', 'التقارير', BarChart3, ['owner','manager']], ['control_center', 'الرقابة', ShieldAlert, ['owner','manager'], alertCount], ['audit', 'السجل', History, ['owner','manager']], ['menu', 'المنيو', UtensilsCrossed, ['owner','manager']],
    ]},
    { title: 'النظام', items: [
      ['users', 'المستخدمون', UserCog, ['owner']], ['devices', 'الأجهزة', HardDrive, ['owner','manager']], ['backup', 'النسخ', Database, ['owner','manager']], ['settings', 'الإعدادات', Settings, ['owner','manager']],
    ]},
  ], [grillCount, alertCount]);
  const role = currentUser.role === 'owner' ? 'المالك' : currentUser.role === 'manager' ? 'المدير' : currentUser.role === 'cashier' ? 'كاشير' : currentUser.role === 'grill' ? 'شواية وفحم' : 'مشرف';

  return <div className="flex h-screen flex-col overflow-hidden bg-[#F5F5F5] text-slate-800">
    <header className="z-40 flex h-16 shrink-0 items-center justify-between border-b border-[#2C343E] bg-[#1A1F26] px-5 text-white shadow-md">
      <div className="flex items-center gap-5">
        <div className="flex items-center gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#0F52BA] text-xl font-black shadow-lg">ف</div><div><h1 className="text-base font-black">{settings.restaurantName}</h1><p className="text-[10px] text-slate-400">نظام إدارة المطعم</p></div></div>
        <div className="h-7 w-px bg-[#2C343E]" />
        <div className="flex items-center gap-2 rounded-lg bg-[#232931] px-3 py-2 text-xs"><span className={`h-2 w-2 rounded-full ${activeShift ? 'bg-emerald-400' : 'bg-slate-500'}`} /><span className="text-slate-400">الشيفت:</span><strong>{activeShift ? activeShift.shiftName : 'مغلق'}</strong></div>
      </div>
      <div className="flex items-center gap-3">
        <div className="text-left" dir="ltr"><p className="text-xs font-bold">{clock.time}</p><p className="text-[10px] text-slate-400">{clock.date}</p></div>
        <div className="relative"><button type="button" onClick={() => setNotificationsOpen(value => !value)} className="relative flex h-10 w-10 items-center justify-center rounded-lg bg-[#232931] text-slate-300 hover:bg-[#2C343E]"><Bell className="h-5 w-5" />{unreadCount > 0 && <span className="absolute left-2 top-2 h-2 w-2 rounded-full bg-red-500" />}</button>
          {notificationsOpen && <div className="absolute left-0 top-12 z-50 w-80 rounded-xl border border-slate-200 bg-white p-3 text-slate-800 shadow-2xl"><div className="mb-2 flex justify-between border-b pb-2"><strong className="text-sm">التنبيهات</strong><span className="text-xs text-slate-400">{notifications.length}</span></div><div className="max-h-72 space-y-2 overflow-y-auto">{notifications.map(notification => <button key={notification.id} type="button" onClick={() => markNotificationRead(notification.id)} className={`w-full rounded-lg border p-3 text-right ${notification.read ? 'bg-slate-50' : 'border-blue-100 bg-blue-50'}`}><span className="block text-xs font-bold">{notification.title}</span><span className="mt-1 block text-[11px] text-slate-500">{notification.message}</span></button>)}</div></div>}
        </div>
        <button type="button" onClick={lockScreen} className="flex h-10 items-center gap-2 rounded-lg bg-[#232931] px-3 text-xs font-bold text-amber-300 hover:bg-[#2C343E]"><Lock className="h-4 w-4" />قفل</button>
        <div className="relative"><button type="button" onClick={() => setUserOpen(value => !value)} className="flex items-center gap-2 rounded-lg bg-[#232931] px-2.5 py-1.5 hover:bg-[#2C343E]"><div className="flex h-7 w-7 items-center justify-center rounded-full bg-[#0F52BA] text-xs font-bold">{currentUser.name.charAt(0)}</div><div className="text-right"><p className="text-xs font-bold">{currentUser.name}</p><p className="text-[10px] text-slate-400">{role}</p></div><ChevronDown className="h-3.5 w-3.5" /></button>{userOpen && <button type="button" onClick={lockScreen} className="absolute left-0 top-12 z-50 w-52 rounded-xl border bg-white p-3 text-right text-xs font-bold text-slate-700 shadow-xl">قفل وتبديل المستخدم</button>}</div>
      </div>
    </header>
    <nav className="shrink-0 overflow-x-auto border-b border-slate-200 bg-white px-3 py-2 shadow-sm"><div className="flex min-w-max items-start gap-3">{groups.map(group => {
      const visible = group.items.filter(item => (item[3] as string[]).includes(currentUser.role)); if (!visible.length) return null;
      return <section key={group.title} className="border-l border-slate-200 pl-3 last:border-0"><p className="mb-1 px-1 text-[9px] font-bold text-slate-400">{group.title}</p><div className="flex gap-1">{visible.map(item => { const [id, label, Icon, , badge] = item; const active = activeModule === id; const NavIcon = Icon as typeof LayoutDashboard; return <button key={id as string} data-module={id as string} type="button" onClick={() => setActiveModule(id as string)} className={`flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-bold ${active ? 'bg-[#0F52BA] text-white shadow-md' : 'text-slate-600 hover:bg-slate-100'}`}><NavIcon className="h-4 w-4" />{label as string}{badge ? <span className={`rounded-full px-1.5 text-[9px] ${active ? 'bg-white text-[#0F52BA]' : 'bg-red-500 text-white'}`}>{badge as number}</span> : null}</button>; })}</div></section>;
    })}</div></nav>
    <main className={`relative flex-1 overflow-auto ${activeModule === 'pos' ? 'p-0' : 'p-5'}`}>{children}</main>
  </div>;
}
