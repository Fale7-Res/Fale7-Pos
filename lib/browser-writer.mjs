import { acquireWriterLock, assertWriter, refreshWriterLock, releaseWriterLock } from './single-writer.mjs';

const browserWriterId = `tab-${Date.now()}-${Math.random().toString(36).slice(2)}`;

export const getBrowserWriterId = () => browserWriterId;

export const ensureBrowserWriter = (storage, now = Date.now()) => {
  if (!acquireWriterLock(storage, browserWriterId, now)) {
    throw new Error('النظام مفتوح بالفعل في نافذة أخرى. هذه النافذة للقراءة فقط.');
  }
  return browserWriterId;
};

export const assertBrowserWriter = (storage, now = Date.now()) => {
  assertWriter(storage, browserWriterId, now);
};

export const refreshBrowserWriter = (storage, now = Date.now()) => {
  refreshWriterLock(storage, browserWriterId, now);
};

export const releaseBrowserWriter = storage => releaseWriterLock(storage, browserWriterId);
