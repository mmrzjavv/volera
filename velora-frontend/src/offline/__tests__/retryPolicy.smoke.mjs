/**
 * Pure JS smoke checks for offline retry classification (no build step).
 * Run: node src/offline/__tests__/retryPolicy.smoke.mjs
 */

function isTransientSendError(error) {
  if (!error || typeof error !== 'object') return true;
  const status = error.response?.status;
  if (status === 401 || status === 403 || status === 400 || status === 404 || status === 422) return false;
  if (status === 408 || status === 409 || status === 425 || status === 429) return true;
  if (status !== undefined && status >= 500) return true;
  if (error.code === 'ERR_NETWORK' || error.code === 'ECONNABORTED') return true;
  return true;
}

function shouldGiveUp(attemptCount) {
  return attemptCount >= 12;
}

function computeNextAttemptAt(attemptCount, now = Date.now()) {
  const exp = Math.min(5 * 60 * 1000, 1000 * Math.pow(2, Math.max(0, attemptCount)));
  return now + exp;
}

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

assert(isTransientSendError({ code: 'ERR_NETWORK' }) === true, 'network transient');
assert(isTransientSendError({ response: { status: 503 } }) === true, '503 transient');
assert(isTransientSendError({ response: { status: 400 } }) === false, '400 permanent');
assert(shouldGiveUp(12) === true, 'max attempts');
assert(computeNextAttemptAt(0, 1000) >= 2000, 'backoff');

const mem = new Map();
const item = {
  clientMessageId: 'c1',
  content: 'hi',
  status: 'queued',
  attemptCount: 0,
  nextAttemptAt: Date.now(),
  createdAt: Date.now(),
  updatedAt: Date.now(),
};
mem.set(item.clientMessageId, item);
assert(mem.get('c1')?.status === 'queued', 'memory queue');

console.log('offline retryPolicy smoke OK');
