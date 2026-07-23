import axios from 'axios';
import { useToastStore } from '../store/useToastStore';
import type { ApiResponse, AuthResponse, LoginRequest, RegisterRequest, User, Message, UpdateProfileRequest, RecentChat, SessionInfo, StoryRing, Story, StoryViewer, CreateStoryItemPayload, Group, ChannelDetails, ChannelAnalytics, SuggestedPost, ChannelMember } from '../types';

// Read base API URL from environment; fall back to same-origin `/api/v1` (Vite proxy) when empty.
const API_VERSION = 'v1';
const VERSION_PATH = `/api/${API_VERSION}`;
const rawEnvBase = (import.meta.env.VITE_API_URL ?? '').trim();
const trimmedEnvBase = rawEnvBase.replace(/\/+$/, '');
const hasVersionSuffix = Boolean(
  trimmedEnvBase &&
    trimmedEnvBase.toLowerCase().endsWith(VERSION_PATH.toLowerCase()),
);
const resolvedBase = hasVersionSuffix
  ? trimmedEnvBase.slice(0, -VERSION_PATH.length)
  : trimmedEnvBase;
const API_BASE = resolvedBase || '';
const API_URL = hasVersionSuffix
  ? trimmedEnvBase
  : API_BASE
  ? `${API_BASE}${VERSION_PATH}`
  : VERSION_PATH;
console.log('API_URL configured to:', API_URL);


export const getApiBase = () => API_BASE;

/** Prefer API envelope message over generic axios status text. */
export function getApiErrorMessage(error: unknown, fallback = 'Request failed'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse | undefined;
    const raw = data?.message;
    if (Array.isArray(raw) && raw.length) return raw.filter(Boolean).join(' ');
    if (typeof raw === 'string' && raw.trim()) return raw;
    if (error.message) return error.message;
  }
  if (error instanceof Error && error.message) return error.message;
  return fallback;
}

/** Report a client-side error to the backend (no interceptors, no toasts). */
export async function reportError(payload: {
  message: string;
  stack?: string | null;
  url?: string;
  userAgent?: string;
  componentStack?: string | null;
  category?: string;
}): Promise<void> {
  const url = `${API_URL}/errors`;
  const token = localStorage.getItem('token');
  try {
    await axios.post(
      url,
      {
        message: payload.message,
        stackTrace: payload.stack ?? undefined,
        url: payload.url ?? (typeof window !== 'undefined' ? window.location.href : undefined),
        userAgent: payload.userAgent ?? (typeof navigator !== 'undefined' ? navigator.userAgent : undefined),
        componentStack: payload.componentStack ?? undefined,
        category: payload.category ?? 'React',
      },
      {
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        timeout: 5000,
      }
    );
  } catch (e) {
    console.warn('Failed to report error to server:', e);
  }
}

const api = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        // Handle 401 Unauthorized (Token Expired)
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            try {
                const refreshToken = localStorage.getItem('refreshToken');
                const token = localStorage.getItem('token');

                if (refreshToken && token) {
                    const response = await axios.post<ApiResponse<AuthResponse>>(`${API_URL}/Auth/refresh-token`, {
                        accessToken: token,
                        refreshToken: refreshToken
                    });
                    const payload = response.data?.data;
                    if (payload?.token) {
                        localStorage.setItem('token', payload.token);
                        localStorage.setItem('refreshToken', payload.refreshToken);

                        // Update default headers
                        api.defaults.headers.common['Authorization'] = `Bearer ${payload.token}`;
                        originalRequest.headers['Authorization'] = `Bearer ${payload.token}`;

                        return api(originalRequest);
                    }
                }
            } catch (refreshError) {
                // If refresh fails, logout
                authService.logout();
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }

        const addToast = useToastStore.getState().addToast;
        const data = error.response?.data as ApiResponse | undefined;
        const rawMessage = data?.message;
        const message = Array.isArray(rawMessage) ? rawMessage.join(' ') : (rawMessage ?? error.message ?? 'An unexpected error occurred');
        
        // Don't show toast for 401 (handled above or logout)
        if (error.response?.status !== 401) {
            addToast(message, 'error');
        }
        
        return Promise.reject(error);
    }
);


export const authService = {
  login: async (data: LoginRequest) => {
    const response = await api.post<ApiResponse<AuthResponse>>('/Auth/login', data);
    const payload = response.data?.data;
    if (payload?.token) {
        localStorage.setItem('token', payload.token);
        localStorage.setItem('refreshToken', payload.refreshToken);
        localStorage.setItem('user', JSON.stringify(payload.user));
    }
    return payload!;
  },
  register: async (data: RegisterRequest) => {
    const response = await api.post<ApiResponse<{ userId: string }>>('/Auth/register', data);
    return response.data?.data ?? response.data as unknown as { userId: string };
  },
  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },
};

export const userService = {
  getUsers: async (page: number = 1, pageSize: number = 20, searchTerm?: string) => {
    const response = await api.get<ApiResponse<{ items: User[]; totalCount: number; page: number; pageSize: number }>>('/User', {
        params: { page, pageSize, term: searchTerm }
    });
    return response.data?.data ?? { items: [], totalCount: 0, page, pageSize };
  },
  getProfile: async () => {
      const response = await api.get<ApiResponse<User>>('/User/profile');
      return response.data?.data!;
  },
  getPublicProfile: async (userId: string) => {
      const response = await api.get<ApiResponse<User>>(`/User/${userId}/profile`);
      return response.data?.data!;
  },
  getMessageLengthLimit: async () => {
      const response = await api.get<ApiResponse<{ limit: number }>>('/User/message-length-limit', {
        // Treat missing endpoint as optional; avoid throwing on 404.
        validateStatus: (status) => status < 500,
      });
      if (response.status === 404) return 2000;
      return response.data?.data?.limit ?? 2000; // Default to 2000 if not found
  },
  updateProfile: async (data: UpdateProfileRequest) => {
      await api.put('/User/profile', data);
  },
  uploadProfilePicture: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      const response = await api.post<ApiResponse<{ url: string; objectKey?: string }>>('/User/upload-profile-picture', formData, {
          headers: {
              'Content-Type': 'multipart/form-data',
          },
      });
      const data = response.data?.data;
      // Persist object key when available; use display URL only as fallback.
      return data?.objectKey || data?.url || '';
  },
  uploadProfilePictureWithPreview: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      const response = await api.post<ApiResponse<{ url: string; objectKey?: string }>>('/User/upload-profile-picture', formData, {
          headers: {
              'Content-Type': 'multipart/form-data',
          },
      });
      const data = response.data?.data;
      return {
        objectKey: data?.objectKey || '',
        previewUrl: data?.url || '',
        persistValue: data?.objectKey || data?.url || '',
      };
  },
  changePassword: async (data: any) => {
      await api.put('/User/change-password', data);
  }
};

export const sessionService = {
  getMySessions: async (): Promise<SessionInfo[]> => {
    const response = await api.get<ApiResponse<SessionInfo[]>>('/Session');
    return response.data?.data ?? [];
  },
  revokeSession: async (sessionId: string): Promise<void> => {
    await api.delete(`/Session/${sessionId}`);
  },
};

export const messageService = {
  getRecentChats: async () => {
      const response = await api.get<ApiResponse<RecentChat[]>>('/Message/recent');
      return response.data?.data ?? [];
  },
  getConversation: async (userId: string, limit: number = 20, before?: string) => {
    const params: any = { limit };
    if (before) params.before = before;
    const response = await api.get<ApiResponse<Message[]>>(`/Message/${userId}`, { params });
    return response.data?.data ?? [];
  },
  sendMessage: async (payload: {
    receiverId?: string;
    groupId?: string;
    content: string;
    attachmentUrl?: string;
    attachmentType?: string;
    replyToMessageId?: string;
    clientMessageId?: string;
    sendAsChannelId?: string;
  }) => {
    const response = await api.post<ApiResponse<{ id: string; clientMessageId?: string }>>('/Message', payload);
    const data = response.data?.data;
    if (!data?.id) throw new Error(response.data?.message?.toString() || 'Send failed');
    return data;
  },
  syncMessages: async (params: {
    peerUserId?: string;
    groupId?: string;
    afterSentAt?: string;
    afterId?: string;
    limit?: number;
  }) => {
    const response = await api.get<ApiResponse<{
      messages: Message[];
      nextAfterSentAt?: string;
      nextAfterId?: string;
      hasMore: boolean;
    }>>('/Message/sync', { params });
    return response.data?.data ?? { messages: [], hasMore: false };
  },
  editMessage: async (messageId: string, content: string) => {
    await api.patch(`/Message/${messageId}`, { content });
  },
  deleteMessage: async (messageId: string) => {
    await api.delete(`/Message/${messageId}`);
  },
  getTotalCount: async () => {
      const response = await api.get<ApiResponse<{ count: number }>>('/Message/count');
      return response.data?.data ?? { count: 0 };
  },
  getUnreadCounts: async () => {
      const response = await api.get<ApiResponse<{ senderId: string; count: number }[]>>('/Message/unread');
      return response.data?.data ?? [];
  },
  markAsRead: async (senderId: string) => {
      await api.post(`/Message/mark-read/${senderId}`);
  },
  getSavedMessages: async (page: number = 1, pageSize: number = 20) => {
      const response = await api.get<ApiResponse<{ items: { id: string; message: Message; savedAt: string }[]; totalCount: number; page: number; pageSize: number }>>('/Message/saved', {
          params: { page, pageSize }
      });
      return response.data?.data ?? { items: [], totalCount: 0, page, pageSize };
  },
  addOrUpdateReaction: async (messageId: string, emoji: string) => {
      await api.post(`/Message/${messageId}/reaction`, { emoji });
  },
  removeReaction: async (messageId: string) => {
      await api.delete(`/Message/${messageId}/reaction`);
  },
  saveMessage: async (messageId: string) => {
      await api.post(`/Message/${messageId}/save`);
  },
  unsaveMessage: async (messageId: string) => {
      await api.delete(`/Message/${messageId}/save`);
  },
  forwardMessage: async (messageId: string, target: { receiverId?: string; groupId?: string }) => {
      await api.post(`/Message/${messageId}/forward`, target);
  },
  pinMessage: async (messageId: string) => {
      await api.post(`/Message/${messageId}/pin`);
  },
  unpinMessage: async (messageId: string) => {
      await api.delete(`/Message/${messageId}/pin`);
  },
  /** Remove chat from recent list. Direct chats: hide. Group chats: leave group. Call after undo timeout. */
  removeChatFromRecent: async (params: { userId?: string; groupId?: string }) => {
      const searchParams = new URLSearchParams();
      if (params.userId) searchParams.set('userId', params.userId);
      if (params.groupId) searchParams.set('groupId', params.groupId);
      await api.delete(`/Message/chat?${searchParams.toString()}`);
  }
};

export const groupService = {
    createGroup: async (data: { name: string; memberIds: string[] }) => {
        const response = await api.post<ApiResponse<{ groupId: string }>>('/Group', data);
        return response.data?.data ?? response.data as unknown as { groupId: string };
    },
    getMyGroups: async () => {
        const response = await api.get<ApiResponse<unknown[]>>('/Group');
        return response.data?.data ?? [];
    },
    getGroupDetails: async (groupId: string) => {
        const response = await api.get<ApiResponse<any>>(`/Group/${groupId}/details`);
        return response.data?.data ?? {};
    },
    getGroupMessages: async (groupId: string, limit: number = 20, before?: string) => {
        const params: any = { limit };
        if (before) params.before = before;
        const response = await api.get<ApiResponse<Message[]>>(`/Group/${groupId}/messages`, { params });
        return response.data?.data ?? [];
    },
    addMember: async (groupId: string, memberId: string) => {
        await api.post(`/Group/${groupId}/members`, { memberId });
    },
    removeMember: async (groupId: string, memberId: string) => {
        await api.delete(`/Group/${groupId}/members/${memberId}`);
    },
    leaveGroup: async (groupId: string) => {
        await api.post(`/Group/${groupId}/leave`, {});
    },
    changeAdmin: async (groupId: string, newAdminId: string) => {
        await api.post(`/Group/${groupId}/change-admin`, { newAdminId });
    },
    updateProfile: async (groupId: string, data: { name?: string; description?: string | null; profilePictureUrl?: string | null }) => {
        await api.put(`/Group/${groupId}/profile`, data);
    },
    deleteGroup: async (groupId: string) => {
        await api.delete(`/Group/${groupId}`);
    },
    generateInviteLink: async (groupId: string) => {
        const response = await api.post<ApiResponse<{ inviteCode: string }>>(`/Group/${groupId}/invite-link`, {});
        return response.data?.data ?? { inviteCode: '' };
    },
    /** Preview group by invite code (public, no auth required). */
    getGroupByInviteCode: async (inviteCode: string) => {
        const response = await api.get<ApiResponse<{ id: string; name: string; inviteCode?: string }>>(`/Group/invite/${encodeURIComponent(inviteCode)}`);
        return response.data?.data ?? null;
    },
    joinByInvite: async (inviteCode: string) => {
        await api.post(`/Group/join-by-invite/${inviteCode}`, {});
    }
};

export const channelService = {
  createChannel: async (data: { name: string; description?: string; isPublic?: boolean; publicUsername?: string }) => {
    const response = await api.post<ApiResponse<{ channelId: string; inviteCode?: string }>>('/Channel', data);
    return response.data?.data ?? { channelId: '' };
  },
  getMyChannels: async () => {
    const response = await api.get<ApiResponse<Group[]>>('/Channel/mine');
    return response.data?.data ?? [];
  },
  search: async (q: string) => {
    const response = await api.get<ApiResponse<unknown[]>>('/Channel/search', { params: { q } });
    return response.data?.data ?? [];
  },
  getChannelDetails: async (id: string) => {
    const response = await api.get<ApiResponse<ChannelDetails>>(`/Channel/${id}`);
    return response.data?.data as ChannelDetails;
  },
  getByUsername: async (username: string) => {
    const response = await api.get<ApiResponse<ChannelDetails>>(`/Channel/u/${encodeURIComponent(username)}`);
    return response.data?.data as ChannelDetails;
  },
  subscribe: async (id: string) => {
    await api.post(`/Channel/${id}/subscribe`);
  },
  leave: async (id: string) => {
    await api.delete(`/Channel/${id}/leave`);
  },
  generateInviteLink: async (id: string) => {
    const response = await api.post<ApiResponse<{ inviteCode: string }>>(`/Channel/${id}/invite-link`);
    return response.data?.data?.inviteCode ?? '';
  },
  joinByInvite: async (code: string) => {
    const response = await api.post<ApiResponse<{ channelId: string }>>(`/Channel/join/${encodeURIComponent(code)}`);
    return response.data?.data?.channelId ?? '';
  },
  getInvitePreview: async (code: string) => {
    const response = await api.get<ApiResponse<ChannelDetails>>(`/Channel/invite/${encodeURIComponent(code)}`);
    return response.data?.data as ChannelDetails;
  },
  updateProfile: async (id: string, data: { name: string; description?: string; profilePictureUrl?: string }) => {
    await api.put(`/Channel/${id}/profile`, data);
  },
  setVisibility: async (id: string, data: { isPublic: boolean; publicUsername?: string }) => {
    await api.put(`/Channel/${id}/visibility`, data);
  },
  toggleSignatures: async (id: string, enabled: boolean) => {
    await api.put(`/Channel/${id}/signatures`, { enabled });
  },
  recordViews: async (id: string, messageIds: string[]) => {
    await api.post(`/Channel/${id}/views`, { messageIds });
  },
  getAnalytics: async (id: string) => {
    const response = await api.get<ApiResponse<ChannelAnalytics>>(`/Channel/${id}/analytics`);
    return response.data?.data as ChannelAnalytics;
  },
  linkDiscussion: async (id: string, discussionGroupId: string) => {
    await api.post(`/Channel/${id}/discussion`, { discussionGroupId });
  },
  unlinkDiscussion: async (id: string) => {
    await api.delete(`/Channel/${id}/discussion`);
  },
  suggestPost: async (id: string, data: { content: string; attachmentUrl?: string; attachmentType?: string }) => {
    const response = await api.post<ApiResponse<{ suggestionId: string }>>(`/Channel/${id}/suggestions`, data);
    return response.data?.data?.suggestionId ?? '';
  },
  listSuggestions: async (id: string, status?: string) => {
    const response = await api.get<ApiResponse<SuggestedPost[]>>(`/Channel/${id}/suggestions`, { params: { status } });
    return response.data?.data ?? [];
  },
  acceptSuggestion: async (suggestionId: string) => {
    const response = await api.post<ApiResponse<{ messageId: string }>>(`/Channel/suggestions/${suggestionId}/accept`);
    return response.data?.data?.messageId ?? '';
  },
  rejectSuggestion: async (suggestionId: string, adminNote?: string) => {
    await api.post(`/Channel/suggestions/${suggestionId}/reject`, { adminNote });
  },
  setAdmin: async (id: string, data: { userId: string; canPost?: boolean; canEditMessages?: boolean; canDeleteMessages?: boolean; canManageSubscribers?: boolean; canChangeInfo?: boolean; canAddAdmins?: boolean }) => {
    await api.post(`/Channel/${id}/admins`, data);
  },
  removeAdmin: async (id: string, targetUserId: string) => {
    await api.delete(`/Channel/${id}/admins/${targetUserId}`);
  },
  getSubscribers: async (id: string, page = 1) => {
    const response = await api.get<ApiResponse<ChannelMember[]>>(`/Channel/${id}/subscribers`, { params: { page } });
    return response.data?.data ?? [];
  },
};

export const fileService = {
  upload: async (file: File, onProgress?: (progress: number) => void) => {
    const formData = new FormData();
    formData.append('file', file);
    try {
      const response = await api.post<ApiResponse<{ url: string; publicUrl?: string; objectKey?: string }>>('/Upload', formData, {
        onUploadProgress: (progressEvent) => {
          if (progressEvent.total) {
            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            onProgress?.(percentCompleted);
          }
        },
      });
      const data = response.data?.data;
      return {
        url: data?.url || data?.publicUrl || '',
        publicUrl: data?.publicUrl || data?.url || '',
        // Prefer durable object key for message AttachmentUrl (server resolves to presigned URL on read).
        objectKey: data?.objectKey || '',
        attachmentRef: data?.objectKey || data?.url || data?.publicUrl || '',
      };
    } catch (error) {
      throw new Error(getApiErrorMessage(error, 'Upload failed'));
    }
  },
  initiateUpload: async (fileName: string, contentType: string) => {
      const response = await api.post<ApiResponse<{
        uploadUrl: string;
        publicUrl?: string;
        downloadUrl?: string;
        objectKey?: string;
      }>>('/Upload/initiate', {
          fileName,
          contentType
      });
      const data = response.data?.data;
      return {
        uploadUrl: data?.uploadUrl ?? '',
        publicUrl: data?.publicUrl || data?.objectKey || data?.downloadUrl || '',
        downloadUrl: data?.downloadUrl || '',
        objectKey: data?.objectKey || '',
        attachmentRef: data?.objectKey || data?.publicUrl || data?.downloadUrl || '',
      };
  },
  uploadToPresignedUrl: async (uploadUrl: string, file: Blob, contentType: string, onProgress?: (progress: number) => void) => {
      // Direct PUT to MinIO — do not attach API Authorization header.
      await axios.put(uploadUrl, file, {
          headers: {
              'Content-Type': contentType
          },
          transformRequest: [(data) => data],
          onUploadProgress: (progressEvent) => {
            if (progressEvent.total) {
              const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
              onProgress?.(percentCompleted);
            }
          },
      });
  },
  downloadFile: async (
      url: string,
      onProgress?: (progress: number) => void,
      cancelToken?: any,
      totalBytes?: number
  ) => {
      const response = await axios.get(url, {
          responseType: 'blob',
          cancelToken,
          onDownloadProgress: (progressEvent) => {
              const total = progressEvent.total ?? totalBytes ?? 0;
              if (total > 0) {
                  const percentCompleted = Math.min(99, Math.round((progressEvent.loaded * 100) / total));
                  onProgress?.(percentCompleted);
              } else if (progressEvent.loaded > 0) {
                  // Fallback when Content-Length is missing (chunked transfer): show indeterminate progress
                  onProgress?.(Math.min(90, Math.round(50 * Math.log10(1 + progressEvent.loaded / 1024))));
              }
          }
      });
      return response.data;
  },
  /** Re-sign a private object for the current browser origin (fixes localhost MinIO on LAN phones). */
  getDownloadUrl: async (objectKey: string) => {
      const response = await api.get<ApiResponse<{ url: string }>>('/Upload/download-url', {
          params: { objectKey },
      });
      const url = response.data?.data?.url;
      if (!url) throw new Error(response.data?.message?.toString() || 'Failed to resolve download URL');
      return url;
  },
  checkFileSize: async (url: string) => {
      try {
        const response = await axios.head(url);
        return parseInt(response.headers['content-length'] || '0', 10);
      } catch (error) {
        console.error("Failed to check file size", error);
        return 0;
      }
  }
};

export const storyService = {
  getFeed: async () => {
    const response = await api.get<ApiResponse<StoryRing[]>>('/Story/feed');
    return response.data?.data ?? [];
  },
  getUserStories: async (userId: string) => {
    const response = await api.get<ApiResponse<Story[]>>(`/Story/user/${userId}`);
    return response.data?.data ?? [];
  },
  create: async (items: CreateStoryItemPayload[]) => {
    const response = await api.post<ApiResponse<{ storyId: string }>>('/Story', { items });
    return response.data?.data;
  },
  markViewed: async (storyId: string) => {
    await api.post(`/Story/${storyId}/view`);
  },
  getViewers: async (storyId: string) => {
    const response = await api.get<ApiResponse<StoryViewer[]>>(`/Story/${storyId}/viewers`);
    return response.data?.data ?? [];
  },
  deleteStory: async (storyId: string) => {
    await api.delete(`/Story/${storyId}`);
  },
  deleteItem: async (itemId: string) => {
    await api.delete(`/Story/items/${itemId}`);
  },
  reply: async (itemId: string, content: string) => {
    const response = await api.post<ApiResponse<{ messageId: string }>>(`/Story/items/${itemId}/reply`, { content });
    return response.data?.data;
  },
};

export const systemMessageService = {
  create: async (data: { title: string; content: string; expiresAt?: string }) => {
    const response = await api.post<ApiResponse<{ id: string }>>('/system-messages', data);
    return response.data?.data ?? response.data as unknown as { id: string };
  },
  getActive: async () => {
    const response = await api.get<ApiResponse<unknown[]>>('/system-messages/active');
    return response.data?.data ?? [];
  },
  update: async (id: string, data: { title: string; content: string; expiresAt?: string }) => {
    await api.put(`/system-messages/${id}`, data);
  },
  delete: async (id: string) => {
    await api.delete(`/system-messages/${id}`);
  },
  markAsRead: async (id: string) => {
    await api.post(`/system-messages/${id}/read`);
  }
};

export default api;
