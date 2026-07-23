import { fileService } from '../services/api';
import { extractObjectKeyFromMediaUrl, isLoopbackMediaUrl, resolveMediaUrl } from './resolveMediaUrl';

/**
 * If the media URL points at an unreachable host (loopback MinIO, bare localhost
 * without the app port, or Docker-only hostnames), re-sign through the API so
 * the link uses the current page origin (nginx → MinIO).
 * Also resolves bare object keys stored in the DB.
 */
export async function ensureReachableMediaUrl(url: string): Promise<string> {
  if (typeof window === 'undefined' || !url) return url;

  // Bare object key / relative storage path (not an absolute or data/blob URL)
  const isAbsolute =
    /^https?:\/\//i.test(url) ||
    url.startsWith('blob:') ||
    url.startsWith('data:') ||
    url.startsWith('/');
  if (!isAbsolute) {
    try {
      return await fileService.getDownloadUrl(url);
    } catch {
      return url;
    }
  }

  const pageOrigin = window.location.origin;
  let needsResign = isLoopbackMediaUrl(url);

  try {
    const parsed = new URL(url, pageOrigin);
    const page = new URL(pageOrigin);
    // Signed as http://localhost/... (no port) while app is on :18262
    if (
      (parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1') &&
      parsed.port !== page.port
    ) {
      needsResign = true;
    }
    // Docker-internal hostname (minio) is not resolvable in the browser
    if (parsed.hostname === 'minio') {
      needsResign = true;
    }
    // Same LAN host but wrong port (e.g. :80 vs :18262)
    if (parsed.hostname === page.hostname && parsed.port !== page.port) {
      needsResign = true;
    }
  } catch {
    /* ignore */
  }

  if (!needsResign) return url;

  const key = extractObjectKeyFromMediaUrl(url);
  if (!key) {
    // Bare relative rewrite for unsigned loopback paths
    return resolveMediaUrl(url);
  }

  try {
    return await fileService.getDownloadUrl(key);
  } catch {
    return resolveMediaUrl(url);
  }
}
