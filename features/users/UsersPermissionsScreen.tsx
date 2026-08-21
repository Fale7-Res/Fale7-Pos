'use client';

import React from 'react';
import { useAppStore } from '@/lib/store';
import { UserCog } from 'lucide-react';
import { User } from '@/lib/types';

export default function UsersPermissionsScreen() {
  const { users, currentUser, updateUser, requestManagerAuth } = useAppStore();
  const permissionCatalog = [
    ['pos', 'نقطة البيع'], ['orders_view', 'عرض الطلبات'], ['orders_cancel', 'إلغاء طلب'],
    ['orders_discount', 'الخصومات'], ['orders_refund', 'المرتجعات'], ['shifts_blind_close', 'تقفيل شيفت'],
    ['expenses_add', 'المصروفات'], ['employees_ledger', 'سلف وسحب'], ['menu_manage', 'إدارة المنيو'],
    ['reports_view', 'التقارير'], ['audit_view', 'سجل الرقابة'], ['devices_manage', 'الأجهزة'],
  ];

  const togglePermission = (user: User, permission: string) => {
    if (user.role === 'owner') return;
    const next = user.permissions.includes(permission)
      ? user.permissions.filter((item) => item !== permission)
      : [...user.permissions, permission];
    updateUser({ ...user, permissions: next });
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-blue-500/10 text-blue-600 border border-blue-500/20 flex items-center justify-center">
            <UserCog className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">المستخدمين وصلاحيات النظام</h1>
            <p className="text-xs text-slate-500">
              إدارة حسابات تسجيل الدخول، رموز PIN السريعة، وصلاحيات كل دور ومستوى
            </p>
          </div>
        </div>
      </div>

      {/* 2. Users Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">المستخدم</th>
                <th className="py-3.5 px-4">اسم الدخول</th>
                <th className="py-3.5 px-4">الدور الوظيفي</th>
                <th className="py-3.5 px-4 text-center">رمز PIN</th>
                <th className="py-3.5 px-4 text-center">الصلاحيات</th>
                <th className="py-3.5 px-4 text-center">الحالة</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {users.map((u) => (
                <React.Fragment key={u.id}>
                <tr className="hover:bg-slate-50 transition-colors">
                  <td className="py-3.5 px-4 font-black text-slate-900 text-sm">
                    <div className="flex items-center gap-2">
                      <span className="text-xl">{u.avatar || '👤'}</span>
                      <span>{u.name}</span>
                    </div>
                  </td>
                  <td className="py-3.5 px-4 font-mono text-slate-600">@{u.username}</td>
                  <td className="py-3.5 px-4 font-bold text-red-600">
                    {u.role === 'owner'
                      ? 'المالك (كامل الصلاحيات)'
                      : u.role === 'manager'
                      ? 'مدير صالة وورديات'
                      : u.role === 'cashier'
                      ? 'كاشير'
                      : u.role === 'grill'
                      ? 'عامل محطة الفحم'
                      : 'مشرف'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-mono font-bold text-slate-900 bg-slate-50">
                    ••••••
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    <span className="text-[11px] text-slate-500">
                      {u.role === 'owner'
                        ? 'كافة شاشات النظام'
                        : u.role === 'manager'
                        ? 'الورديات، الرقابة، الموظفين'
                        : u.role === 'grill'
                        ? 'شاشة الفحم فقط'
                        : 'نقطة البيع وبونات الورديات'}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    <span
                      className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                        u.active ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-500'
                      }`}
                    >
                      {u.active ? 'نشط' : 'معطل'}
                    </span>
                  </td>
                </tr>
                <tr className="bg-slate-50/70">
                  <td colSpan={6} className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      {permissionCatalog.map(([key, label]) => {
                        const enabled = u.role === 'owner' || u.permissions.includes('all') || u.permissions.includes(key);
                        return <button key={key} type="button" disabled={u.role === 'owner'} onClick={() => togglePermission(u, key)} className={`px-2.5 py-1.5 rounded-lg border text-[10px] font-bold transition-colors ${enabled ? 'bg-emerald-50 border-emerald-200 text-emerald-800' : 'bg-white border-slate-200 text-slate-400'} disabled:cursor-default`}>{enabled ? '✓ ' : '○ '}{label}</button>;
                      })}
                      {u.id !== currentUser.id && <button type="button" onClick={() => requestManagerAuth(`${u.active ? 'تعطيل' : 'تفعيل'} حساب ${u.name}`, 'تغيير حالة حساب مستخدم يؤثر على قدرته على دخول النظام.', true, () => updateUser({ ...u, active: !u.active }))} className="px-3 py-1.5 rounded-lg border border-red-200 text-red-700 bg-red-50 text-[10px] font-bold">{u.active ? 'تعطيل الحساب' : 'إعادة التفعيل'}</button>}
                    </div>
                  </td>
                </tr>
                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
