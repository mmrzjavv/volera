import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type Theme = 'light' | 'dark' | 'system';

export type ChatColorTemplateId =
  | 'default'
  | 'ocean'
  | 'forest'
  | 'sunset'
  | 'lavender'
  | 'mono';

export interface ChatColorTemplate {
  id: ChatColorTemplateId;
  name: string;
  /** Sent message bubble (CSS background) */
  bubbleMe: string;
  /** Received message bubble background */
  bubbleOther: string;
  /** Received message bubble border (optional) */
  bubbleOtherBorder?: string;
  /** Chat area background (optional) */
  chatBg?: string;
}

export const CHAT_COLOR_TEMPLATES: Record<ChatColorTemplateId, ChatColorTemplate> = {
  default: {
    id: 'default',
    name: 'Default',
    bubbleMe: 'rgb(37 99 235)', // blue-600
    bubbleOther: 'rgb(255 255 255)',
    bubbleOtherBorder: 'rgb(229 231 235)',
    chatBg: '',
  },
  ocean: {
    id: 'ocean',
    name: 'Ocean',
    bubbleMe: 'linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%)',
    bubbleOther: 'rgb(224 242 254)',
    bubbleOtherBorder: 'rgb(186 230 253)',
    chatBg: 'linear-gradient(180deg, #f0f9ff 0%, #e0f2fe 100%)',
  },
  forest: {
    id: 'forest',
    name: 'Forest',
    bubbleMe: 'linear-gradient(135deg, #059669 0%, #047857 100%)',
    bubbleOther: 'rgb(220 252 231)',
    bubbleOtherBorder: 'rgb(187 247 208)',
    chatBg: 'linear-gradient(180deg, #f0fdf4 0%, #dcfce7 100%)',
  },
  sunset: {
    id: 'sunset',
    name: 'Sunset',
    bubbleMe: 'linear-gradient(135deg, #f97316 0%, #ea580c 100%)',
    bubbleOther: 'rgb(255 237 213)',
    bubbleOtherBorder: 'rgb(254 215 170)',
    chatBg: 'linear-gradient(180deg, #fff7ed 0%, #ffedd5 100%)',
  },
  lavender: {
    id: 'lavender',
    name: 'Lavender',
    bubbleMe: 'linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)',
    bubbleOther: 'rgb(237 233 254)',
    bubbleOtherBorder: 'rgb(221 214 254)',
    chatBg: 'linear-gradient(180deg, #f5f3ff 0%, #ede9fe 100%)',
  },
  mono: {
    id: 'mono',
    name: 'Monochrome',
    bubbleMe: 'rgb(55 65 81)',
    bubbleOther: 'rgb(243 244 246)',
    bubbleOtherBorder: 'rgb(229 231 235)',
    chatBg: 'rgb(249 250 251)',
  },
};

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  chatColorTemplate: ChatColorTemplateId;
  setChatColorTemplate: (id: ChatColorTemplateId) => void;
  getChatTemplate: () => ChatColorTemplate;
}

const sessionStorageAdapter = {
  getItem: (name: string) => {
    try {
      return sessionStorage.getItem(name);
    } catch {
      return null;
    }
  },
  setItem: (name: string, value: string) => {
    try {
      sessionStorage.setItem(name, value);
    } catch {}
  },
  removeItem: (name: string) => {
    try {
      sessionStorage.removeItem(name);
    } catch {}
  },
};

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'system',
      chatColorTemplate: 'default',
      setTheme: (theme) => {
        set({ theme });
        applyTheme(theme);
        applyChatTemplate(get().chatColorTemplate);
      },
      setChatColorTemplate: (id) => {
        set({ chatColorTemplate: id });
        applyChatTemplate(id);
      },
      getChatTemplate: () => CHAT_COLOR_TEMPLATES[get().chatColorTemplate],
    }),
    {
      name: 'theme-session',
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      storage: {
        getItem: (name: string) => sessionStorageAdapter.getItem(name),
        setItem: (name: string, value: string) => sessionStorageAdapter.setItem(name, value),
        removeItem: (name: string) => sessionStorageAdapter.removeItem(name),
      } as any,
      partialize: (state: ThemeState) => ({
        theme: state.theme,
        chatColorTemplate: state.chatColorTemplate,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          applyTheme(state.theme);
          applyChatTemplate(state.chatColorTemplate);
        }
      },
    }
  )
);

const applyTheme = (theme: Theme) => {
  if (typeof window === 'undefined') return;
  const root = window.document.documentElement;
  root.classList.remove('light', 'dark');
  if (theme === 'system') {
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    root.classList.add(systemTheme);
  } else {
    root.classList.add(theme);
  }
};

const DARK_BUBBLE_OTHER = 'rgb(55 65 81)';      /* gray-700 – received bubbles in dark mode */
const DARK_BUBBLE_OTHER_BORDER = 'rgb(75 85 99)'; /* gray-600 */

const applyChatTemplate = (id: ChatColorTemplateId) => {
  if (typeof document === 'undefined') return;
  const template = CHAT_COLOR_TEMPLATES[id];
  const root = document.documentElement;
  const isDark = root.classList.contains('dark');
  root.style.setProperty('--chat-bubble-me', template.bubbleMe);
  root.style.setProperty(
    '--chat-bubble-other',
    isDark ? DARK_BUBBLE_OTHER : template.bubbleOther
  );
  root.style.setProperty(
    '--chat-bubble-other-border',
    isDark ? DARK_BUBBLE_OTHER_BORDER : (template.bubbleOtherBorder ?? template.bubbleOther)
  );
  root.style.setProperty('--chat-bg', template.chatBg || '');
};

/** Call from App so DOM always reflects store (fixes theme not updating on click). */
export function syncThemeToDom() {
  const state = useThemeStore.getState();
  applyTheme(state.theme);
  applyChatTemplate(state.chatColorTemplate);
}

if (typeof window !== 'undefined') {
  syncThemeToDom();
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const { theme } = useThemeStore.getState();
    if (theme === 'system') applyTheme('system');
  });
}
