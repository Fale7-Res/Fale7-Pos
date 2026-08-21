'use client';

import React, { useState } from 'react';
import { useAppStore } from '@/lib/store';
import { AttendanceRecord, Employee } from '@/lib/types';
import {
  CalendarCheck,
  Fingerprint,
  Clock,
  UserCheck,
  UserX,
  AlertTriangle,
  Plus,
  Check,
  X,
  ShieldAlert,
  Search,
} from 'lucide-react';

export default function AttendanceScreen() {
  const {
    attendance,
    employees,
    biometricCheckIn,
    biometricCheckOut,
    correctAttendance,
    requestManagerAuth,
  } = useAppStore();

  const [selectedEmpId, setSelectedEmpId] = useState<string>(employees[0]?.id || '');
  const [correctionModalOpen, setCorrectionModalOpen] = useState(false);
  const [targetRecord, setTargetRecord] = useState<AttendanceRecord | null>(null);
  const [correctStatus, setCorrectStatus] = useState<any>('present');
  const [correctCheckIn, setCorrectCheckIn] = useState('08:00 ص');
  const [correctCheckOut, setCorrectCheckOut] = useState('04:00 م');
  const [correctLateMins, setCorrectLateMins] = useState('0');
  const [correctReason, setCorrectReason] = useState('');

  // Handle Biometric Check-In
  const handleCheckIn = () => {
    const emp = employees.find((e) => e.id === selectedEmpId);
    if (!emp) return;
    biometricCheckIn(emp.id);
  };

  // Handle Biometric Check-Out
  const handleCheckOut = () => {
    const emp = employees.find((e) => e.id === selectedEmpId);
    if (!emp) return;
    biometricCheckOut(emp.id);
  };

  // Open Correction
  const handleOpenCorrection = (rec: AttendanceRecord) => {
    setTargetRecord(rec);
    setCorrectStatus(rec.status);
    setCorrectCheckIn(rec.checkIn || '08:00 ص');
    setCorrectCheckOut(rec.checkOut || '04:00 م');
    setCorrectLateMins(String(rec.lateMinutes || 0));
    setCorrectReason('');
    setCorrectionModalOpen(true);
  };

  // Submit Correction with Manager PIN
  const handleSaveCorrection = (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetRecord || !correctReason.trim()) return;

    requestManagerAuth(
      `تعديل يدوي لسجل حضور الموظف ${targetRecord.employeeName}`,
      `التاريخ: ${targetRecord.date} - الحالة الجديدة: ${correctStatus} - السبب: ${correctReason}`,
      true,
      (managerName) => {
        correctAttendance(
          targetRecord.id,
          correctStatus,
          Number(correctLateMins) || 0,
          targetRecord.workHours || 8,
          targetRecord.overtimeHours || 0,
          targetRecord.tatbiqCount || 0,
          correctReason,
          managerName
        );
        setCorrectionModalOpen(false);
      }
    );
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-cyan-500/10 text-cyan-600 border border-cyan-500/20 flex items-center justify-center">
            <Fingerprint className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">سجل الحضور والبصمة البيومترية</h1>
            <p className="text-xs text-slate-500">
              تسجيل بصمة الدخول والانصراف للموظفين وحساب ساعات العمل الإضافية والتأخيرات بدقة
            </p>
          </div>
        </div>
      </div>

      {/* 2. Biometric Simulator Box (Touch terminal for employees) */}
      <div className="bg-slate-900 text-white p-6 rounded-3xl border border-slate-800 shadow-lg flex flex-col md:flex-row items-center justify-between gap-6">
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-2xl bg-cyan-500/20 border border-cyan-500/30 flex items-center justify-center text-cyan-400">
            <Fingerprint className="w-10 h-10 animate-pulse" />
          </div>
          <div>
            <h3 className="text-base font-extrabold text-white">جهاز البصمة الإلكتروني المتصل (ZKTeco Live)</h3>
            <p className="text-xs text-slate-400 mt-0.5">
              اختر الموظف لتسجيل بصمة الحضور أو الانصراف وتحديث كشف الحساب فورياً
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 w-full md:w-auto">
          <select
            value={selectedEmpId}
            onChange={(e) => setSelectedEmpId(e.target.value)}
            className="bg-slate-800 border border-slate-700 text-white text-xs font-bold px-4 py-3 rounded-2xl focus:outline-hidden focus:border-cyan-500"
          >
            {employees
              .filter((e) => e.isActive)
              .map((emp) => (
                <option key={emp.id} value={emp.id}>
                  {emp.name} ({emp.jobTitle})
                </option>
              ))}
          </select>

          <button
            type="button"
            onClick={handleCheckIn}
            className="px-4 py-3 bg-cyan-600 hover:bg-cyan-500 active:bg-cyan-700 text-white text-xs font-black rounded-2xl shadow-md transition-all flex items-center gap-1.5 whitespace-nowrap"
          >
            <UserCheck className="w-4 h-4" />
            <span>بصمة حضور</span>
          </button>

          <button
            type="button"
            onClick={handleCheckOut}
            className="px-4 py-3 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 text-xs font-black rounded-2xl transition-all flex items-center gap-1.5 whitespace-nowrap"
          >
            <UserX className="w-4 h-4" />
            <span>بصمة انصراف</span>
          </button>
        </div>
      </div>

      {/* 3. Daily Attendance Records Table */}
      <div className="bg-white rounded-3xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CalendarCheck className="w-4 h-4 text-slate-600" />
            <h3 className="font-extrabold text-sm text-slate-800">سجل حضور الموظفين لليوم</h3>
          </div>
          <span className="text-xs text-slate-400 font-semibold">{attendance.length} سجلات مسجلة</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-right text-xs">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-slate-600 font-bold">
                <th className="py-3.5 px-4">الموظف</th>
                <th className="py-3.5 px-4">التاريخ</th>
                <th className="py-3.5 px-4 text-center">وقت الدخول</th>
                <th className="py-3.5 px-4 text-center">وقت الانصراف</th>
                <th className="py-3.5 px-4 text-center">التأخير</th>
                <th className="py-3.5 px-4 text-center">الساعات المنفذة</th>
                <th className="py-3.5 px-4 text-center">ساعات الإضافي</th>
                <th className="py-3.5 px-4 text-center">الحالة</th>
                <th className="py-3.5 px-4 text-center">تعديل يدوي</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {attendance.map((rec) => (
                <tr key={rec.id} className="hover:bg-slate-50 transition-colors">
                  <td className="py-3.5 px-4 font-bold text-slate-900">{rec.employeeName}</td>
                  <td className="py-3.5 px-4 text-slate-500 font-mono text-[11px]">{rec.date}</td>
                  <td className="py-3.5 px-4 text-center font-bold text-emerald-700 font-mono">
                    {rec.checkIn || '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-slate-700 font-mono">
                    {rec.checkOut || '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold">
                    {rec.lateMinutes && rec.lateMinutes > 0 ? (
                      <span className="text-amber-600">{rec.lateMinutes} دقيقة</span>
                    ) : (
                      <span className="text-slate-400 font-normal">لا يوجد</span>
                    )}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-slate-800">
                    {rec.workHours ? `${rec.workHours} ساعات` : '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-blue-600">
                    {rec.overtimeHours && rec.overtimeHours > 0 ? `+${rec.overtimeHours} س` : '-'}
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    <span
                      className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-black ${
                        rec.status === 'present'
                          ? 'bg-emerald-100 text-emerald-800'
                          : rec.status === 'late'
                          ? 'bg-amber-100 text-amber-800'
                          : 'bg-red-100 text-red-800'
                      }`}
                    >
                      {rec.status === 'present' ? 'حاضر' : rec.status === 'late' ? 'متأخر' : 'غائب'}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    <button
                      type="button"
                      onClick={() => handleOpenCorrection(rec)}
                      className="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-bold transition-colors"
                    >
                      تصحيح
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4. CORRECTION MODAL */}
      {correctionModalOpen && targetRecord && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in">
          <form
            onSubmit={handleSaveCorrection}
            className="bg-white rounded-3xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 p-6 space-y-4 text-slate-800"
          >
            <div className="flex justify-between items-center border-b border-slate-100 pb-3">
              <div>
                <span className="text-xs font-bold text-red-600 block">تصحيح حضور يدوي</span>
                <h3 className="font-extrabold text-base text-slate-900">{targetRecord.employeeName}</h3>
              </div>
              <button
                type="button"
                onClick={() => setCorrectionModalOpen(false)}
                className="p-1 text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">الحالة:</label>
              <select
                value={correctStatus}
                onChange={(e) => setCorrectStatus(e.target.value as any)}
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2.5"
              >
                <option value="present">حاضر (في الموعد)</option>
                <option value="late">متأخر</option>
                <option value="absent">غائب</option>
              </select>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">وقت الدخول:</label>
                <input
                  type="text"
                  value={correctCheckIn}
                  onChange={(e) => setCorrectCheckIn(e.target.value)}
                  className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">وقت الانصراف:</label>
                <input
                  type="text"
                  value={correctCheckOut}
                  onChange={(e) => setCorrectCheckOut(e.target.value)}
                  className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-700 mb-1">دقائق التأخير:</label>
              <input
                type="number"
                value={correctLateMins}
                onChange={(e) => setCorrectLateMins(e.target.value)}
                className="w-full text-xs font-bold bg-slate-50 border border-slate-200 rounded-xl p-2"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-red-700 mb-1">سبب التصحيح * (مطلوب للتدقيق):</label>
              <input
                type="text"
                value={correctReason}
                onChange={(e) => setCorrectReason(e.target.value)}
                placeholder="مثال: عطل مؤقت في جهاز البصمة، إذن مسبق..."
                className="w-full text-xs bg-slate-50 border border-slate-300 rounded-xl p-2.5 text-slate-900"
                required
              />
            </div>

            <p className="text-[11px] text-red-700 bg-red-50 p-2 rounded-xl border border-red-200">
              🔒 يتطلب حفظ التصحيح موافقة المدير وتوثيقه في سجل الرقابة.
            </p>

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setCorrectionModalOpen(false)}
                className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl"
              >
                إلغاء
              </button>
              <button
                type="submit"
                className="flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white text-xs font-bold rounded-xl shadow-md"
              >
                طلب اعتماد التصحيح
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
