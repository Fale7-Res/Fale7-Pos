import { CORE_STORAGE_KEY, ENTERPRISE_STORAGE_KEY, stagedWrite, writeJson } from './local-persistence.mjs';
export const BACKUP_VERSION = 4;
export const SYSTEM_EVENTS_KEY = 'falah-system-events-v1';
const arrays = ['products','categories','orders','shifts','employees','ledgerEntries','expenses','auditEvents','users','devices','cashMovements'];
const enterpriseArrays = ['treasuryAccounts','treasuryMovements','cashHandovers','suppliers','supplierLedger','purchases','inventoryItems','stockMovements','payrollRuns','payrollLines','leaveRequests','taxRules','businessDays','drivers','deliveries','diningTables'];
const hash = text => { let h=2166136261; for (let i=0;i<text.length;i++) h=Math.imul(h^text.charCodeAt(i),16777619); return (h>>>0).toString(16).padStart(8,'0'); };
const validMoney = item => !item || typeof item !== 'object' || ['amount','amountPiastres','paid','remainingAmount','debit','credit'].every(k => item[k] === undefined || (Number.isFinite(item[k]) && item[k] >= 0));
export function validateBackupEnvelope(value) {
  if (!value || typeof value !== 'object') throw new Error('ملف النسخة غير صالح.');
  if (value.version === 2 || value.version === 3 || value.backupVersion === 2 || value.backupVersion === 3) throw new Error('هذه النسخة قديمة ولا تحتوي على البيانات المالية الكاملة.');
  if (value.backupVersion !== BACKUP_VERSION || !value.core || !value.enterprise) throw new Error('إصدار النسخة الاحتياطية غير مدعوم.');
  if (!value.core.data || value.core.version !== 2 || !arrays.every(k => Array.isArray(value.core.data[k]))) throw new Error('بيانات التشغيل الأساسية داخل النسخة غير صالحة.');
  if (!value.core.data.settings || !enterpriseArrays.every(k => Array.isArray(value.enterprise[k]))) throw new Error('بيانات الوحدات المالية داخل النسخة غير صالحة.');
  if (![...value.core.data.expenses, ...value.enterprise.treasuryMovements, ...value.enterprise.supplierLedger, ...value.enterprise.purchases].every(validMoney)) throw new Error('تحتوي النسخة على قيمة مالية غير صالحة.');
  const body = JSON.stringify({ core:value.core, enterprise:value.enterprise, journalMetadata:value.journalMetadata });
  if (value.integrity?.algorithm !== 'fnv1a32' || value.integrity.checksum !== hash(body)) throw new Error('فشل التحقق من سلامة ملف النسخة الاحتياطية.');
  return value;
}
export function createBackupEnvelope({ core, enterprise, journalMetadata = {}, appVersion = 'unknown', createdAt = new Date().toISOString() }) {
  const body = JSON.stringify({ core, enterprise, journalMetadata });
  return { backupVersion:BACKUP_VERSION, appVersion, createdAt, containsSensitiveCredentials:true, sensitiveDataNotice:'تحتوي هذه النسخة على بيانات دخول المستخدمين ويجب حفظها في مكان آمن.', core, enterprise, journalMetadata, integrity:{ algorithm:'fnv1a32', checksum:hash(body) } };
}
function event(storage, type, detail) {
  let events=[]; try { events=JSON.parse(storage.getItem(SYSTEM_EVENTS_KEY)||'[]'); if(!Array.isArray(events)) events=[]; } catch {}
  writeJson(storage, SYSTEM_EVENTS_KEY, [{ id:`event-${Date.now()}-${Math.random()}`, type, timestamp:new Date().toISOString(), detail }, ...events].slice(0,200));
}
export function restoreBackupAtomically(storage, candidate) {
  event(storage, 'restore_attempt', { backupVersion:candidate?.backupVersion });
  let backup;
  try { backup=validateBackupEnvelope(candidate); }
  catch(error) { try { event(storage,'restore_validation_failed',{message:error.message}); } catch {} throw error; }
  const core = { ...backup.core, savedAt:new Date().toISOString(), data:{ ...backup.core.data, currentUserId:null } };
  try {
    const commitId=stagedWrite(storage,[{key:CORE_STORAGE_KEY,value:core},{key:ENTERPRISE_STORAGE_KEY,value:backup.enterprise}],{coreVersion:2,enterpriseVersion:1,operation:'restore'});
    event(storage,'restore_success',{commitId,backupVersion:backup.backupVersion}); return {commitId,requiresLogin:true};
  } catch(error) { try { event(storage,'restore_failed',{message:error.message}); } catch {} throw error; }
}
