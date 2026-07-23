import { messageService } from '../services/api';
import {
  createQueuedItem,
  listOutgoing,
  putOutgoing,
  updateOutgoingStatus,
} from './outgoingQueue';
import { computeNextAttemptAt, isTransientSendError, shouldGiveUp } from './retryPolicy';
import { invalidateReachabilityCache, probeServerReachable } from './serverReachability';
import type { OutgoingQueueItem } from './types';

let processing = false;
let wakeTimer: ReturnType<typeof setTimeout> | null = null;

export type OutgoingLifecycleListener = (item: OutgoingQueueItem) => void;

const listeners = new Set<OutgoingLifecycleListener>();

export function subscribeOutgoingLifecycle(listener: OutgoingLifecycleListener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function emit(item: OutgoingQueueItem): void {
  listeners.forEach((l) => {
    try {
      l(item);
    } catch {
      /* ignore listener errors */
    }
  });
}

export async function enqueueOutgoingMessage(input: {
  clientMessageId: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  replyToMessageId?: string;
  sendAsChannelId?: string;
}): Promise<OutgoingQueueItem> {
  const item = createQueuedItem(input);
  await putOutgoing(item);
  emit(item);
  scheduleProcessQueue(0);
  return item;
}

export function scheduleProcessQueue(delayMs = 250): void {
  if (wakeTimer) clearTimeout(wakeTimer);
  wakeTimer = setTimeout(() => {
    void processOutgoingQueue();
  }, delayMs);
}

export async function processOutgoingQueue(): Promise<void> {
  if (processing) return;
  processing = true;
  try {
    const reachable = await probeServerReachable();
    if (!reachable) {
      scheduleProcessQueue(5000);
      return;
    }

    const items = await listOutgoing();
    const due = items
      .filter((i) => i.status === 'queued' || i.status === 'retrying' || i.status === 'sending')
      .filter((i) => i.nextAttemptAt <= Date.now())
      .sort((a, b) => a.createdAt - b.createdAt);

    for (const item of due) {
      await sendOne(item);
    }

    const upcoming = items
      .filter((i) => i.status === 'queued' || i.status === 'retrying')
      .map((i) => i.nextAttemptAt)
      .filter((t) => t > Date.now());
    if (upcoming.length) {
      scheduleProcessQueue(Math.max(250, Math.min(...upcoming) - Date.now()));
    }
  } finally {
    processing = false;
  }
}

async function sendOne(item: OutgoingQueueItem): Promise<void> {
  const sending = await updateOutgoingStatus(item.clientMessageId, {
    status: 'sending',
    attemptCount: item.attemptCount + 1,
  });
  if (sending) emit(sending);

  try {
    const result = await messageService.sendMessage({
      receiverId: item.receiverId,
      groupId: item.groupId,
      content: item.content,
      attachmentUrl: item.attachmentUrl,
      attachmentType: item.attachmentType,
      replyToMessageId: item.replyToMessageId,
      clientMessageId: item.clientMessageId,
      sendAsChannelId: item.sendAsChannelId,
    });

    const accepted = await updateOutgoingStatus(item.clientMessageId, {
      status: 'accepted',
      serverMessageId: result.id,
      lastError: undefined,
    });
    if (accepted) emit(accepted);
  } catch (error) {
    invalidateReachabilityCache();
    if (!isTransientSendError(error) || shouldGiveUp((sending?.attemptCount ?? item.attemptCount) + 0)) {
      const failed = await updateOutgoingStatus(item.clientMessageId, {
        status: 'permanently_failed',
        lastError: error instanceof Error ? error.message : 'Send failed',
      });
      if (failed) emit(failed);
      return;
    }

    const attemptCount = sending?.attemptCount ?? item.attemptCount + 1;
    const retrying = await updateOutgoingStatus(item.clientMessageId, {
      status: 'retrying',
      attemptCount,
      nextAttemptAt: computeNextAttemptAt(attemptCount),
      lastError: error instanceof Error ? error.message : 'Transient send failure',
    });
    if (retrying) emit(retrying);
  }
}

export async function retryFailedMessage(clientMessageId: string): Promise<void> {
  const item = await updateOutgoingStatus(clientMessageId, {
    status: 'queued',
    nextAttemptAt: Date.now(),
    lastError: undefined,
  });
  if (item) emit(item);
  scheduleProcessQueue(0);
}

export function startOutgoingQueueWatchers(): void {
  if (typeof window === 'undefined') return;
  window.addEventListener('online', () => {
    invalidateReachabilityCache();
    scheduleProcessQueue(0);
  });
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
      invalidateReachabilityCache();
      scheduleProcessQueue(0);
    }
  });
  scheduleProcessQueue(500);
}
