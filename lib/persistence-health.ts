'use client';
import { useSyncExternalStore } from 'react';
export type PersistenceIssue = { code:string; message:string; rawKey?:string } | null;
let issue: PersistenceIssue = null; const listeners = new Set<() => void>();
export function reportPersistenceIssue(next: PersistenceIssue) { issue=next; listeners.forEach(fn=>fn()); }
export function usePersistenceIssue() { return useSyncExternalStore(cb=>{listeners.add(cb);return()=>listeners.delete(cb);},()=>issue,()=>null); }
