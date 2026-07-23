import { create } from 'zustand';
import type { WidgetConfig } from '@/types';

interface WidgetState {
  config: WidgetConfig;
  setBranchId: (branchId: string) => void;
  setColor: (color: string) => void;
  setPosition: (position: WidgetConfig['position']) => void;
  reset: () => void;
}

const DEFAULT: WidgetConfig = {
  branchId: '',
  color: '#0ea5e9',
  position: 'bottom-right',
  scriptUrl: 'https://example.com/widget.js',
};

export const useWidgetStore = create<WidgetState>((set) => ({
  config: { ...DEFAULT },

  setBranchId: (branchId) =>
    set((s) => ({ config: { ...s.config, branchId } })),

  setColor: (color) =>
    set((s) => ({ config: { ...s.config, color } })),

  setPosition: (position) =>
    set((s) => ({ config: { ...s.config, position } })),

  reset: () => set({ config: { ...DEFAULT } }),
}));
