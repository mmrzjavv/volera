import { create } from 'zustand';
import type { User, LoginRequest, RegisterRequest } from '../types';
import { authService, refreshAccessToken, userService } from '../services/api';
import { isAccessTokenExpired } from '../utils/jwt';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  /** True after the latest session check finished (success or kick-out). */
  isAuthReady: boolean;
  error: string | null;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  /**
   * Validate session. When forceRefresh is true (default on protected pages),
   * always rotate tokens so the refresh-token window stays extended.
   */
  checkAuth: (options?: { forceRefresh?: boolean }) => Promise<boolean>;
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

function clearSessionLocally(set: (partial: Partial<AuthState>) => void) {
  authService.logout();
  set({ user: null, isAuthenticated: false, isAuthReady: true });
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: getUserFromStorage(),
  isAuthenticated: !!localStorage.getItem('token') || !!localStorage.getItem('refreshToken'),
  isLoading: false,
  isAuthReady: false,
  error: null,

  setUser: (user) => {
      localStorage.setItem('user', JSON.stringify(user));
      set({ user });
  },

  login: async (data) => {
    set({ isLoading: true, error: null });
    try {
      const response = await authService.login(data);
      set({
        user: response.user,
        isAuthenticated: true,
        isLoading: false,
        isAuthReady: true,
      });
    } catch (error: any) {
      set({ error: error.response?.data?.message || 'Login failed', isLoading: false, isAuthReady: true });
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
    clearSessionLocally(set);
  },

  checkAuth: async (options) => {
      const forceRefresh = options?.forceRefresh ?? false;
      const token = localStorage.getItem('token');
      const refreshToken = localStorage.getItem('refreshToken');

      if (!token && !refreshToken) {
          set({ isAuthenticated: false, user: null, isAuthReady: true });
          return false;
      }

      set({ isAuthReady: false });

      try {
          const needsRefresh = forceRefresh || isAccessTokenExpired(token);
          if (needsRefresh) {
              try {
                  const newToken = await refreshAccessToken();
                  if (!newToken) {
                      clearSessionLocally(set);
                      return false;
                  }
              } catch {
                  // Offline / transient error while refreshing.
                  // Keep the session only if the access token is still usable.
                  if (isAccessTokenExpired(token, 0)) {
                      clearSessionLocally(set);
                      return false;
                  }
              }
          }

          const user = await userService.getProfile();
          localStorage.setItem('user', JSON.stringify(user));
          set({ user, isAuthenticated: true, isAuthReady: true });
          return true;
      } catch {
          // Profile fetch failed. If interceptor already refreshed+retried, this is real auth failure
          // or offline. Kick out only when the access token is expired / missing.
          if (isAccessTokenExpired(localStorage.getItem('token'), 0) || !localStorage.getItem('refreshToken')) {
              clearSessionLocally(set);
              return false;
          }
          // Soft keep: cached user + still-valid token (e.g. offline)
          const cached = get().user ?? getUserFromStorage();
          set({
            user: cached,
            isAuthenticated: true,
            isAuthReady: true,
          });
          return true;
      }
  }
}));
