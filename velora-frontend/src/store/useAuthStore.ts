import { create } from 'zustand';
import type { User, LoginRequest, RegisterRequest } from '../types';
import { authService, userService } from '../services/api';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  checkAuth: () => Promise<void>;
  setUser: (user: User) => void;
}

const getUserFromStorage = () => {
  try {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  } catch (e) {
    console.error("Failed to parse user from storage", e);
    return null;
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  user: getUserFromStorage(),
  isAuthenticated: !!localStorage.getItem('token'),
  isLoading: false,
  error: null,

  setUser: (user) => {
      localStorage.setItem('user', JSON.stringify(user));
      set({ user });
  },

  login: async (data) => {
    set({ isLoading: true, error: null });
    try {
      const response = await authService.login(data);
      set({ user: response.user, isAuthenticated: true, isLoading: false });
    } catch (error: any) {
      set({ error: error.response?.data?.message || 'Login failed', isLoading: false });
      throw error;
    }
  },

  register: async (data) => {
    set({ isLoading: true, error: null });
    try {
      await authService.register(data);
      set({ isLoading: false });
    } catch (error: any) {
      set({ error: error.response?.data?.message || 'Registration failed', isLoading: false });
      throw error;
    }
  },

  logout: () => {
    authService.logout();
    set({ user: null, isAuthenticated: false });
  },

  checkAuth: async () => {
      const token = localStorage.getItem('token');
      if (!token) {
          set({ isAuthenticated: false, user: null });
          return;
      }
      try {
          const user = await userService.getProfile();
          set({ user, isAuthenticated: true });
      } catch (error) {
          authService.logout();
          set({ isAuthenticated: false, user: null });
      }
  }
}));
