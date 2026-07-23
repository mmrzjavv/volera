/**
 * Make media URLs reachable on the device that opened the app.
 * Loopback MinIO hosts (localhost:9000) are unreachable from phones on LAN.
 * Prefer re-signing via the API when the URL looks like a private object link.
 */
export function isLoopbackMediaUrl(url: string): boolean {
  try {
    const parsed = new URL(url, typeof window !== 'undefined' ? window.location.origin : 'http://localhost');
    return (
      parsed.hostname === 'localhost' ||
      parsed.hostname === '127.0.0.1' ||
      parsed.hostname === '[::1]'
    );
  } catch {
    return false;
  }
}

/** Extract path-style object key from /{bucket}/{key}... */
export function extractObjectKeyFromMediaUrl(
  url: string,
  buckets: string[] = ['voice-call-app', 'volera-media']
): string | null {
  try {
    const parsed = new URL(url, typeof window !== 'undefined' ? window.location.origin : 'http://localhost');
    const path = decodeURIComponent(parsed.pathname.replace(/^\/+/, ''));
    for (const bucket of buckets) {
      const prefix = `${bucket}/`;
      if (path.toLowerCase().startsWith(prefix)) {
        const key = path.slice(prefix.length);
        return key && !key.includes('..') ? key : null;
      }
    }
  } catch {
    /* ignore */
  }
  return null;
}

/**
 * Best-effort same-origin rewrite for unsigned/legacy URLs.
 * Do not use alone for SigV4 presigned links — re-sign instead.
 */
export function resolveMediaUrl(url: string | null | undefined): string {
  if (!url) return '';
  if (typeof window === 'undefined') return url;
  if (!isLoopbackMediaUrl(url)) return url;

  try {
    const parsed = new URL(url, window.location.origin);
    return `${window.location.origin}${parsed.pathname}${parsed.search}${parsed.hash}`;
  } catch {
    return url;
  }
}
