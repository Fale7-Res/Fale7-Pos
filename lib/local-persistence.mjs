export const CORE_STORAGE_KEY = 'fateh-restaurant-os-v2';
export const ENTERPRISE_STORAGE_KEY = 'falah-enterprise-domains-v1';
export const COMMIT_MARKER_KEY = 'falah-persistence-commit-v1';

const messages = {
  QUOTA: 'تعذر حفظ العملية على الجهاز. لم يتم اعتماد العملية. راجع مساحة التخزين.',
  UNAVAILABLE: 'التخزين المحلي غير متاح. تم إيقاف العمليات المالية لحماية البيانات.',
  CORRUPT: 'البيانات المحفوظة تالفة. تم إيقاف العمليات المالية لحين الاستعادة.',
  VERSION_MISMATCH: 'إصدار البيانات المحفوظة غير متوافق مع هذا الإصدار.',
  WRITE_FAILED: 'تعذر حفظ العملية على الجهاز. لم يتم اعتماد العملية.',
};

export class PersistenceError extends Error {
  constructor(code, detail, cause) {
    super(messages[code] || messages.WRITE_FAILED, { cause });
    this.name = 'PersistenceError'; this.code = code; this.detail = detail;
  }
}

export function safeParse(raw, label = 'البيانات') {
  try { return JSON.parse(raw); }
  catch (error) { throw new PersistenceError('CORRUPT', `${label}: invalid JSON`, error); }
}

export function storageAvailable(storage) {
  if (!storage) return false;
  const key = '__falah_storage_probe__';
  try { storage.setItem(key, '1'); storage.removeItem(key); return true; } catch { return false; }
}

export function classifyStorageError(error) {
  if (error?.name === 'QuotaExceededError' || error?.code === 22 || error?.code === 1014) return 'QUOTA';
  return 'WRITE_FAILED';
}

export function readJson(storage, key, options = {}) {
  let raw;
  try { raw = storage?.getItem(key); }
  catch (error) { throw new PersistenceError('UNAVAILABLE', key, error); }
  if (raw === null || raw === undefined) return { exists: false, value: options.fallback };
  const value = safeParse(raw, key);
  if (options.validate && !options.validate(value)) throw new PersistenceError('CORRUPT', `${key}: invalid structure`);
  return { exists: true, value, raw };
}

export function writeJson(storage, key, value) {
  let serialized;
  try { serialized = JSON.stringify(value); }
  catch (error) { throw new PersistenceError('WRITE_FAILED', `${key}: serialization failed`, error); }
  try {
    storage.setItem(key, serialized);
    if (storage.getItem(key) !== serialized) throw new Error('write verification failed');
  } catch (error) { throw new PersistenceError(classifyStorageError(error), key, error); }
  return serialized;
}

export function stagedWrite(storage, writes, marker = {}) {
  const tx = marker.commitId || `commit-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  const originals = new Map(writes.map(({ key }) => [key, storage.getItem(key)]));
  const staged = writes.map(({ key, value }) => ({ key, value, tempKey: `${key}.staging.${tx}` }));
  try {
    for (const item of staged) writeJson(storage, item.tempKey, item.value);
    writeJson(storage, COMMIT_MARKER_KEY, { ...marker, commitId: tx, timestamp: new Date().toISOString(), status: 'staging' });
    for (const item of staged) writeJson(storage, item.key, item.value);
    writeJson(storage, COMMIT_MARKER_KEY, { ...marker, commitId: tx, timestamp: new Date().toISOString(), status: 'committed' });
  } catch (error) {
    for (const [key, raw] of originals) { try { raw === null ? storage.removeItem(key) : storage.setItem(key, raw); } catch {} }
    try { writeJson(storage, COMMIT_MARKER_KEY, { ...marker, commitId: tx, timestamp: new Date().toISOString(), status: 'failed' }); } catch {}
    throw error instanceof PersistenceError ? error : new PersistenceError(classifyStorageError(error), 'staged write', error);
  } finally { for (const item of staged) { try { storage.removeItem(item.tempKey); } catch {} } }
  return tx;
}

export function assertCommittedSnapshot(storage) {
  const result = readJson(storage, COMMIT_MARKER_KEY);
  if (result.exists && result.value?.status !== 'committed') throw new PersistenceError('CORRUPT', 'incomplete coordinated commit');
  return result.value || null;
}
