import api from './api';
import type { ApiResponse } from '../types';

interface InitiateGroupCallResponse {
  groupCallId: string;
}

export const groupCallService = {
  initiate: async (groupId: string, isVideo: boolean = false) => {
    const response = await api.post<ApiResponse<InitiateGroupCallResponse>>('/GroupCall/initiate', {
      groupId,
      isVideo,
    });
    return response.data?.data ?? { groupCallId: '' };
  },

  join: async (groupCallId: string) => {
    await api.post(`/GroupCall/${groupCallId}/join`);
  },

  end: async (groupCallId: string) => {
    await api.post(`/GroupCall/${groupCallId}/end`);
  },
};

