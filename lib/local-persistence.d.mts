export declare const CORE_STORAGE_KEY: string;
export declare const ENTERPRISE_STORAGE_KEY: string;
export declare const COMMIT_MARKER_KEY: string;
export class PersistenceError extends Error { code: 'QUOTA'|'UNAVAILABLE'|'CORRUPT'|'VERSION_MISMATCH'|'WRITE_FAILED'; detail?: string; constructor(code: PersistenceError['code'], detail?: string, cause?: unknown); }
export function safeParse(raw: string, label?: string): unknown;
export function storageAvailable(storage: Storage): boolean;
export function classifyStorageError(error: unknown): string;
export function readJson<T>(storage: Storage, key: string, options?: { fallback?: T; validate?: (value: unknown) => boolean }): { exists: boolean; value: T; raw?: string };
export function writeJson(storage: Storage, key: string, value: unknown): string;
export function stagedWrite(storage: Storage, writes: {key:string;value:unknown}[], marker?: Record<string, unknown>): string;
export function assertCommittedSnapshot(storage: Storage): unknown;
