import { apiRequest, apiRequestWithBearer, getBaseUrl } from '@/lib/api';
import type { BranchDto } from '@/api/company';

const PREFIX = '/api/v1/support';

export interface SupportUserDto {
  id: string;
  companyId: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  role: string;
}

export interface SupportLoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  supportUser: SupportUserDto;
}

export interface SupportMessageSender {
  id: string;
  username?: string;
  firstName?: string;
  lastName?: string;
  email?: string | null;
  phoneNumber?: string | null;
  role?: string;
}

export interface MessageReactionDto {
  userId?: string | null;
  userName?: string | null;
  supportUserId?: string | null;
  supportUserName?: string | null;
  emoji: string;
}

export interface SupportMessage {
  id: string;
  senderId: string;
  sender?: SupportMessageSender | null;
  supportSenderId?: string | null;
  supportSender?: { id: string; firstName?: string; lastName?: string; username?: string } | null;
  targetReceiverUserId?: string | null;
  content: string;
  sentAt: string;
  attachmentUrl?: string | null;
  attachmentType?: string | null;
  replyToMessageId?: string | null;
  replyToMessage?: { id: string; content?: string; contentSnippet?: string } | null;
  messageReactions?: MessageReactionDto[] | null;
}

/** Real-time branch message from SupportHub BranchMessage event */
export interface BranchMessagePayload {
  messageId: string;
  senderId: string;
  targetReceiverUserId?: string | null;
  branchId: string;
  content: string;
  sentAt: string;
  attachmentUrl?: string | null;
  attachmentType?: string | null;
  replyToMessageId?: string | null;
  replyToMessage?: { contentSnippet?: string } | null;
  supportSenderId?: string | null;
}

export const supportApi = {
  login: (body: { username: string; password: string; companyId?: string | null }) =>
    apiRequest<SupportLoginResponse>(`${PREFIX}/login`, {
      method: 'POST',
      body: JSON.stringify(
        body.companyId
          ? { username: body.username, password: body.password, companyId: body.companyId }
          : { username: body.username, password: body.password }
      ),
    }),

  getBranches: (bearerToken: string, supportUserId: string) =>
    apiRequestWithBearer<BranchDto[]>(
      `${PREFIX}/users/${supportUserId}/branches`,
      bearerToken,
      { method: 'GET' }
    ),

  getBranchMessages: (
    bearerToken: string,
    branchId: string,
    params?: { limit?: number; before?: string }
  ) => {
    const search = new URLSearchParams();
    if (params?.limit != null) search.set('limit', String(params.limit));
    if (params?.before) search.set('before', params.before);
    const q = search.toString();
    return apiRequestWithBearer<SupportMessage[]>(
      `${PREFIX}/users/branches/${branchId}/messages${q ? `?${q}` : ''}`,
      bearerToken,
      { method: 'GET' }
    );
  },

  sendReply: (
    bearerToken: string,
    branchId: string,
    body: {
      content: string;
      targetClientUserId?: string | null;
      replyToMessageId?: string | null;
      attachmentUrl?: string | null;
      attachmentType?: string | null;
    }
  ) =>
    apiRequestWithBearer<{ messageId: string }>(
      `${PREFIX}/users/branches/${branchId}/messages`,
      bearerToken,
      {
        method: 'POST',
        body: JSON.stringify({
          content: body.content,
          targetClientUserId: body.targetClientUserId || undefined,
          replyToMessageId: body.replyToMessageId || undefined,
          attachmentUrl: body.attachmentUrl || undefined,
          attachmentType: body.attachmentType || undefined,
        }),
      }
    ),

  addReaction: (bearerToken: string, branchId: string, messageId: string, emoji: string) =>
    apiRequestWithBearer<null>(
      `${PREFIX}/users/branches/${branchId}/messages/${messageId}/reaction`,
      bearerToken,
      { method: 'POST', body: JSON.stringify({ emoji }) }
    ),

  removeReaction: (bearerToken: string, branchId: string, messageId: string) =>
    apiRequestWithBearer<null>(
      `${PREFIX}/users/branches/${branchId}/messages/${messageId}/reaction`,
      bearerToken,
      { method: 'DELETE' }
    ),

  uploadFile: (bearerToken: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return fetch(`${getBaseUrl()}/api/v1/support/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${bearerToken}` },
      body: form,
    }).then(async (r) => {
      const json = await r.json().catch(() => ({}));
      if (!r.ok) throw new Error(Array.isArray(json.message) ? json.message[0] : json.message ?? 'Upload failed');
      return json as { success: boolean; data?: { url: string } };
    });
  },
};

export function getSupportApiBase(): string {
  return getBaseUrl();
}
