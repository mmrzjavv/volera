import { create } from 'zustand';
import { getUpdateStatus, dismissUpdateBanner, clearCacheAndReload } from '../utils/versionCheck';

interface VersionState {
  updateAvailable: boolean;
  serverVersion: string | null;
  /** True after we've run the check once this page load – prevents calling API again and again. */
  versionCheckDone: boolean;
  checkForUpdate: () => Promise<void>;
  dismissUpdate: () => void;
  clearCacheAndReload: () => Promise<void>;
}

export const useVersionStore = create<VersionState>((set, get) => ({
  updateAvailable: false,
  serverVersion: null,
  versionCheckDone: false,

  checkForUpdate: async () => {
    if (get().versionCheckDone) return;
    set({ versionCheckDone: true });
    const { updateAvailable, serverVersion } = await getUpdateStatus();
    set({ updateAvailable, serverVersion: serverVersion || null });
  },

  dismissUpdate: () => {
    const { serverVersion } = get();
    if (serverVersion) {
      dismissUpdateBanner(serverVersion);
      set({ updateAvailable: false });
    }
  },

  clearCacheAndReload: async () => {
    await clearCacheAndReload();
  },
}));
