export declare const WRITER_LOCK_KEY: string;
export function acquireWriterLock(storage: Storage, id: string, now?: number, ttl?: number): boolean;
export function refreshWriterLock(storage: Storage, id: string, now?: number, ttl?: number): void;
export function releaseWriterLock(storage: Storage, id: string): void;
export function assertWriter(storage: Storage, id: string, now?: number): void;
