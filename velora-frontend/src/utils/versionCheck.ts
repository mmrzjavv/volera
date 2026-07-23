import { getApiBase } from '../services/api';

declare const __APP_VERSION__: string;

/** Set to true to enable "new version available" check and banner. */
const UPDATE_CHECK_ENABLED = false;

const CLIENT_VERSION = typeof __APP_VERSION__ !== 'undefined' ? __APP_VERSION__ : '0.0.0';
/** Persist in localStorage so dismiss survives new tabs/sessions and we don't show again and again. */
const STORAGE_KEY_DISMISSED = 'app_update_dismissed_version';

/** Compare two semver-like strings (e.g. "1.0.0" vs "1.0.1"). Returns true if server is "newer". */
function isNewerVersion(server: string, client: string): boolean {
  const toParts = (v: string) => v.split('.').map((n) => parseInt(n, 10) || 0);
  const s = toParts(server);
  const c = toParts(client);
  for (let i = 0; i < Math.max(s.length, c.length); i++) {
    const a = s[i] ?? 0;
    const b = c[i] ?? 0;
    if (a > b) return true;
    if (a < b) return false;
  }
  return false;
}

/** Fetch the app version from the backend (no auth required). Call once on main page. */
export async function getServerVersion(): Promise<string> {
  const base = getApiBase();
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const url = `${base || origin}/version`;
  const res = await fetch(url, { cache: 'no-store' });
  if (!res.ok) throw new Error('Version check failed');
  const data = await res.json();
  return data?.version ?? '0.0.0';
}

export type UpdateStatus = { updateAvailable: boolean; serverVersion: string };

/** Single promise per page load – no matter how many times getUpdateStatus is called, we only fetch once. */
let cachedStatusPromise: Promise<UpdateStatus> | null = null;

/**
 * Call once on main page. Fetches /version, compares with client.
 * Only returns updateAvailable true when server is newer AND we haven't already shown/dismissed for this server version (localStorage).
 * As soon as we decide to show, we persist that so we never show again for this version until they reload.
 */
export async function getUpdateStatus(): Promise<UpdateStatus> {
  if (!UPDATE_CHECK_ENABLED || import.meta.env.DEV) return { updateAvailable: false, serverVersion: '' };
  if (cachedStatusPromise) return cachedStatusPromise;
  cachedStatusPromise = (async (): Promise<UpdateStatus> => {
    try {
      const serverVersion = await getServerVersion();
      if (!isNewerVersion(serverVersion, CLIENT_VERSION)) {
        return { updateAvailable: false, serverVersion };
      }
      const alreadyShownOrDismissed = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY_DISMISSED) : null;
      if (alreadyShownOrDismissed === serverVersion) {
        return { updateAvailable: false, serverVersion };
      }
      // Persist immediately so we never show again for this version (even if user never clicks Dismiss)
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(STORAGE_KEY_DISMISSED, serverVersion);
      }
      return { updateAvailable: true, serverVersion };
    } catch {
      return { updateAvailable: false, serverVersion: '' };
    }
  })();
  return cachedStatusPromise;
}

/** Hide banner for this server version; stored in localStorage so it doesn't show again and again. */
export function dismissUpdateBanner(serverVersion: string): void {
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem(STORAGE_KEY_DISMISSED, serverVersion);
  }
}

/** Clear caches and reload. Clears dismissed version so after reload we re-evaluate. */
export async function clearCacheAndReload(): Promise<void> {
  if (typeof localStorage !== 'undefined') {
    localStorage.removeItem(STORAGE_KEY_DISMISSED);
  }
  if ('caches' in window) {
    const names = await caches.keys();
    await Promise.all(names.map((name) => caches.delete(name)));
  }
  if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
    navigator.serviceWorker.ready.then((reg) => {
      reg.waiting?.postMessage({ type: 'SKIP_WAITING' });
    });
  }
  window.location.reload();
}

export { CLIENT_VERSION };
