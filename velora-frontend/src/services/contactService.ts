import api from './api';
import type { ApiResponse, Contact, AddContactDto, SyncContactsDto } from '../types';

export const contactService = {
  getContacts: async (status?: string) => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<Contact[]>>('/Contact', { params });
    return response.data?.data ?? [];
  },

  addContact: async (data: AddContactDto) => {
    const response = await api.post<ApiResponse<{ id: string }>>('/Contact/add', data);
    return response.data?.data ?? response.data as unknown as { id: string };
  },

  syncContacts: async (data: SyncContactsDto) => {
    const response = await api.post<ApiResponse<Contact[]>>('/Contact/sync', data);
    return response.data?.data ?? [];
  },

  deleteContact: async (id: string) => {
    await api.delete(`/Contact/${id}`);
  },

  searchUsers: async (query: string, by: 'username' | 'phoneNumber' = 'username') => {
    const params = by === 'username'
      ? { username: query }
      : { phoneNumber: query };
    const response = await api.get<ApiResponse<unknown>>('/User/search', { params });
    return response.data?.data ?? response.data;
  }
};
