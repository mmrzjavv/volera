import { getApiBase } from '../services/api';

let lastReachable = true;
let lastCheckedAt = 0;
const CACHE_MS = 5000;

/** Probe domestic API /health — not just navigator.onLine. */
export async function probeServerReachable(timeoutMs = 4000): Promise<boolean> {
  const now = Date.now();
  if (now - lastCheckedAt < CACHE_MS) return lastReachable;

  const base = getApiBase() || (typeof window !== 'undefined' ? window.location.origin : '');
  if (!base) {
    lastReachable = false;
    lastCheckedAt = now;
    return false;
  }

  try {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);
    const res = await fetch(`${base}/health`, { signal: controller.signal, cache: 'no-store' });
    clearTimeout(timer);
    lastReachable = res.ok;
  } catch {
    lastReachable = false;
  }
  lastCheckedAt = now;
  return lastReachable;
}

export function getCachedServerReachable(): boolean {
  return lastReachable;
}

export function invalidateReachabilityCache(): void {
  lastCheckedAt = 0;
}
