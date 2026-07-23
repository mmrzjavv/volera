/**
 * In-memory cache for chat images loaded with progress.
 * Avoids re-downloading when scrolling. Uses a simple LRU-style limit.
 */
const MAX_CACHED = 80;
const cache = new Map<string, string>();
const order: string[] = [];

export function getCachedImageBlobUrl(url: string): string | undefined {
  return cache.get(url);
}

export function setCachedImageBlobUrl(url: string, blobUrl: string): void {
  if (cache.has(url)) {
    order.splice(order.indexOf(url), 1);
  } else if (cache.size >= MAX_CACHED) {
    const oldest = order.shift();
    if (oldest) {
      const existing = cache.get(oldest);
      if (existing) window.URL.revokeObjectURL(existing);
      cache.delete(oldest);
    }
  }
  cache.set(url, blobUrl);
  order.push(url);
}
