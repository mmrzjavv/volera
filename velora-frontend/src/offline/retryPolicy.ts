import { MAX_ATTEMPTS, MAX_BACKOFF_MS } from './types';

/** Classify HTTP/network errors for retry vs permanent failure. */
export function isTransientSendError(error: unknown): boolean {
  if (!error || typeof error !== 'object') return true;
  const anyErr = error as { response?: { status?: number }; code?: string; message?: string };
  const status = anyErr.response?.status;
  if (status === 401 || status === 403 || status === 400 || status === 404 || status === 422) {
    return false;
  }
  if (status === 408 || status === 409 || status === 425 || status === 429) return true;
  if (status !== undefined && status >= 500) return true;
  if (anyErr.code === 'ERR_NETWORK' || anyErr.code === 'ECONNABORTED') return true;
  return true;
}

export function computeNextAttemptAt(attemptCount: number, now = Date.now()): number {
  const exp = Math.min(MAX_BACKOFF_MS, 1000 * Math.pow(2, Math.max(0, attemptCount)));
  const jitter = Math.floor(Math.random() * 500);
  return now + exp + jitter;
}

export function shouldGiveUp(attemptCount: number): boolean {
  return attemptCount >= MAX_ATTEMPTS;
}
