import { create } from 'zustand';

export type InAppNotificationType = 'message' | 'group_message' | 'call_initiated' | 'group_call_initiated';

export interface InAppNotification {
  id: string;
  type: InAppNotificationType;
  title: string;
  body: string;
  /** For DM: sender id; for group: group id (string). */
  senderId?: string;
  groupId?: string;
  senderName?: string;
  groupName?: string;
  /** Call-only */
  callId?: string;
  callerId?: string;
  callerName?: string;
  receiverId?: string;
  isVideo?: boolean;
  createdAt: number;
}

interface InAppNotificationState {
  items: InAppNotification[];
  add: (n: Omit<InAppNotification, 'id' | 'createdAt'>) => void;
  remove: (id: string) => void;
  clear: () => void;
}

export const useInAppNotificationStore = create<InAppNotificationState>((set) => ({
  items: [],
  add: (n) => {
    const id = `n-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const item: InAppNotification = { ...n, id, createdAt: Date.now() };
    set((state) => ({ items: [...state.items, item].slice(-10) }));
    // Auto-dismiss after 5s
    setTimeout(() => {
      set((state) => ({ items: state.items.filter((i) => i.id !== id) }));
    }, 5000);
  },
  remove: (id) => set((state) => ({ items: state.items.filter((i) => i.id !== id) })),
  clear: () => set({ items: [] }),
}));
