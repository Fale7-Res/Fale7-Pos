import { PersistenceError, readJson, writeJson } from './local-persistence.mjs';
export const FINANCIAL_JOURNAL_KEY = 'falah-financial-journal-v1';
export const FINANCIAL_JOURNAL_RETENTION = 500;
const validEntry = entry => entry && typeof entry === 'object' && typeof entry.operationId === 'string' && typeof entry.idempotencyKey === 'string' && ['pending','complete','failed','recovery_required'].includes(entry.status);
export function readFinancialJournal(storage) {
  const result = readJson(storage, FINANCIAL_JOURNAL_KEY, { fallback: [], validate: value => Array.isArray(value) && value.every(validEntry) });
  return result.value;
}
export function pruneFinancialJournal(entries, retention = FINANCIAL_JOURNAL_RETENTION) {
  const protectedEntries = entries.filter(x => x.status !== 'complete');
  const completed = entries.filter(x => x.status === 'complete').sort((a,b) => String(b.createdAt).localeCompare(String(a.createdAt))).slice(0, retention);
  return [...protectedEntries, ...completed].sort((a,b) => String(b.createdAt).localeCompare(String(a.createdAt)));
}
export function writeFinancialJournal(storage, entries, retention) { writeJson(storage, FINANCIAL_JOURNAL_KEY, pruneFinancialJournal(entries, retention)); }
export function startJournal(storage, entry) {
  const entries = readFinancialJournal(storage);
  if (entries.some(x => x.idempotencyKey === entry.idempotencyKey)) return false;
  writeFinancialJournal(storage, [{ ...entry, status:'pending', createdAt: entry.createdAt || new Date().toISOString() }, ...entries]); return true;
}
export function completeJournal(storage, idempotencyKey, payload = {}) {
  const entries = readFinancialJournal(storage); const current = entries.find(x => x.idempotencyKey === idempotencyKey);
  if (!current) throw new PersistenceError('CORRUPT', `missing pending journal entry: ${idempotencyKey}`);
  writeFinancialJournal(storage, [{ ...current, ...payload, status:'complete' }, ...entries.filter(x => x.idempotencyKey !== idempotencyKey)]);
}
