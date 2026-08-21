'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Settings as SettingsIcon, Store, Printer, Shield, KeyRound, Save, CheckCircle2 } from 'lucide-react';

export default function SettingsScreen() {
  const { settings, updateSettings } = useAppStore();
  const [formData, setFormData] = useState({ ...settings });
  const [saved, setSaved] = useState(false);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    updateSettings(formData);
    setSaved(true);
    setTimeout(() => setSaved(false), 3000);
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-slate-100 text-slate-700 border border-slate-200 flex items-center justify-center">
            <SettingsIcon className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">إعدادات النظام والمطعم</h1>
            <p className="text-xs text-slate-500">
              تخصيص بيانات الفاتورة الحرارية 80mm، أرقام التواصل، ورموز الأمان والتفويض
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={handleSave}
          className="px-5 py-2.5 bg-red-600 hover:bg-red-700 active:bg-red-800 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-1.5"
        >
          <Save className="w-4 h-4" />
          <span>حفظ التغييرات</span>
        </button>
      </div>

      {saved && (
        <div className="p-3 bg-emerald-50 text-emerald-800 border border-emerald-200 rounded-2xl text-xs font-bold flex items-center gap-2 animate-in fade-in">
          <CheckCircle2 className="w-4 h-4 text-emerald-600" />
          <span>تم حفظ الإعدادات بنجاح وتحديث كافة الشاشات وطابعات البونات!</span>
        </div>
      )}

      {/* 2. Settings Form */}
      <form onSubmit={handleSave} className="space-y-6">
        {/* Restaurant Identity */}
        <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs space-y-4">
          <h3 className="text-sm font-extrabold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-2">
            <Store className="w-4 h-4 text-red-600" />
            <span>بيانات وهوية المطعم</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">اسم المطعم التجاري:</label>
              <input
                type="text"
                value={formData.restaurantName}
                onChange={(e) => setFormData({ ...formData, restaurantName: e.target.value })}
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                required
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">الاسم الفرعي / الشعار:</label>
              <input
                type="text"
                value={formData.subName}
                onChange={(e) => setFormData({ ...formData, subName: e.target.value })}
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">عنوان الفرع:</label>
              <input
                type="text"
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">أرقام هواتف الدليفري والاستفسارات:</label>
              <input
                type="text"
                value={formData.phone1}
                onChange={(e) => setFormData({ ...formData, phone1: e.target.value })}
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>
          </div>
        </div>

        {/* Thermal Receipt Settings */}
        <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs space-y-4">
          <h3 className="text-sm font-extrabold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-2">
            <Printer className="w-4 h-4 text-emerald-600" />
            <span>إعدادات الفاتورة الحرارية 80mm</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">تذييل الفاتورة:</label>
              <input
                type="text"
                value={formData.receiptFooter}
                onChange={(e) => setFormData({ ...formData, receiptFooter: e.target.value })}
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">هاتف الشكاوى والمقترحات:</label>
              <input
                type="text"
                value={formData.complaintsPhone}
                onChange={(e) => setFormData({ ...formData, complaintsPhone: e.target.value })}
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>
          </div>

          <div className="space-y-3 pt-2">
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="autoPrintCheck"
                checked={formData.autoPrintReceipt}
                onChange={(e) => setFormData({ ...formData, autoPrintReceipt: e.target.checked })}
                className="w-4 h-4 rounded-md accent-red-600"
              />
              <label htmlFor="autoPrintCheck" className="text-xs font-bold text-slate-800 cursor-pointer">
                طباعة إيصال العميل تلقائياً فور إنهاء البيع والدفع
              </label>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="autoGrillCheck"
                checked={formData.autoPrintPreparationTicket}
                onChange={(e) => setFormData({ ...formData, autoPrintPreparationTicket: e.target.checked })}
                className="w-4 h-4 rounded-md accent-amber-600"
              />
              <label htmlFor="autoGrillCheck" className="text-xs font-bold text-slate-800 cursor-pointer">
                إرسال بونات أصناف الفحم تلقائياً لشاشة المطبخ (KDS)
              </label>
            </div>
          </div>
        </div>

        {/* Security & System Rules */}
        <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs space-y-4">
          <h3 className="text-sm font-extrabold text-slate-900 flex items-center gap-2 border-b border-slate-100 pb-2">
            <Shield className="w-4 h-4 text-red-600" />
            <span>الأمان وضوابط الخزنة</span>
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">حد الفرق النقدي لطلب اعتماد المدير (ج):</label>
              <input
                type="number"
                value={formData.cashDiscrepancyThreshold}
                onChange={(e) => setFormData({ ...formData, cashDiscrepancyThreshold: Number(e.target.value) })}
                className="w-full text-sm font-mono font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
              <span className="text-[10px] text-slate-400 mt-1 block">
                أي فرق عجز أو زيادة يتجاوز هذا الحد يستوجب اعتماد PIN المدير لإغلاق الوردية
              </span>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">أجر ساعة الإضافي الافتراضي (ج):</label>
              <input
                type="number"
                value={formData.overtimeHourlyRate}
                onChange={(e) => setFormData({ ...formData, overtimeHourlyRate: Number(e.target.value) })}
                className="w-full text-sm font-mono font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">رسوم التوصيل الافتراضية (ج):</label>
              <input
                type="number"
                min="0"
                value={formData.deliveryFee}
                onChange={(e) => setFormData({ ...formData, deliveryFee: Number(e.target.value) })}
                className="w-full text-sm font-mono font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
              />
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
