export declare const BACKUP_VERSION: number; export declare const SYSTEM_EVENTS_KEY: string;
export function createBackupEnvelope(input: {core:any;enterprise:any;journalMetadata?:any;appVersion?:string;createdAt?:string}): any;
export function validateBackupEnvelope(value: unknown): any;
export function restoreBackupAtomically(storage: Storage, value: unknown): {commitId:string;requiresLogin:boolean};
