import type { OutgoingQueueItem, OutgoingMessageStatus } from './types';

const DB_NAME = 'velora-offline';
const STORE = 'outgoing';
const DB_VERSION = 1;

type MemoryStore = Map<string, OutgoingQueueItem>;

/** In-memory fallback used when IndexedDB is unavailable (SSR/tests). */
const memoryStore: MemoryStore = new Map();

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    if (typeof indexedDB === 'undefined') {
      reject(new Error('IndexedDB unavailable'));
      return;
    }
    const req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains(STORE)) {
        db.createObjectStore(STORE, { keyPath: 'clientMessageId' });
      }
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error ?? new Error('IndexedDB open failed'));
  });
}

async function withStore<T>(
  mode: IDBTransactionMode,
  fn: (store: IDBObjectStore) => IDBRequest<T> | void
): Promise<T | void> {
  try {
    const db = await openDb();
    return await new Promise<T | void>((resolve, reject) => {
      const tx = db.transaction(STORE, mode);
      const store = tx.objectStore(STORE);
      const result = fn(store);
      if (result) {
        result.onsuccess = () => resolve(result.result);
        result.onerror = () => reject(result.error);
      } else {
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
      }
    });
  } catch {
    return undefined;
  }
}

export async function putOutgoing(item: OutgoingQueueItem): Promise<void> {
  memoryStore.set(item.clientMessageId, item);
  await withStore('readwrite', (store) => {
    store.put(item);
  });
}

export async function getOutgoing(clientMessageId: string): Promise<OutgoingQueueItem | undefined> {
  const fromIdb = await withStore<OutgoingQueueItem>('readonly', (store) => store.get(clientMessageId));
  if (fromIdb) return fromIdb;
  return memoryStore.get(clientMessageId);
}

export async function listOutgoing(): Promise<OutgoingQueueItem[]> {
  const fromIdb = await withStore<OutgoingQueueItem[]>('readonly', (store) => store.getAll());
  if (fromIdb && fromIdb.length) return fromIdb;
  return Array.from(memoryStore.values());
}

export async function updateOutgoingStatus(
  clientMessageId: string,
  patch: Partial<OutgoingQueueItem>
): Promise<OutgoingQueueItem | undefined> {
  const current = await getOutgoing(clientMessageId);
  if (!current) return undefined;
  const next = { ...current, ...patch, updatedAt: Date.now() };
  await putOutgoing(next);
  return next;
}

export async function removeOutgoing(clientMessageId: string): Promise<void> {
  memoryStore.delete(clientMessageId);
  await withStore('readwrite', (store) => {
    store.delete(clientMessageId);
  });
}

/** Test helper: clear memory store. */
export function __resetMemoryOutgoingStore(): void {
  memoryStore.clear();
}

export function createQueuedItem(input: {
  clientMessageId: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  replyToMessageId?: string;
  sendAsChannelId?: string;
}): OutgoingQueueItem {
  const now = Date.now();
  return {
    ...input,
    status: 'queued' as OutgoingMessageStatus,
    attemptCount: 0,
    nextAttemptAt: now,
    createdAt: now,
    updatedAt: now,
  };
}
