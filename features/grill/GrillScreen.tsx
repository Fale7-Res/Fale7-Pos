'use client';

import React, { useState, useEffect } from 'react';
import { useAppStore } from '@/lib/store';
import { Flame, Clock, CheckCircle2, Play, AlertCircle, Utensils, Volume2, VolumeX, RefreshCw } from 'lucide-react';

export default function GrillScreen() {
  const { grillTickets, updateGrillTicketStatus, settings, updateSettings } = useAppStore();
  const [filterStatus, setFilterStatus] = useState<'all' | 'pending' | 'preparing' | 'ready'>('all');
  const [nowTime, setNowTime] = useState<number>(() => (typeof Date !== 'undefined' ? Date.now() : 0));

  useEffect(() => {
    const timer = setInterval(() => {
      setNowTime(Date.now());
    }, 10000); // update every 10s
    return () => clearInterval(timer);
  }, []);

  const filteredTickets = grillTickets.filter((ticket) => {
    if (filterStatus === 'all') return true;
    return ticket.status === filterStatus;
  });

  // Calculate elapsed minutes from creation time
  const getElapsedMinutes = (createdAt: string) => {
    try {
      const parts = createdAt.split(' ');
      if (parts.length < 2) return 5;
      const [hours, mins] = parts[1].split(':').map(Number);
      const now = new Date();
      const created = new Date();
      created.setHours(hours, mins, 0, 0);
      const diffMs = now.getTime() - created.getTime();
      const diffMins = Math.max(0, Math.floor(diffMs / 60000));
      return diffMins;
    } catch {
      return 5;
    }
  };

  const pendingCount = grillTickets.filter((t) => t.status === 'pending').length;
  const preparingCount = grillTickets.filter((t) => t.status === 'preparing').length;
  const readyCount = grillTickets.filter((t) => t.status === 'ready').length;

  return (
    <div className="h-full flex flex-col bg-slate-950 text-white font-sans overflow-hidden select-none" dir="rtl">
      {/* 1. Grill Station Top Bar */}
      <div className="bg-slate-900 border-b border-slate-800 px-6 py-3.5 flex items-center justify-between shrink-0 shadow-md">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-amber-500/20 border border-amber-500/30 flex items-center justify-center text-amber-400">
            <Flame className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-black tracking-wide text-white">شاشة الفحم والتحضير (Grill Station)</h2>
              <span className="text-xs bg-amber-500/20 text-amber-300 font-bold px-2 py-0.5 rounded-full border border-amber-500/30">
                مباشر
              </span>
            </div>
            <p className="text-xs text-slate-400">محطة تجهيز أصناف المشويات والشواية للمطبخ</p>
          </div>
        </div>

        {/* Status Filter Tabs */}
        <div className="flex items-center gap-2 bg-slate-950 p-1.5 rounded-2xl border border-slate-800">
          <button
            type="button"
            onClick={() => setFilterStatus('all')}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
              filterStatus === 'all' ? 'bg-slate-800 text-white shadow-xs' : 'text-slate-400 hover:text-white'
            }`}
          >
            الكل ({grillTickets.length})
          </button>
          <button
            type="button"
            onClick={() => setFilterStatus('pending')}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${
              filterStatus === 'pending'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'text-amber-400 hover:bg-slate-900'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-amber-400"></span>
            <span>جديد ({pendingCount})</span>
          </button>
          <button
            type="button"
            onClick={() => setFilterStatus('preparing')}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${
              filterStatus === 'preparing'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'text-blue-400 hover:bg-slate-900'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-blue-400 animate-pulse"></span>
            <span>قيد الشوي ({preparingCount})</span>
          </button>
          <button
            type="button"
            onClick={() => setFilterStatus('ready')}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${
              filterStatus === 'ready'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'text-emerald-400 hover:bg-slate-900'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
            <span>جاهز ({readyCount})</span>
          </button>
        </div>

        {/* Audio Alert Toggle */}
        <button
          type="button"
          onClick={() =>
            updateSettings({ grillScreenSoundAlert: !settings.grillScreenSoundAlert })
          }
          className={`p-2.5 rounded-xl border transition-colors flex items-center gap-2 text-xs font-bold ${
            settings.grillScreenSoundAlert
              ? 'bg-amber-500/20 border-amber-500/40 text-amber-300'
              : 'bg-slate-800 border-slate-700 text-slate-400'
          }`}
          title="تنبيه الصوت عند وصول طلب جديد"
        >
          {settings.grillScreenSoundAlert ? <Volume2 className="w-4 h-4" /> : <VolumeX className="w-4 h-4" />}
          <span>{settings.grillScreenSoundAlert ? 'التنبيه الصوتي مفعل' : 'صامت'}</span>
        </button>
      </div>

      {/* 2. Grill Ticket Grid */}
      <div className="flex-1 overflow-y-auto p-6">
        {filteredTickets.length === 0 ? (
          <div className="h-full flex flex-col items-center justify-center text-slate-500">
            <div className="w-20 h-20 rounded-3xl bg-slate-900 border border-slate-800 flex items-center justify-center mb-4 text-slate-600">
              <Utensils className="w-10 h-10" />
            </div>
            <h3 className="text-xl font-bold text-slate-300">لا توجد طلبات فحم في هذا القسم حالياً</h3>
            <p className="text-sm text-slate-500 mt-1">تصل طلبات المشويات والشواية تلقائياً فور تأكيد الدفع في الكاشير</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
            {filteredTickets.map((ticket) => {
              const elapsedMins = getElapsedMinutes(ticket.createdAt);
              const isUrgent = elapsedMins >= 15;
              const isWarning = elapsedMins >= 8 && elapsedMins < 15;

              return (
                <div
                  key={ticket.id}
                  className={`bg-slate-900 rounded-3xl overflow-hidden border-2 flex flex-col justify-between transition-all shadow-xl ${
                    ticket.status === 'ready'
                      ? 'border-emerald-500/50 opacity-70 hover:opacity-100'
                      : ticket.status === 'preparing'
                      ? 'border-blue-500 shadow-blue-500/10'
                      : isUrgent
                      ? 'border-red-500 animate-pulse shadow-red-500/20'
                      : isWarning
                      ? 'border-amber-500 shadow-amber-500/10'
                      : 'border-slate-800'
                  }`}
                >
                  {/* Card Header */}
                  <div
                    className={`p-4 flex items-center justify-between border-b ${
                      ticket.status === 'ready'
                        ? 'bg-emerald-950/60 border-emerald-900/60'
                        : ticket.status === 'preparing'
                        ? 'bg-blue-950/60 border-blue-900/60'
                        : isUrgent
                        ? 'bg-red-950/60 border-red-900/60'
                        : 'bg-slate-800/80 border-slate-700/80'
                    }`}
                  >
                    {/* HUGE Order Number */}
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 block">رقم الطلب</span>
                      <span className="text-3xl font-black font-mono tracking-wider text-white">
                        {ticket.orderNumber}
                      </span>
                    </div>

                    {/* Waiting Timer Badge */}
                    <div className="text-left space-y-1">
                      <span
                        className={`text-xs font-black px-2.5 py-1 rounded-xl flex items-center gap-1.5 ${
                          ticket.status === 'ready'
                            ? 'bg-emerald-500/20 text-emerald-300'
                            : isUrgent
                            ? 'bg-red-600 text-white font-mono font-black animate-bounce'
                            : isWarning
                            ? 'bg-amber-500/20 text-amber-300'
                            : 'bg-slate-700/60 text-slate-300'
                        }`}
                      >
                        <Clock className="w-3.5 h-3.5" />
                        <span>{elapsedMins} دقيقة</span>
                      </span>
                      <span className="text-[10px] text-slate-400 block font-semibold text-left">
                        {ticket.orderType === 'dine_in'
                          ? `صالة ${ticket.notes ? `(${ticket.notes})` : ''}`
                          : ticket.orderType === 'takeaway'
                          ? 'تيك أواي'
                          : 'دليفري'}
                      </span>
                    </div>
                  </div>

                  {/* Card Items List */}
                  <div className="p-4 space-y-3 flex-1 overflow-y-auto max-h-64">
                    {ticket.items.map((item, idx) => (
                      <div
                        key={idx}
                        className="bg-slate-950/70 p-3 rounded-2xl border border-slate-800 flex items-start justify-between gap-2"
                      >
                        <div>
                          <div className="text-base font-black text-white">{item.productName}</div>
                          {item.variantName && (
                            <div className="text-xs font-bold text-amber-400 mt-0.5">
                              نوع الخبز/الحجم: {item.variantName}
                            </div>
                          )}
                          {item.notes && (
                            <div className="text-xs font-bold text-red-400 bg-red-950/60 border border-red-900/60 px-2 py-0.5 rounded-lg mt-1">
                              ⚠️ {item.notes}
                            </div>
                          )}
                        </div>

                        {/* Large Quantity Badge */}
                        <div className="w-9 h-9 rounded-xl bg-amber-500 text-slate-950 font-black text-lg flex items-center justify-center shrink-0 shadow-md">
                          ×{item.quantity}
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Card Footer Touch Actions */}
                  <div className="p-3 bg-slate-950 border-t border-slate-800 flex gap-2">
                    {ticket.status === 'pending' && (
                      <button
                        type="button"
                        onClick={() => updateGrillTicketStatus(ticket.id, 'preparing')}
                        className="w-full py-3.5 bg-blue-600 hover:bg-blue-500 active:bg-blue-700 text-white font-black text-sm rounded-2xl shadow-lg transition-all flex items-center justify-center gap-2"
                      >
                        <Play className="w-4 h-4" />
                        <span>ابدأ التحضير والشوي</span>
                      </button>
                    )}

                    {ticket.status === 'preparing' && (
                      <button
                        type="button"
                        onClick={() => updateGrillTicketStatus(ticket.id, 'ready')}
                        className="w-full py-3.5 bg-emerald-600 hover:bg-emerald-500 active:bg-emerald-700 text-white font-black text-sm rounded-2xl shadow-lg transition-all flex items-center justify-center gap-2"
                      >
                        <CheckCircle2 className="w-5 h-5" />
                        <span>جاهز للتقديم والتسليم ✓</span>
                      </button>
                    )}

                    {ticket.status === 'ready' && (
                      <div className="w-full py-2.5 bg-emerald-950/80 border border-emerald-800 text-emerald-300 font-bold text-xs rounded-xl flex items-center justify-center gap-1.5">
                        <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                        <span>تم تسليم الطلب للعميل ({ticket.readyAt || 'جاهز'})</span>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
