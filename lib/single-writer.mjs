import { PersistenceError, readJson, writeJson } from './local-persistence.mjs';
export const WRITER_LOCK_KEY = 'falah-single-writer-lock-v1';
export function acquireWriterLock(storage, instanceId, now = Date.now(), ttlMs = 15000) {
  const current = readJson(storage, WRITER_LOCK_KEY, { fallback: null }).value;
  if (current && current.instanceId !== instanceId && current.expiresAt > now) return false;
  writeJson(storage, WRITER_LOCK_KEY, { instanceId, expiresAt: now + ttlMs });
  return readJson(storage, WRITER_LOCK_KEY).value?.instanceId === instanceId;
}
export function refreshWriterLock(storage, instanceId, now = Date.now(), ttlMs = 15000) {
  if (!acquireWriterLock(storage, instanceId, now, ttlMs)) throw new PersistenceError('UNAVAILABLE', 'another writer is active');
}
export function releaseWriterLock(storage, instanceId) {
  const current = readJson(storage, WRITER_LOCK_KEY, { fallback: null }).value;
  if (current?.instanceId === instanceId) storage.removeItem(WRITER_LOCK_KEY);
}
export function assertWriter(storage, instanceId, now = Date.now()) {
  const current = readJson(storage, WRITER_LOCK_KEY, { fallback: null }).value;
  if (!current || current.instanceId !== instanceId || current.expiresAt <= now) throw new PersistenceError('UNAVAILABLE', 'another writer is active');
}
