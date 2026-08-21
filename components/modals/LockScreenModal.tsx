'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Lock, Unlock, KeyRound, Shield, UserCheck, Flame } from 'lucide-react';

export default function LockScreenModal() {
  const { isLocked, unlockScreen, currentUser, settings } = useAppStore();
  const [pin, setPin] = useState('');
  const [error, setError] = useState('');

  if (!isLocked) return null;

  const handleKeyClick = (num: string) => {
    if (pin.length < 6) {
      setPin((prev) => prev + num);
      setError('');
    }
  };

  const handleBackspace = () => {
    setPin((prev) => prev.slice(0, -1));
    setError('');
  };

  const handleClear = () => {
    setPin('');
    setError('');
  };

  const handleUnlock = (targetPin?: string) => {
    const pinToTest = targetPin || pin;
    const success = unlockScreen(pinToTest);
    if (!success) {
      setError('رمز PIN غير صحيح. يرجى المحاولة مرة أخرى.');
    } else {
      setPin('');
      setError('');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/90 backdrop-blur-md p-4 animate-in fade-in duration-300">
      <div className="bg-slate-900 text-white rounded-3xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-800 p-8 space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="w-16 h-16 bg-red-600/20 text-red-500 rounded-2xl flex items-center justify-center mx-auto border border-red-500/30">
            <Lock className="w-8 h-8" />
          </div>
          <h2 className="text-2xl font-black text-white">{settings.restaurantName}</h2>
          <p className="text-xs text-slate-400">شاشة النظام مقفلة لحماية العمليات المالية</p>
        </div>

        {/* Current user badge */}
        <div className="bg-slate-800/80 border border-slate-700/80 rounded-2xl p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-slate-700 flex items-center justify-center text-xl">
              {currentUser.avatar || '👤'}
            </div>
            <div>
              <div className="font-bold text-sm text-slate-200">{currentUser.name}</div>
              <div className="text-xs text-red-400 font-medium">
                {currentUser.role === 'owner'
                  ? 'مالك النظام'
                  : currentUser.role === 'manager'
                  ? 'مدير الصالة والورديات'
                  : currentUser.role === 'cashier'
                  ? 'كاشير'
                  : currentUser.role === 'grill'
                  ? 'عامل محطة الفحم'
                  : 'مشرف'}
              </div>
            </div>
          </div>
          <span className="text-xs px-2.5 py-1 bg-amber-500/20 text-amber-300 border border-amber-500/30 rounded-lg">
            مغلق مؤقتاً
          </span>
        </div>

        {/* PIN Indicators */}
        <div className="space-y-3">
          <div className="flex justify-center gap-3 py-2">
            {[0, 1, 2, 3].map((idx) => (
              <div
                key={idx}
                className={`w-4 h-4 rounded-full border-2 transition-all duration-200 ${
                  pin.length > idx
                    ? 'bg-red-500 border-red-500 scale-125 shadow-xs shadow-red-500/50'
                    : 'border-slate-600 bg-slate-800'
                }`}
              />
            ))}
          </div>
          {error && <p className="text-xs text-red-400 font-semibold text-center">{error}</p>}
        </div>

        {/* Touch Keypad */}
        <div className="grid grid-cols-3 gap-2.5 select-none max-w-xs mx-auto">
          {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map((num) => (
            <button
              key={num}
              type="button"
              onClick={() => handleKeyClick(num)}
              className="h-14 text-xl font-bold bg-slate-800 hover:bg-slate-700 active:bg-red-600 text-slate-100 rounded-2xl transition-colors border border-slate-700/60 shadow-xs"
            >
              {num}
            </button>
          ))}
          <button
            type="button"
            onClick={handleClear}
            className="h-14 text-sm font-bold bg-slate-800/60 hover:bg-slate-700 active:bg-slate-600 text-slate-400 rounded-2xl transition-colors border border-slate-700/40"
          >
            مسح
          </button>
          <button
            key="0"
            type="button"
            onClick={() => handleKeyClick('0')}
            className="h-14 text-xl font-bold bg-slate-800 hover:bg-slate-700 active:bg-red-600 text-slate-100 rounded-2xl transition-colors border border-slate-700/60"
          >
            0
          </button>
          <button
            type="button"
            onClick={handleBackspace}
            className="h-14 text-lg font-bold bg-slate-800/60 hover:bg-slate-700 active:bg-slate-600 text-slate-300 rounded-2xl transition-colors border border-slate-700/40"
          >
            ⌫
          </button>
        </div>

        {/* Submit Button */}
        <div className="max-w-xs mx-auto">
          <button
            type="button"
            onClick={() => handleUnlock()}
            disabled={pin.length < 4}
            className="w-full py-3.5 bg-red-600 hover:bg-red-500 active:bg-red-700 disabled:opacity-40 disabled:pointer-events-none rounded-2xl font-bold text-white shadow-lg transition-all flex items-center justify-center gap-2"
          >
            <Unlock className="w-5 h-5" />
            فتح الشاشة
          </button>
        </div>

        <p className="pt-2 border-t border-slate-800 text-center text-[11px] text-slate-500">
          أدخل رمزك الشخصي؛ سيحدد النظام المستخدم وصلاحياته تلقائيًا.
        </p>
      </div>
    </div>
  );
}
