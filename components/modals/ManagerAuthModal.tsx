'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { ShieldAlert, Check, X, KeyRound, AlertTriangle } from 'lucide-react';

export default function ManagerAuthModal() {
  const { managerAuthOpen, managerAuthParams, closeManagerAuth, verifyManagerPin } = useAppStore();
  const [pin, setPin] = useState('');
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');

  if (!managerAuthOpen || !managerAuthParams) return null;

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

  const handleSubmit = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (managerAuthParams.requireReason && !reason.trim()) {
      setError('الرجاء إدخال سبب العملية للمتابعة والتدقيق.');
      return;
    }

    const { valid, managerName, approval, lockedUntil } = verifyManagerPin(pin, managerAuthParams.action, reason);
    if (!valid || !managerName || !approval) {
      if (lockedUntil) { setError('تم إيقاف المحاولات مؤقتًا لمدة دقيقة بسبب تكرار PIN غير صحيح.'); return; }
      setError('رمز PIN غير صحيح أو ليس لديك صلاحية مدير.');
      return;
    }

    try {
      managerAuthParams.onSuccess(managerName, reason, approval);
      setPin('');
      setReason('');
      setError('');
      closeManagerAuth();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'تعذر تنفيذ العملية المالية.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 text-slate-800">
        {/* Header */}
        <div className="bg-red-600 px-6 py-4 text-white flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-red-700/60 rounded-lg">
              <ShieldAlert className="w-6 h-6 text-yellow-300" />
            </div>
            <div>
              <h3 className="font-bold text-lg leading-tight">موافقة وتفويض المدير</h3>
              <p className="text-xs text-red-100 mt-0.5">عملية حساسة تتطلب التحقق وتوثيق السبب</p>
            </div>
          </div>
          <button
            onClick={() => {
              setPin('');
              setReason('');
              closeManagerAuth();
            }}
            className="p-1 text-white/80 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-5">
          {/* Operation details */}
          <div className="bg-amber-50 border border-amber-200 rounded-xl p-3.5 flex items-start gap-3 text-amber-900">
            <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
            <div className="text-sm">
              <span className="font-bold block mb-1">{managerAuthParams.title}</span>
              <span className="text-amber-800 text-xs leading-relaxed">{managerAuthParams.description}</span>
            </div>
          </div>

          {/* Reason input */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              سبب العملية {managerAuthParams.requireReason && <span className="text-red-500">* (مطلوب)</span>}
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="اكتب سبب إجراء هذه العملية للتدقيق في سجل العمليات..."
              rows={2}
              className="w-full text-sm rounded-xl border border-slate-300 px-3 py-2 text-slate-800 focus:outline-hidden focus:ring-2 focus:ring-red-500 focus:border-red-500"
            />
          </div>

          {/* PIN Input & Display */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5 flex items-center justify-between">
              <span>رمز PIN للمدير / المالك:</span>
              <span className="text-[11px] text-slate-400 font-normal">(أمثلة تجريبية: مالك 1234 / مدير 5555)</span>
            </label>
            <div className="flex items-center justify-center gap-3 bg-slate-50 border border-slate-300 rounded-xl py-3 px-4">
              <KeyRound className="w-5 h-5 text-slate-400" />
              <div className="flex gap-2.5">
                {[0, 1, 2, 3].map((idx) => (
                  <div
                    key={idx}
                    className={`w-4 h-4 rounded-full border-2 transition-all ${
                      pin.length > idx ? 'bg-red-600 border-red-600 scale-110' : 'border-slate-300 bg-white'
                    }`}
                  />
                ))}
              </div>
            </div>
            {error && <p className="text-xs text-red-600 font-medium mt-1.5 text-center">{error}</p>}
          </div>

          {/* Touch Keypad */}
          <div className="grid grid-cols-3 gap-2 select-none">
            {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map((num) => (
              <button
                key={num}
                type="button"
                onClick={() => handleKeyClick(num)}
                className="h-12 text-lg font-bold bg-slate-100 hover:bg-slate-200 active:bg-slate-300 rounded-xl text-slate-800 transition-colors border border-slate-200"
              >
                {num}
              </button>
            ))}
            <button
              type="button"
              onClick={handleClear}
              className="h-12 text-sm font-semibold bg-slate-100 hover:bg-slate-200 active:bg-slate-300 rounded-xl text-slate-600 transition-colors border border-slate-200"
            >
              مسح
            </button>
            <button
              type="button"
              onClick={() => handleKeyClick('0')}
              className="h-12 text-lg font-bold bg-slate-100 hover:bg-slate-200 active:bg-slate-300 rounded-xl text-slate-800 transition-colors border border-slate-200"
            >
              0
            </button>
            <button
              type="button"
              onClick={handleBackspace}
              className="h-12 text-sm font-semibold bg-slate-100 hover:bg-slate-200 active:bg-slate-300 rounded-xl text-slate-600 transition-colors border border-slate-200"
            >
              ⌫
            </button>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => {
                setPin('');
                setReason('');
                closeManagerAuth();
              }}
              className="flex-1 py-3 text-sm font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition-colors"
            >
              إلغاء
            </button>
            <button
              type="button"
              onClick={() => handleSubmit()}
              disabled={pin.length < 4}
              className="flex-1 py-3 text-sm font-bold text-white bg-red-600 hover:bg-red-700 active:bg-red-800 disabled:opacity-50 disabled:pointer-events-none rounded-xl shadow-md transition-colors flex items-center justify-center gap-2"
            >
              <Check className="w-4 h-4" />
              تأكيد التفويض
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
