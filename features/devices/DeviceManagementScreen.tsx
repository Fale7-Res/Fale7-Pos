'use client';

import React from 'react';
import { useAppStore } from '@/lib/store';
import { HardDrive, Printer, Fingerprint, Flame, CheckCircle2, RotateCcw, Activity } from 'lucide-react';

export default function DeviceManagementScreen() {
  const { devices, updateDeviceStatus } = useAppStore();

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-cyan-500/10 text-cyan-600 border border-cyan-500/20 flex items-center justify-center">
            <HardDrive className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">إدارة الأجهزة والعتاد المتصل (Hardware)</h1>
            <p className="text-xs text-slate-500">
              متابعة طابعة البونات 80mm، شاشة الفحم (KDS)، جهاز البصمة البيومترية، ودرج النقدية
            </p>
          </div>
        </div>
      </div>

      {/* 2. Devices Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {devices.map((dev) => (
          <div
            key={dev.id}
            className="bg-white rounded-3xl p-5 border border-slate-200 shadow-xs space-y-4 flex flex-col justify-between"
          >
            <div>
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-700">
                    {dev.type === 'printer' ? (
                      <Printer className="w-6 h-6 text-emerald-600" />
                    ) : dev.type === 'grill' ? (
                      <Flame className="w-6 h-6 text-amber-600" />
                    ) : (
                      <Fingerprint className="w-6 h-6 text-cyan-600" />
                    )}
                  </div>
                  <div>
                    <h3 className="font-extrabold text-base text-slate-900">{dev.name}</h3>
                    <p className="text-xs text-slate-400 font-mono">{dev.ipAddress || dev.details || 'USB Direct'}</p>
                  </div>
                </div>

                <span
                  className={`text-[10px] font-bold px-2.5 py-1 rounded-full flex items-center gap-1.5 ${
                    dev.status === 'connected' ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'
                  }`}
                >
                  <span
                    className={`w-2 h-2 rounded-full ${
                      dev.status === 'connected' ? 'bg-emerald-500 animate-pulse' : 'bg-red-500'
                    }`}
                  ></span>
                  <span>{dev.status === 'connected' ? 'متصل وجاهز' : 'غير متصل'}</span>
                </span>
              </div>

              <div className="pt-4 text-xs text-slate-600 space-y-1.5">
                <div className="flex justify-between">
                  <span className="text-slate-400">آخر إشارة نبض:</span>
                  <span className="font-mono">{dev.lastSeen}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-400">التفاصيل:</span>
                  <span className="font-semibold">{dev.details || dev.ipAddress}</span>
                </div>
              </div>
            </div>

            <div className="pt-3 border-t border-slate-100 flex gap-2">
              <button
                type="button"
                onClick={() => updateDeviceStatus(dev.id, dev.status === 'connected' ? 'disconnected' : 'connected')}
                className="w-full py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1.5"
              >
                <Activity className="w-3.5 h-3.5" />
                <span>{dev.status === 'connected' ? 'قطع الاتصال للتجربة' : 'إعادة الاتصال (Ping)'}</span>
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
