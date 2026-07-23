import api from './api';
import type { ApiResponse } from '../types';
import type { InitiateCallResponse, Call, PaginatedResult, CallHistoryParams, IceServersResponse, IceServerConfig } from '../types/call';

export const callService = {
  initiate: async (receiverId: string, isVideo: boolean = false) => {
    const response = await api.post<ApiResponse<InitiateCallResponse>>('/Call/initiate', { receiverId, isVideo });
    return response.data?.data ?? { callId: '' };
  },
  accept: async (callId: string) => {
    await api.post(`/Call/${callId}/accept`);
  },
  reject: async (callId: string) => {
    await api.post(`/Call/${callId}/reject`);
  },
  end: async (callId: string) => {
    await api.post(`/Call/${callId}/end`);
  },
  /** ICE servers from API. Empty = host-only (LAN / internal network without public STUN). */
  getIceServers: async (): Promise<IceServerConfig[]> => {
    const response = await api.get<ApiResponse<IceServersResponse>>('/Call/ice-servers');
    return response.data?.data?.iceServers ?? [];
  },
  getHistory: async (params: CallHistoryParams = {}) => {
    const response = await api.get<ApiResponse<PaginatedResult<Call>>>('/Call/history', { params });
    return response.data?.data ?? { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 };
  }
};
