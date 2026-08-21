'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { Employee, EmployeeLedgerEntry } from '@/lib/types';
import {
  Users,
  Plus,
  FileText,
  Edit2,
  Trash2,
  Phone,
  Calendar,
  DollarSign,
  Printer,
  X,
  Check,
  Search,
  KeyRound,
  ArrowDownRight,
  ArrowUpRight,
  ShieldCheck,
} from 'lucide-react';

export default function EmployeesScreen() {
  const {
    employees,
    ledgerEntries,
    calculateEmployeeBalance,
    addEmployee,
    updateEmployee,
    recordManualLedgerEntry,
    currentUser,
    requestManagerAuth,
    settings,
  } = useAppStore();

  const [searchQuery, setSearchQuery] = useState('');
  const [statementEmp, setStatementEmp] = useState<Employee | null>(null);
  const [editModalEmp, setEditModalEmp] = useState<Employee | null>(null);
  const [isNewEmp, setIsNewEmp] = useState(false);

  // Manual entry modal inside statement
  const [manualEntryOpen, setManualEntryOpen] = useState(false);
  const [manualType, setManualType] = useState<'credit' | 'debit'>('credit');
  const [manualEntryType, setManualEntryType] = useState<any>('bonus');
  const [manualAmount, setManualAmount] = useState('');
  const [manualDesc, setManualDesc] = useState('');
  const [manualReason, setManualReason] = useState('');

  // Edit employee form state
  const [formData, setFormData] = useState<Partial<Employee>>({
    name: '',
    phone: '',
    role: 'cashier',
    jobTitle: 'كاشير',
    compensationType: 'daily',
    dailyWage: 200,
    shiftValue: 200,
    pin: '1111',
    isActive: true,
  });

  const filteredEmployees = employees.filter(
    (e) =>
      e.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      e.phone.includes(searchQuery) ||
      e.jobTitle.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Open Edit or Add Employee
  const handleOpenForm = (emp?: Employee) => {
    if (emp) {
      setIsNewEmp(false);
      setEditModalEmp(emp);
      setFormData({ ...emp });
    } else {
      setIsNewEmp(true);
      setEditModalEmp(null);
      setFormData({
        name: '',
        phone: '',
        role: 'cashier',
        jobTitle: 'عامل',
        compensationType: 'daily',
        dailyWage: 200,
        shiftValue: 200,
        pin: '1234',
        isActive: true,
      });
    }
  };

  // Save Employee
  const handleSaveEmployee = (e: React.FormEvent) => {
    e.preventDefault();
    if (isNewEmp) {
      addEmployee({
        name: formData.name || 'موظف جديد',
        employeeNumber: `EMP-${Math.floor(100 + Math.random() * 900)}`,
        phone: formData.phone || '',
        role: formData.role || 'cashier',
        jobTitle: formData.jobTitle || 'موظف',
        compensationType: formData.compensationType || 'daily',
        dailyWage: Number(formData.dailyWage) || 200,
        shiftValue: Number(formData.shiftValue) || 200,
        monthlySalary: Number(formData.monthlySalary) || 0,
        pin: formData.pin || '1234',
        hireDate: new Date().toISOString().split('T')[0],
        isActive: true,
      });
    } else if (editModalEmp) {
      updateEmployee({ ...editModalEmp, ...formData } as Employee);
    }
    setEditModalEmp(null);
    setIsNewEmp(false);
  };

  // Submit Manual Ledger Adjustment
  const handleSaveManualEntry = (e: React.FormEvent) => {
    e.preventDefault();
    if (!statementEmp) return;
    const amt = Number(manualAmount) || 0;
    if (amt <= 0 || !manualDesc) return;

    requestManagerAuth(
      `تسجيل قيد مالي يدوي للموظف ${statementEmp.name}`,
      `نوع القيد: ${manualType === 'credit' ? 'مستحق للموظف (+)' : 'مخصوم من الموظف (-)'} بقيمة ${amt} ج - البيان: ${manualDesc} - السبب: ${manualReason || 'تسوية يدوية'}`,
      true,
      (managerName) => {
        recordManualLedgerEntry(
          statementEmp.id,
          manualEntryType,
          amt,
          manualType === 'credit',
          manualDesc,
          managerName
        );
        setManualEntryOpen(false);
        setManualAmount('');
        setManualDesc('');
        setManualReason('');
      }
    );
  };

  // Entries for selected statement employee
  const statementEntries = statementEmp
    ? ledgerEntries
        .filter((l) => l.employeeId === statementEmp.id)
        .sort((a, b) => (b.date + b.time).localeCompare(a.date + a.time))
    : [];

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-blue-500/10 text-blue-600 border border-blue-500/20 flex items-center justify-center">
            <Users className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">شؤون الموظفين وكشوف الحسابات المالية</h1>
            <p className="text-xs text-slate-500">
              إدارة بيانات الموظفين، الرواتب واليوميات، وسجل كشف الحساب التراكمي الشامل
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
              placeholder="ابحث بالاسم أو الهاتف..."
              className="bg-slate-100 text-xs pr-9 pl-3 py-2 rounded-xl border border-slate-200 focus:outline-hidden focus:border-red-500"
            />
          </div>

          <button
            type="button"
            onClick={() => handleOpenForm()}
            className="px-4 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md transition-all flex items-center gap-1.5"
          >
            <Plus className="w-4 h-4" />
            <span>إضافة موظف جديد</span>
          </button>
        </div>
      </div>

      {/* 2. Employees Grid / Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredEmployees.map((emp) => {
          const balance = calculateEmployeeBalance(emp.id);
          return (
            <div
              key={emp.id}
              className="bg-white rounded-3xl p-5 border border-slate-200 shadow-xs hover:shadow-md transition-all space-y-4 flex flex-col justify-between"
            >
              <div>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-2xl bg-slate-100 border border-slate-200 flex items-center justify-center text-2xl">
                      {emp.role === 'manager'
                        ? '👔'
                        : emp.role === 'cashier'
                        ? '🧑‍💼'
                        : emp.role === 'grill'
                        ? '👨‍🍳'
                        : '👷'}
                    </div>
                    <div>
                      <h3 className="font-extrabold text-base text-slate-900">{emp.name}</h3>
                      <p className="text-xs text-red-600 font-bold">{emp.jobTitle}</p>
                    </div>
                  </div>

                  <span
                    className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                      emp.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-500'
                    }`}
                  >
                    {emp.isActive ? 'نشط' : 'معطل'}
                  </span>
                </div>

                {/* Details */}
                <div className="grid grid-cols-2 gap-2 text-xs pt-3 text-slate-600">
                  <div>
                    <span className="text-slate-400 block text-[10px]">كود الموظف:</span>
                    <span className="font-semibold font-mono text-slate-800">{emp.employeeNumber}</span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[10px]">نظام الأجر:</span>
                    <span className="font-semibold text-slate-800">
                      {emp.compensationType === 'daily' ? `يومية ${emp.dailyWage} ج` : `شهري ${emp.monthlySalary || 0} ج`}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[10px]">رقم الهاتف:</span>
                    <span className="font-mono text-slate-700">{emp.phone}</span>
                  </div>
                  <div>
                    <span className="text-slate-400 block text-[10px]">رمز PIN الدخول:</span>
                    <span className="font-mono text-slate-700">••••</span>
                  </div>
                </div>

                {/* Balance badge */}
                <div className="mt-3 p-3 bg-slate-50 rounded-2xl border border-slate-200 flex items-center justify-between">
                  <span className="text-xs font-bold text-slate-600">الرصيد المستحق:</span>
                  <span
                    className={`text-sm font-black px-2.5 py-0.5 rounded-lg ${
                      balance > 0
                        ? 'bg-emerald-100 text-emerald-800'
                        : balance < 0
                        ? 'bg-red-100 text-red-800'
                        : 'bg-slate-200 text-slate-700'
                    }`}
                  >
                    {balance > 0
                      ? `مستحق له: +${balance} ج`
                      : balance < 0
                      ? `مطلوب منه: ${balance} ج`
                      : 'خالص (0 ج)'}
                  </span>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="flex gap-2 pt-2 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setStatementEmp(emp)}
                  className="flex-1 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl transition-colors flex items-center justify-center gap-1.5 shadow-xs"
                >
                  <FileText className="w-3.5 h-3.5" />
                  <span>كشف الحساب</span>
                </button>
                <button
                  type="button"
                  onClick={() => handleOpenForm(emp)}
                  className="p-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl transition-colors"
                  title="تعديل بيانات الموظف"
                >
                  <Edit2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* 3. EMPLOYEE STATEMENT (كشف الحساب) MODAL */}
      {statementEmp && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-xs p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl max-w-3xl w-full overflow-hidden border border-slate-200 flex flex-col max-h-[90vh] text-slate-800">
            {/* Header */}
            <div className="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-extrabold text-lg">كشف حساب مالي تفصيلي</h3>
                  <span className="text-xs bg-red-600 text-white font-bold px-2 py-0.5 rounded-full">
                    {statementEmp.jobTitle}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  الموظف: {statementEmp.name} • الهاتف: {statementEmp.phone}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setStatementEmp(null)}
                className="p-1 text-slate-400 hover:text-white rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Statement Summary Card */}
            <div className="bg-slate-50 p-4 border-b border-slate-200 flex items-center justify-between">
              <div className="flex items-center gap-4 text-xs">
                <div>
                  <span className="text-slate-400 block text-[10px]">الرصيد التراكمي الصافي:</span>
                  <span
                    className={`text-xl font-black ${
                      calculateEmployeeBalance(statementEmp.id) >= 0 ? 'text-emerald-700' : 'text-red-700'
                    }`}
                  >
                    {calculateEmployeeBalance(statementEmp.id)} ج.م
                  </span>
                </div>
                <div className="h-8 w-px bg-slate-200"></div>
                <div>
                  <span className="text-slate-400 block text-[10px]">اليومية المقررة:</span>
                  <span className="font-bold text-slate-800">{statementEmp.dailyWage || 0} ج</span>
                </div>
              </div>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setManualEntryOpen(true)}
                  className="px-3 py-1.5 bg-amber-600 hover:bg-amber-700 text-white text-xs font-bold rounded-xl shadow-xs flex items-center gap-1"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>تسوية / قيد يدوي</span>
                </button>
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="px-3 py-1.5 bg-slate-200 hover:bg-slate-300 text-slate-800 text-xs font-bold rounded-xl flex items-center gap-1"
                >
                  <Printer className="w-3.5 h-3.5" />
                  <span>طباعة الكشف</span>
                </button>
              </div>
            </div>

            {/* Statement Table */}
            <div className="flex-1 overflow-y-auto p-4">
              <table className="w-full text-right text-xs">
                <thead>
                  <tr className="bg-slate-100 border-b border-slate-200 text-slate-600 font-bold">
                    <th className="py-2.5 px-3">التاريخ والوقت</th>
                    <th className="py-2.5 px-3">البيان / الحركة</th>
                    <th className="py-2.5 px-3 text-center">المستحق له (+)</th>
                    <th className="py-2.5 px-3 text-center">المخصوم منه (-)</th>
                    <th className="py-2.5 px-3 text-center">الرصيد بعد الحركة</th>
                    <th className="py-2.5 px-3">المعتمد</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {statementEntries.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="text-center py-6 text-slate-400">
                        لا توجد حركات مالية مسجلة لهذا الموظف حتى الآن.
                      </td>
                    </tr>
                  ) : (
                    statementEntries.map((entry) => (
                      <tr key={entry.id} className="hover:bg-slate-50 transition-colors">
                        <td className="py-2.5 px-3 text-slate-500 font-mono text-[11px]">{entry.date} {entry.time}</td>
                        <td className="py-2.5 px-3 font-medium text-slate-800">
                          <div>{entry.description}</div>
                        </td>
                        <td className="py-2.5 px-3 text-center font-bold text-emerald-600">
                          {entry.credit > 0 ? `+${entry.credit} ج` : '-'}
                        </td>
                        <td className="py-2.5 px-3 text-center font-bold text-red-600">
                          {entry.debit > 0 ? `-${entry.debit} ج` : '-'}
                        </td>
                        <td className="py-2.5 px-3 text-center font-black text-slate-900">
                          {entry.balanceAfter} ج
                        </td>
                        <td className="py-2.5 px-3 text-slate-500 text-[11px]">
                          {entry.authorizedBy || entry.recordedBy}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* 4. MANUAL LEDGER ENTRY MODAL */}
      {manualEntryOpen && statementEmp && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleSaveManualEntry}
            className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-base text-slate-900">
                إضافة قيد مالي يدوي - {statementEmp.name}
              </h3>
              <button
                type="button"
                onClick={() => setManualEntryOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => {
                  setManualType('credit');
                  setManualEntryType('bonus');
                }}
                className={`py-2 rounded-xl text-xs font-bold border transition-all ${
                  manualType === 'credit'
                    ? 'bg-emerald-50 border-emerald-600 text-emerald-800 shadow-xs'
                    : 'border-slate-200 text-slate-600'
                }`}
              >
                مستحق للموظف (+)
              </button>
              <button
                type="button"
                onClick={() => {
                  setManualType('debit');
                  setManualEntryType('penalty');
                }}
                className={`py-2 rounded-xl text-xs font-bold border transition-all ${
                  manualType === 'debit'
                    ? 'bg-red-50 border-red-600 text-red-800 shadow-xs'
                    : 'border-slate-200 text-slate-600'
                }`}
              >
                مخصوم من الموظف (-)
              </button>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">المبلغ (بالجنيه):</label>
              <input
                type="number"
                value={manualAmount}
                onChange={(e) => setManualAmount(e.target.value)}
                placeholder="أدخل المبلغ"
                className="w-full text-base font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                autoFocus
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">البيان / نوع التسوية:</label>
              <input
                type="text"
                value={manualDesc}
                onChange={(e) => setManualDesc(e.target.value)}
                placeholder="مثال: مكافأة تميز، خصم تأخير، تسوية رصيد سابق..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">السبب الموثق (للمدير):</label>
              <input
                type="text"
                value={manualReason}
                onChange={(e) => setManualReason(e.target.value)}
                placeholder="اكتب سبب العملية..."
                className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-800"
              />
            </div>

            <p className="text-[11px] text-red-700 bg-red-50 p-2 rounded-xl border border-red-200">
              🔒 يتطلب حفظ القيد اليدوي اعتماد المدير وإدخال رمز PIN.
            </p>

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setManualEntryOpen(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                طلب اعتماد القيد
              </button>
            </div>
          </form>
        </div>
      )}

      {/* 5. ADD / EDIT EMPLOYEE FORM MODAL */}
      {(editModalEmp || isNewEmp) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleSaveEmployee}
            className="bg-white rounded-3xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-base text-slate-900">
                {isNewEmp ? 'إضافة موظف جديد' : `تعديل بيانات ${editModalEmp?.name}`}
              </h3>
              <button
                type="button"
                onClick={() => {
                  setEditModalEmp(null);
                  setIsNewEmp(false);
                }}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">الاسم الكامل:</label>
                <input
                  type="text"
                  value={formData.name || ''}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="اسم الموظف"
                  className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">رقم الهاتف:</label>
                <input
                  type="text"
                  value={formData.phone || ''}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  placeholder="01012345678"
                  className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                  required
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">المسمى الوظيفي:</label>
                <input
                  type="text"
                  value={formData.jobTitle || ''}
                  onChange={(e) => setFormData({ ...formData, jobTitle: e.target.value })}
                  placeholder="مثال: كاشير، شيف مشويات..."
                  className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">الدور والصلاحيات:</label>
                <select
                  value={formData.role || 'cashier'}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value as any })}
                  className="w-full text-xs bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900 font-bold"
                >
                  <option value="cashier">كاشير (POS)</option>
                  <option value="grill">عامل شواية وفحم</option>
                  <option value="manager">مدير وردية / صالة</option>
                  <option value="supervisor">مشرف</option>
                  <option value="owner">مالك</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">اليومية (ج):</label>
                <input
                  type="number"
                  value={formData.dailyWage || 0}
                  onChange={(e) => setFormData({ ...formData, dailyWage: Number(e.target.value) })}
                  className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">رمز PIN للدخول:</label>
                <input
                  type="text"
                  value={formData.pin || '1234'}
                  onChange={(e) => setFormData({ ...formData, pin: e.target.value })}
                  maxLength={6}
                  className="w-full text-xs font-bold font-mono bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-slate-900"
                  required
                />
              </div>
            </div>

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => {
                  setEditModalEmp(null);
                  setIsNewEmp(false);
                }}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                حفظ بيانات الموظف
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
