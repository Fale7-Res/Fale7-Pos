export function getBrowserWriterId(): string;
export function ensureBrowserWriter(storage: Storage, now?: number): string;
export function assertBrowserWriter(storage: Storage, now?: number): void;
export function refreshBrowserWriter(storage: Storage, now?: number): void;
export function releaseBrowserWriter(storage: Storage): void;
