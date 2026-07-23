/** Outgoing message delivery states (offline-first pipeline). */
export type OutgoingMessageStatus =
  | 'queued'
  | 'sending'
  | 'accepted'
  | 'retrying'
  | 'permanently_failed'
  | 'cancelled';

export interface OutgoingQueueItem {
  clientMessageId: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  replyToMessageId?: string;
  sendAsChannelId?: string;
  status: OutgoingMessageStatus;
  attemptCount: number;
  nextAttemptAt: number;
  lastError?: string;
  serverMessageId?: string;
  createdAt: number;
  updatedAt: number;
}

export const MAX_ATTEMPTS = 12;
export const MAX_BACKOFF_MS = 5 * 60 * 1000;
