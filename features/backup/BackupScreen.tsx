'use client';

import React, { useRef, useState } from 'react';
import { useAppStore } from '@/lib/store';
import { useEnterpriseStore } from '@/lib/enterprise-store';
import { Database, Download, Upload, ShieldCheck, CheckCircle2, AlertTriangle, RefreshCw } from 'lucide-react';
import { createBackupEnvelope, restoreBackupAtomically, validateBackupEnvelope } from '@/lib/backup-domain.mjs';
import { readFinancialJournal } from '@/lib/financial-journal.mjs';
import { writeJson } from '@/lib/local-persistence.mjs';

export default function BackupScreen() {
  const enterprise = useEnterpriseStore();
  const {
    products,
    categories,
    orders,
    grillTickets,
    shifts,
    employees,
    ledgerEntries,
    attendance,
    expenses,
    auditEvents,
    settings,
    users,
    devices,
    cashMovements,
    notifications,
    currentUser,
    requestManagerAuth,
    logAuditEvent,
  } = useAppStore();

  const [downloadSuccess, setDownloadSuccess] = useState(false);
  const [restoreMessage, setRestoreMessage] = useState('');
  const restoreInputRef = useRef<HTMLInputElement>(null);

  // Handle Export Backup
  const handleExportBackup = () => {
    const core = { version: 2, savedAt: new Date().toISOString(), data: {
        products,
        categories,
        orders,
        grillTickets,
        shifts,
        employees,
        ledgerEntries,
        attendance,
        expenses,
        auditEvents,
        settings,
        users,
        devices,
        cashMovements,
        notifications,
        currentUserId: currentUser.id,
      } };
    const journal = readFinancialJournal(window.localStorage);
    const backupData = createBackupEnvelope({ core, enterprise: enterprise.exportState(), appVersion: '2.3.0', journalMetadata: { entryCount: journal.length, pendingCount: journal.filter(item => item.status !== 'complete').length } });

    const dataStr = 'data:text/json;charset=utf-8,' + encodeURIComponent(JSON.stringify(backupData, null, 2));
    const downloadAnchor = document.createElement('a');
    downloadAnchor.setAttribute('href', dataStr);
    downloadAnchor.setAttribute(
      'download',
      `FALEH_BACKUP_${new Date().toISOString().split('T')[0]}_${Date.now()}.json`
    );
    document.body.appendChild(downloadAnchor);
    downloadAnchor.click();
    downloadAnchor.remove();

    logAuditEvent({
      userId: currentUser.id,
      userName: currentUser.name,
      userRole: currentUser.role,
      action: 'تصدير نسخة احتياطية كاملة',
      category: 'settings',
      details: 'تم تصدير ملف النسخة الاحتياطية بنجاح إلى الجهاز',
      severity: 'info',
    });
    setDownloadSuccess(true);
    writeJson(window.localStorage, 'fateh-last-backup-at', new Date().toISOString());
    setTimeout(() => setDownloadSuccess(false), 4000);
  };

  const handleRestoreFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    setRestoreMessage('');
    try {
      if (file.size > 25 * 1024 * 1024) {
        throw new Error('حجم ملف النسخة أكبر من الحد المسموح (25 ميجابايت).');
      }
      const validated = validateBackupEnvelope(JSON.parse(await file.text()));
      const parsed = { ...validated, version: 3, data: validated.core.data };
      const requiredArrays = ['products', 'categories', 'orders', 'shifts', 'employees', 'ledgerEntries', 'auditEvents'];
      if (
        ![2, 3].includes(parsed.version) ||
        !parsed.data ||
        !requiredArrays.every(key => Array.isArray(parsed.data[key])) ||
        !parsed.data.settings ||
        !Array.isArray(parsed.data.users)
      ) {
        throw new Error('ملف النسخة غير صالح أو إصدار غير مدعوم.');
      }
      requestManagerAuth(
        'استعادة نسخة احتياطية',
        'سيتم استبدال بيانات النسخة التجريبية الحالية ثم إعادة تحميل النظام.',
        true,
        () => {
          try { restoreBackupAtomically(window.localStorage, validated); window.location.reload(); }
          catch (error) { setRestoreMessage(error instanceof Error ? error.message : 'فشلت الاستعادة ولم يتم تغيير البيانات الحالية.'); }
        }
      );
    } catch (error) {
      setRestoreMessage(error instanceof Error ? error.message : 'تعذر قراءة ملف النسخة.');
    } finally {
      event.target.value = '';
    }
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-100 p-6 space-y-6 font-sans select-none" dir="rtl">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-3xl border border-slate-200 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-emerald-500/10 text-emerald-600 border border-emerald-500/20 flex items-center justify-center">
            <Database className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-black text-slate-900">النسخ الاحتياطي واستعادة البيانات (Offline Backup)</h1>
            <p className="text-xs text-slate-500">
              حفظ وتصدير قاعدة بيانات المطعم محلياً للعمل بدون انقطاع وضمان عدم ضياع أي فاتورة أو حركة مالية
            </p>
          </div>
        </div>
      </div>

      {/* 2. Status & Export Box */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Export Card */}
        <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <Download className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-extrabold text-base text-slate-900">تصدير نسخة احتياطية فورية</h3>
              <p className="text-xs text-slate-500">حفظ ملف JSON يحتوي كافة البيانات والطلبات واليوميات</p>
            </div>
          </div>

          <p className="text-xs text-slate-600 leading-relaxed">
            يتم حفظ كافة المعاملات المالية، كشوف الحسابات، بونات الفحم، وإغلاقات الورديات في ملف JSON محلي يمكن نقله
            على فلاشة USB.
          </p>

          <button
            type="button"
            onClick={handleExportBackup}
            className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-bold text-xs rounded-2xl shadow-md transition-all flex items-center justify-center gap-2"
          >
            <Download className="w-4 h-4" />
            <span>تنزيل النسخة الاحتياطية الآن</span>
          </button>
          <input ref={restoreInputRef} type="file" accept="application/json,.json" onChange={handleRestoreFile} className="hidden" />
          <button
            type="button"
            onClick={() => restoreInputRef.current?.click()}
            className="w-full py-3 bg-slate-900 hover:bg-slate-800 text-white font-bold text-xs rounded-2xl flex items-center justify-center gap-2"
          >
            <Upload className="w-4 h-4" />
            <span>استعادة نسخة مع اعتماد المدير</span>
          </button>
          {restoreMessage && <div className="p-3 bg-red-50 text-red-700 border border-red-200 rounded-xl text-xs font-bold">{restoreMessage}</div>}

          {downloadSuccess && (
            <div className="p-3 bg-emerald-50 text-emerald-800 border border-emerald-200 rounded-xl text-xs font-bold flex items-center gap-2 animate-in fade-in">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              <span>تم تجهيز وتنزيل ملف النسخة الاحتياطية بنجاح على جهازك!</span>
            </div>
          )}
        </div>

        {/* Local Persistence Status Card */}
        <div className="bg-white p-6 rounded-3xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-extrabold text-base text-slate-900">حالة التخزين المحلي الآمن (Local Storage)</h3>
              <p className="text-xs text-slate-500">حفظ محلي مستمر على جهاز الكاشير للنسخة التجريبية</p>
            </div>
          </div>

          <div className="space-y-2 text-xs text-slate-600">
            <div className="flex justify-between p-2.5 bg-emerald-50 rounded-xl text-emerald-800">
              <span>حالة الحفظ التلقائي:</span>
              <strong>مفعّل داخل الجهاز</strong>
            </div>
            <div className="flex justify-between p-2.5 bg-slate-50 rounded-xl">
              <span>إجمالي الطلبات المحفوظة:</span>
              <strong className="text-slate-900 font-mono">{orders.length} طلب</strong>
            </div>
            <div className="flex justify-between p-2.5 bg-slate-50 rounded-xl">
              <span>حركات كشف الحساب المسجلة:</span>
              <strong className="text-slate-900 font-mono">{ledgerEntries.length} قيد</strong>
            </div>
            <div className="flex justify-between p-2.5 bg-slate-50 rounded-xl">
              <span>سجلات الرقابة والتدقيق:</span>
              <strong className="text-slate-900 font-mono">{auditEvents.length} حدث</strong>
            </div>
            <div className="flex justify-between p-2.5 bg-slate-50 rounded-xl">
              <span>حركات الخزنة والمخزون:</span>
              <strong className="text-slate-900 font-mono">{enterprise.treasuryMovements.length + enterprise.stockMovements.length} حركة</strong>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
