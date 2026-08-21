export declare const FINANCIAL_JOURNAL_KEY: string; export declare const FINANCIAL_JOURNAL_RETENTION: number;
export function readFinancialJournal(storage: Storage): any[];
export function pruneFinancialJournal(entries: any[], retention?: number): any[];
export function writeFinancialJournal(storage: Storage, entries: any[], retention?: number): void;
export function startJournal(storage: Storage, entry: any): boolean;
export function completeJournal(storage: Storage, key: string, payload?: any): void;
