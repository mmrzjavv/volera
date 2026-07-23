import api from './api';
import type { ApiResponse } from '../types';

const ADMIN_BASE = '/admin';

export interface AdminUserListDto {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  role: string;
  isDisabled: boolean;
  suspendedUntil?: string | null;
  createdAt: string;
  messageCount: number;
  chatCount: number;
  savedMessagesCount: number;
  storageUsedBytes: number;
}

export interface AdminUserDetailDto extends AdminUserListDto {
  phoneNumber: string;
  email?: string | null;
  bio?: string | null;
  profilePicture?: string | null;
  updatedAt: string;
  limitOverrides: AdminLimitOverrideDto[];
}

export interface AdminSessionDto {
  id: string;
  userId: string;
  deviceType: string;
  browser: string;
  os: string;
  location: string;
  loginAt: string;
  lastActivityAt: string;
  appVersion: string;
  isRevoked: boolean;
}

export interface AdminLimitOverrideDto {
  limitKey: string;
  value: number;
}

export interface AdminChatDto {
  conversationKey: string;
  type: string;
  userId1?: string;
  userId2?: string;
  userName1?: string;
  userName2?: string;
  groupId?: string;
  groupName?: string;
  groupProfilePictureUrl?: string | null;
  lastMessageContent?: string;
  lastMessageAt?: string;
}

export interface AdminMessageDto {
  id: string;
  senderId: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  sentAt: string;
  isEdited: boolean;
  deletedAt?: string | null;
  senderUsername?: string;
}

export interface AdminAuditLogDto {
  id: string;
  adminUserId: string;
  adminUsername?: string;
  action: string;
  resourceType: string;
  resourceId?: string | null;
  details?: string | null;
  createdAt: string;
}

export interface SystemStatsDto {
  totalUsers: number;
  totalMessages: number;
  totalGroups: number;
  storageUsedBytes: number;
  usersOverLimit: number;
}

export interface SystemLimitDto {
  key: string;
  value: number;
  description?: string;
}

export interface AdminChatMessageDto {
  id: string;
  senderId: string;
  senderUsername: string;
  senderFirstName?: string;
  senderLastName?: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  sentAt: string;
  isEdited: boolean;
  deletedAt?: string | null;
}

export interface AdminConversationResult {
  messages: AdminChatMessageDto[];
  nextCursor?: string | null;
  hasMore: boolean;
  conversationKey: string;
  conversationTitle: string;
  type: string;
}

export interface ExtendedMonitoringStatsDto {
  totalUsers: number;
  totalMessages: number;
  totalGroups: number;
  onlineUsersCount: number;
  disabledUsersCount: number;
  suspendedUsersCount: number;
  unreadMessagesCount: number;
  newUsersLast24h: number;
  newUsersLast7d: number;
  newUsersLast30d: number;
  messagesLast24h: number;
  messagesLast7d: number;
  usersByRole: Record<string, number>;
}

export interface MessagesPerDayDto {
  date: string;
  count: number;
}

export interface MostActiveUserDto {
  userId: string;
  username: string;
  messageCount: number;
}

export interface MostActiveGroupDto {
  groupId: string;
  groupName: string;
  messageCount: number;
}

export interface TableRowCountsDto {
  counts: Record<string, number>;
}

export interface UserUsageDto {
  userId: string;
  username: string;
  messageCount: number;
  savedMessagesCount: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const adminApi = {
  // Users
  getUsers: async (params: { page?: number; pageSize?: number; searchTerm?: string; roleFilter?: string; isDisabled?: boolean; sortBy?: string; sortDesc?: boolean }) => {
    const response = await api.get<ApiResponse<PaginatedResult<AdminUserListDto>>>(`${ADMIN_BASE}/users`, { params });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },
  getUserDetail: async (id: string) => {
    const response = await api.get<ApiResponse<AdminUserDetailDto>>(`${ADMIN_BASE}/users/${id}`);
    return response.data?.data;
  },
  updateUser: async (id: string, data: { firstName: string; lastName: string; email?: string; bio?: string }) => {
    await api.put(`${ADMIN_BASE}/users/${id}`, data);
  },
  disableUser: async (id: string) => {
    await api.post(`${ADMIN_BASE}/users/${id}/disable`);
  },
  suspendUser: async (id: string, until: string) => {
    await api.post(`${ADMIN_BASE}/users/${id}/suspend`, { until });
  },
  reactivateUser: async (id: string) => {
    await api.post(`${ADMIN_BASE}/users/${id}/reactivate`);
  },
  setUserRole: async (id: string, role: string) => {
    await api.post(`${ADMIN_BASE}/users/${id}/role`, { role });
  },
  getUserSessions: async (userId: string) => {
    const response = await api.get<ApiResponse<AdminSessionDto[]>>(`${ADMIN_BASE}/users/${userId}/sessions`);
    return response.data?.data ?? [];
  },
  revokeUserSession: async (userId: string, sessionId: string) => {
    await api.delete(`${ADMIN_BASE}/users/${userId}/sessions/${sessionId}`);
  },

  // Chats
  getChats: async (params: { page?: number; pageSize?: number; searchTerm?: string; type?: string }) => {
    const response = await api.get<ApiResponse<PaginatedResult<AdminChatDto>>>(`${ADMIN_BASE}/chats`, { params });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },
  getChatByKey: async (key: string) => {
    const response = await api.get<ApiResponse<AdminChatDto>>(`${ADMIN_BASE}/chats/${encodeURIComponent(key)}`);
    return response.data?.data;
  },
  getConversationMessages: async (key: string, limit = 50, before?: string) => {
    const params: Record<string, string | number> = { limit };
    if (before) params.before = before;
    const response = await api.get<ApiResponse<AdminConversationResult>>(`${ADMIN_BASE}/chats/${encodeURIComponent(key)}/messages`, { params });
    return response.data?.data;
  },
  purgeConversation: async (key: string) => {
    const response = await api.delete<ApiResponse<{ deleted: number }>>(`${ADMIN_BASE}/chats/${encodeURIComponent(key)}`);
    return response.data?.data?.deleted ?? 0;
  },

  uploadGroupProfilePicture: async (groupId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<ApiResponse<{ url: string }>>(`${ADMIN_BASE}/groups/${groupId}/profile-picture`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data?.data?.url;
  },

  // Messages
  searchMessages: async (params: { page?: number; pageSize?: number; content?: string; senderId?: string; groupId?: string; dateFrom?: string; dateTo?: string }) => {
    const response = await api.get<ApiResponse<PaginatedResult<AdminMessageDto>>>(`${ADMIN_BASE}/messages/search`, { params });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },
  editMessage: async (id: string, content: string) => {
    await api.put(`${ADMIN_BASE}/messages/${id}`, { content });
  },
  deleteMessage: async (id: string, hardDelete = false) => {
    await api.delete(`${ADMIN_BASE}/messages/${id}`, { params: { hardDelete } });
  },

  // Limits
  getSystemLimits: async () => {
    const response = await api.get<ApiResponse<SystemLimitDto[]>>(`${ADMIN_BASE}/limits/system`);
    return response.data?.data ?? [];
  },
  setSystemLimit: async (key: string, value: number) => {
    await api.put(`${ADMIN_BASE}/limits/system`, { key, value });
  },
  getUserOverrides: async (userId: string) => {
    const response = await api.get<ApiResponse<AdminLimitOverrideDto[]>>(`${ADMIN_BASE}/limits/users/${userId}`);
    return response.data?.data ?? [];
  },
  getEffectiveLimits: async (userId: string) => {
    const response = await api.get<ApiResponse<AdminLimitOverrideDto[]>>(`${ADMIN_BASE}/limits/users/${userId}/effective`);
    return response.data?.data ?? [];
  },
  setUserOverride: async (userId: string, key: string, value: number) => {
    await api.put(`${ADMIN_BASE}/limits/users/${userId}`, { key, value });
  },
  removeUserOverride: async (userId: string, key: string) => {
    await api.delete(`${ADMIN_BASE}/limits/users/${userId}/overrides`, { params: { key } });
  },

  // Monitoring
  getStats: async () => {
    const response = await api.get<ApiResponse<SystemStatsDto>>(`${ADMIN_BASE}/monitoring/stats`);
    return response.data?.data ?? { totalUsers: 0, totalMessages: 0, totalGroups: 0, storageUsedBytes: 0, usersOverLimit: 0 };
  },
  getExtendedStats: async () => {
    const response = await api.get<ApiResponse<ExtendedMonitoringStatsDto>>(`${ADMIN_BASE}/monitoring/stats/extended`);
    return response.data?.data;
  },
  getUsersOverLimit: async (limitKey: string, page = 1, pageSize = 20) => {
    const response = await api.get<ApiResponse<PaginatedResult<AdminUserListDto>>>(`${ADMIN_BASE}/monitoring/over-limit`, { params: { limitKey, page, pageSize } });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },
  getMessagesPerDay: async (days = 30) => {
    const response = await api.get<ApiResponse<MessagesPerDayDto[]>>(`${ADMIN_BASE}/monitoring/messages-per-day`, { params: { days } });
    return response.data?.data ?? [];
  },
  getMostActiveUsers: async (page = 1, pageSize = 20) => {
    const response = await api.get<ApiResponse<PaginatedResult<MostActiveUserDto>>>(`${ADMIN_BASE}/monitoring/most-active-users`, { params: { page, pageSize } });
    return response.data?.data ?? { items: [], totalCount: 0, page, pageSize, totalPages: 0 };
  },
  getMostActiveGroups: async (page = 1, pageSize = 20) => {
    const response = await api.get<ApiResponse<PaginatedResult<MostActiveGroupDto>>>(`${ADMIN_BASE}/monitoring/most-active-groups`, { params: { page, pageSize } });
    return response.data?.data ?? { items: [], totalCount: 0, page, pageSize, totalPages: 0 };
  },
  getTableRowCounts: async () => {
    const response = await api.get<ApiResponse<TableRowCountsDto>>(`${ADMIN_BASE}/monitoring/table-counts`);
    return response.data?.data ?? { counts: {} };
  },
  getUserUsage: async (page = 1, pageSize = 20, sortBy?: string, sortDesc = true) => {
    const response = await api.get<ApiResponse<PaginatedResult<UserUsageDto>>>(`${ADMIN_BASE}/monitoring/user-usage`, { params: { page, pageSize, sortBy, sortDesc } });
    return response.data?.data ?? { items: [], totalCount: 0, page, pageSize, totalPages: 0 };
  },
  getUnreadCount: async () => {
    const response = await api.get<ApiResponse<{ count: number }>>(`${ADMIN_BASE}/monitoring/unread-count`);
    return response.data?.data?.count ?? 0;
  },

  // Audit
  getAuditLog: async (params: { page?: number; pageSize?: number; adminUserId?: string; action?: string; resourceType?: string; from?: string; to?: string }) => {
    const response = await api.get<ApiResponse<PaginatedResult<AdminAuditLogDto>>>(`${ADMIN_BASE}/audit`, { params });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  },

  // Error logs → Seq (Serilog)
  getErrorLoggingInfo: async () => {
    const response = await api.get<ApiResponse<{ provider: string; uiUrl: string; message: string }>>(`${ADMIN_BASE}/errors`);
    return response.data?.data ?? { provider: 'Seq', uiUrl: 'http://localhost:5341', message: 'Application errors are shipped to Seq via Serilog.' };
  },

  // App version (shown to users; when you bump it, "A new version is available" appears until they reload)
  getVersion: async () => {
    const response = await api.get<ApiResponse<{ version: string }>>(`${ADMIN_BASE}/version`);
    return response.data?.data?.version ?? '1.0.0';
  },
  setVersion: async (version: string) => {
    await api.put(`${ADMIN_BASE}/version`, { version: version.trim() });
  },
};

export interface ErrorLogEntryDto {
  id?: string;
  source: string;
  occurredAt: string;
  category: string;
  message: string;
  stackTrace?: string | null;
  severity?: string | null;
  requestPath?: string | null;
  requestMethod?: string | null;
  url?: string | null;
  userAgent?: string | null;
  componentStack?: string | null;
  userId?: string | null;
}

export interface ErrorLogGroupDto {
  groupKey: string;
  count: number;
  lastOccurrence: string;
  sampleMessage?: string | null;
}
