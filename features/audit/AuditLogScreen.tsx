'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { History, Search, Shield, Filter, Download, Printer } from 'lucide-react';

export default function AuditLogScreen() {
  const { auditEvents } = useAppStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');

  const filtered = auditEvents.filter((event) => {
    const matchesSearch =
      event.action.toLowerCase().includes(searchQuery.toLowerCase()) ||
      event.details.toLowerCase().includes(searchQuery.toLowerCase()) ||
      event.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (event.reason && event.reason.toLowerCase().includes(searchQuery.toLowerCase()));

    const matchesCat = categoryFilter === 'all' || event.category === categoryFilter;

    return matchesSearch && matchesCat;
  });

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-slate-100 text-slate-700 border border-slate-200 flex items-center justify-center">
            <History className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">سجل العمليات والتدقيق الشامل (Audit Trail)</h1>
            <p className="text-xs text-slate-500">
              سجل غير قابل للتعديل يوثق كافة التحركات، الحركات المالية، الإلغاءات، وتفويضات المديرين
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="ابحث في السجل..."
              className="bg-slate-100 text-xs pr-9 pl-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden focus:border-red-500"
            />
          </div>

          <select
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            className="bg-slate-100 text-xs px-3 py-2 rounded-xl border border-slate-200 font-bold text-slate-700"
          >
            <option value="all">كل الأقسام</option>
            <option value="pos">نقطة البيع</option>
            <option value="shift">الورديات والخزنة</option>
            <option value="employee">الموظفين والرواتب</option>
            <option value="attendance">الحضور والانصراف</option>
            <option value="auth">التفويض والمدير</option>
            <option value="menu">إدارة المنيو</option>
          </select>
        </div>
      </div>

      {/* 2. Audit Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">التاريخ والوقت</th>
                <th className="py-3.5 px-4">القسم</th>
                <th className="py-3.5 px-4">العملية / الحدث</th>
                <th className="py-3.5 px-4">التفاصيل</th>
                <th className="py-3.5 px-4">المستخدم المنفذ</th>
                <th className="py-3.5 px-4">السبب الموثق</th>
                <th className="py-3.5 px-4">اعتماد المدير</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filtered.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50 transition-colors">
                  <td className="py-3.5 px-4 font-mono text-[11px] text-slate-500">{item.timestamp}</td>
                  <td className="py-3.5 px-4">
                    <span className="inline-block px-2 py-0.5 bg-slate-100 text-slate-700 rounded-md font-bold text-[10px]">
                      {item.category}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 font-black text-slate-900">{item.action}</td>
                  <td className="py-3.5 px-4 text-slate-700 max-w-sm">{item.details}</td>
                  <td className="py-3.5 px-4 font-semibold text-slate-800">{item.userName}</td>
                  <td className="py-3.5 px-4 text-slate-500">{item.reason || '-'}</td>
                  <td className="py-3.5 px-4">
                    {item.authorizedBy ? (
                      <span className="text-emerald-700 font-bold bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200 text-[11px]">
                        {item.authorizedBy} ✓
                      </span>
                    ) : (
                      <span className="text-slate-400">-</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
