import { create } from 'zustand';
import type { Branch, SupportUser } from '@/types';
import type {
  BranchDto,
  SupportUserDto,
  CreateBranchRequest,
  UpdateBranchRequest,
  CreateSupportUserRequest,
  UpdateSupportUserRequest,
} from '@/api/company';
import { companyApi } from '@/api/company';
import { MOCK_PLANS } from '@/data/plans';

/** Map backend role string to frontend Role for UI */
function mapRole(role: string): SupportUser['role'] {
  switch (role) {
    case 'CompanyAdmin':
      return 'CompanyAdmin';
    case 'SupportManager':
      return 'BranchManager';
    case 'SupportAgent':
    default:
      return 'SupportUser';
  }
}

function branchDtoToBranch(d: BranchDto): Branch {
  return {
    id: d.id,
    name: d.name,
    address: d.address ?? null,
    phone: d.phoneNumber ?? null,
    email: d.email ?? null,
    createdAt: new Date().toISOString(),
  };
}

interface CompanyState {
  branches: Branch[];
  supportUsers: SupportUser[];
  /** userId -> branchIds (loaded per user when needed) */
  supportUserBranchIds: Record<string, string[]>;
  plan: typeof MOCK_PLANS[number] | null;
  loading: boolean;
  error: string | null;
  setPlan: (planId: string) => void;
  loadBranches: (companyToken: string) => Promise<void>;
  loadSupportUsers: (companyToken: string) => Promise<void>;
  loadSupportUserBranches: (companyToken: string, supportUserId: string) => Promise<string[]>;
  addBranch: (companyToken: string, branch: CreateBranchRequest) => Promise<{ success: boolean; error?: string }>;
  updateBranch: (companyToken: string, branchId: string, data: UpdateBranchRequest) => Promise<{ success: boolean; error?: string }>;
  deleteBranch: (companyToken: string, branchId: string) => Promise<{ success: boolean; error?: string }>;
  addSupportUser: (companyToken: string, user: CreateSupportUserRequest) => Promise<{ success: boolean; error?: string }>;
  updateSupportUser: (companyToken: string, userId: string, data: UpdateSupportUserRequest) => Promise<{ success: boolean; error?: string }>;
  deleteSupportUser: (companyToken: string, userId: string) => Promise<{ success: boolean; error?: string }>;
  assignUserToBranch: (companyToken: string, userId: string, branchId: string) => Promise<{ success: boolean; error?: string }>;
  unassignUserFromBranch: (companyToken: string, userId: string, branchId: string) => Promise<{ success: boolean; error?: string }>;
  clearError: () => void;
  reset: () => void;
}

export const useCompanyStore = create<CompanyState>((set, get) => ({
  branches: [],
  supportUsers: [],
  supportUserBranchIds: {},
  plan: MOCK_PLANS[0] ?? null,
  loading: false,
  error: null,

  setPlan: (planId) => {
    const plan = MOCK_PLANS.find((p) => p.id === planId) ?? null;
    set({ plan });
  },

  loadBranches: async (companyToken) => {
    set({ loading: true, error: null });
    try {
      const res = await companyApi.getBranches(companyToken);
      if (res.success && Array.isArray(res.data)) {
        set({ branches: res.data.map(branchDtoToBranch) });
      }
    } catch (e) {
      set({ error: e instanceof Error ? e.message : 'Failed to load branches' });
    } finally {
      set({ loading: false });
    }
  },

  loadSupportUsers: async (companyToken) => {
    set({ loading: true, error: null });
    try {
      const res = await companyApi.getSupportUsers(companyToken);
      if (res.success && Array.isArray(res.data)) {
        // Load branch assignments for all users first
        const branchIdsMap: Record<string, string[]> = {};
        await Promise.all(
          res.data.map(async (u: SupportUserDto) => {
            try {
              const branchRes = await companyApi.getSupportUserBranches(companyToken, u.id);
              if (branchRes.success && Array.isArray(branchRes.data)) {
                branchIdsMap[u.id] = branchRes.data.map((b) => b.id);
              } else {
                branchIdsMap[u.id] = [];
              }
            } catch {
              branchIdsMap[u.id] = [];
            }
          })
        );
        
        // Create users with branch IDs already populated
        const users: SupportUser[] = res.data.map((u: SupportUserDto) => ({
          id: u.id,
          username: u.username,
          firstName: u.firstName,
          lastName: u.lastName,
          email: u.email ?? null,
          role: mapRole(u.role),
          branchIds: branchIdsMap[u.id] ?? [],
          isActive: true,
        }));
        
        // Set both users and branch IDs map
        set({
          supportUsers: users,
          supportUserBranchIds: { ...get().supportUserBranchIds, ...branchIdsMap },
        });
      }
    } catch (e) {
      set({ error: e instanceof Error ? e.message : 'Failed to load support users' });
    } finally {
      set({ loading: false });
    }
  },

  loadSupportUserBranches: async (companyToken, supportUserId) => {
    try {
      const res = await companyApi.getSupportUserBranches(companyToken, supportUserId);
      if (res.success && Array.isArray(res.data)) {
        const ids = res.data.map((b) => b.id);
        set((s) => ({
          supportUserBranchIds: { ...s.supportUserBranchIds, [supportUserId]: ids },
          supportUsers: s.supportUsers.map((u) =>
            u.id === supportUserId ? { ...u, branchIds: ids } : u
          ),
        }));
        return ids;
      }
    } catch {
      // ignore
    }
    return get().supportUserBranchIds[supportUserId] ?? [];
  },

  addBranch: async (companyToken, branch) => {
    set({ error: null });
    try {
      const res = await companyApi.createBranch(companyToken, branch);
      if (res.success && res.data?.branchId) {
        await get().loadBranches(companyToken);
        return { success: true };
      }
      return { success: false, error: 'Failed to create branch' };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to create branch';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  updateBranch: async (companyToken, branchId, data) => {
    set({ error: null });
    try {
      await companyApi.updateBranch(companyToken, branchId, data);
      await get().loadBranches(companyToken);
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to update branch';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  deleteBranch: async (companyToken, branchId) => {
    set({ error: null });
    try {
      await companyApi.deleteBranch(companyToken, branchId);
      await get().loadBranches(companyToken);
      set((s) => ({
        supportUserBranchIds: Object.fromEntries(
          Object.entries(s.supportUserBranchIds).map(([uid, ids]) => [uid, ids.filter((id) => id !== branchId)])
        ),
      }));
      await get().loadSupportUsers(companyToken);
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to delete branch';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  addSupportUser: async (companyToken, user) => {
    set({ error: null });
    try {
      const res = await companyApi.createSupportUser(companyToken, user);
      if (res.success) {
        await get().loadSupportUsers(companyToken);
        return { success: true };
      }
      return { success: false, error: 'Failed to create support user' };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to create support user';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  updateSupportUser: async (companyToken, userId, data) => {
    set({ error: null });
    try {
      await companyApi.updateSupportUser(companyToken, userId, data);
      await get().loadSupportUsers(companyToken);
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to update user';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  deleteSupportUser: async (companyToken, userId) => {
    set({ error: null });
    try {
      await companyApi.deleteSupportUser(companyToken, userId);
      await get().loadSupportUsers(companyToken);
      set((s) => {
        const { [userId]: _, ...rest } = s.supportUserBranchIds;
        return { supportUserBranchIds: rest };
      });
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to delete user';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  assignUserToBranch: async (companyToken, userId, branchId) => {
    set({ error: null });
    try {
      await companyApi.assignSupportUserToBranch(companyToken, userId, branchId);
      await get().loadSupportUserBranches(companyToken, userId);
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to assign branch';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  unassignUserFromBranch: async (companyToken, userId, branchId) => {
    set({ error: null });
    try {
      await companyApi.unassignSupportUserFromBranch(companyToken, userId, branchId);
      await get().loadSupportUserBranches(companyToken, userId);
      return { success: true };
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Failed to unassign branch';
      set({ error: msg });
      return { success: false, error: msg };
    }
  },

  clearError: () => set({ error: null }),
  reset: () => set({ branches: [], supportUsers: [], supportUserBranchIds: {}, error: null }),
}));
