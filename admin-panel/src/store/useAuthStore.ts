import { create } from 'zustand';
import type { Role } from '@/types';
import type { CompanyProfile } from '@/api/company';
import { companyApi } from '@/api/company';

const STORAGE_KEY = 'widget_admin_company';

interface PersistedSession {
  companyId: string;
  token: string;
  expiresAt: string;
}

function loadSession(): PersistedSession | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const data = JSON.parse(raw) as PersistedSession;
    if (!data.companyId || !data.token) return null;
    if (data.expiresAt && new Date(data.expiresAt) <= new Date()) return null;
    return data;
  } catch {
    return null;
  }
}

function saveSession(s: PersistedSession | null) {
  if (typeof window === 'undefined') return;
  if (s) localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
  else localStorage.removeItem(STORAGE_KEY);
}

export interface AuthUser {
  companyId: string;
  token: string;
  expiresAt: string;
  profile: CompanyProfile | null;
  role: Role;
}

interface AuthState {
  auth: AuthUser | null;
  setRole: (role: Role) => void;
  setSession: (companyId: string, token: string, expiresAt: string) => void;
  loadProfile: () => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  rehydrate: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  auth: null,
  isAuthenticated: false,

  rehydrate: () => {
    const session = loadSession();
    if (!session) return;
    set({
      auth: {
        companyId: session.companyId,
        token: session.token,
        expiresAt: session.expiresAt,
        profile: null,
        role: 'CompanyAdmin',
      },
      isAuthenticated: true,
    });
  },

  setRole: (role) =>
    set((state) => {
      if (!state.auth) return state;
      return { auth: { ...state.auth, role } };
    }),

  setSession: (companyId, token, expiresAt) => {
    saveSession({ companyId, token, expiresAt });
    set({
      auth: {
        companyId,
        token,
        expiresAt,
        profile: null,
        role: 'CompanyAdmin',
      },
      isAuthenticated: true,
    });
  },

  loadProfile: async () => {
    const { auth } = get();
    if (!auth?.token) return;
    try {
      const res = await companyApi.getProfile(auth.token);
      if (res.success && res.data) {
        set((s) =>
          s.auth
            ? { auth: { ...s.auth, profile: res.data! } }
            : s
        );
      }
    } catch {
      // Token invalid or network error - leave profile null
    }
  },

  logout: () => {
    saveSession(null);
    set({ auth: null, isAuthenticated: false });
  },
}));
