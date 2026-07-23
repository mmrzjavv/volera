import { create } from 'zustand';
import type { SupportUserDto } from '@/api/support';

const STORAGE_KEY = 'widget_admin_support';

interface PersistedSupportSession {
  token: string;
  expiresAt: string;
  supportUserId: string;
  companyId: string;
  username: string;
  firstName: string;
  lastName: string;
  role: string;
}

function loadSession(): PersistedSupportSession | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const data = JSON.parse(raw) as PersistedSupportSession;
    if (!data.token || !data.supportUserId) return null;
    if (data.expiresAt && new Date(data.expiresAt) <= new Date()) return null;
    return data;
  } catch {
    return null;
  }
}

function saveSession(s: PersistedSupportSession | null) {
  if (typeof window === 'undefined') return;
  if (s) localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
  else localStorage.removeItem(STORAGE_KEY);
}

export interface SupportAuthUser {
  token: string;
  expiresAt: string;
  supportUser: SupportUserDto;
}

interface SupportAuthState {
  auth: SupportAuthUser | null;
  setSession: (data: {
    token: string;
    expiresAt: string;
    supportUser: SupportUserDto;
  }) => void;
  logout: () => void;
  isAuthenticated: boolean;
  rehydrate: () => void;
}

export const useSupportAuthStore = create<SupportAuthState>((set, get) => ({
  auth: null,
  isAuthenticated: false,

  rehydrate: () => {
    const session = loadSession();
    if (!session) return;
    set({
      auth: {
        token: session.token,
        expiresAt: session.expiresAt,
        supportUser: {
          id: session.supportUserId,
          companyId: session.companyId,
          username: session.username,
          firstName: session.firstName,
          lastName: session.lastName,
          email: null,
          phoneNumber: null,
          role: session.role,
        },
      },
      isAuthenticated: true,
    });
  },

  setSession: ({ token, expiresAt, supportUser }) => {
    saveSession({
      token,
      expiresAt,
      supportUserId: supportUser.id,
      companyId: supportUser.companyId,
      username: supportUser.username,
      firstName: supportUser.firstName,
      lastName: supportUser.lastName,
      role: supportUser.role,
    });
    set({
      auth: {
        token,
        expiresAt,
        supportUser,
      },
      isAuthenticated: true,
    });
  },

  logout: () => {
    saveSession(null);
    set({ auth: null, isAuthenticated: false });
  },
}));
