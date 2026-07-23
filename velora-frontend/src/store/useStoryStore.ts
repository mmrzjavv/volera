import { create } from 'zustand';
import type { StoryRing, Story, CreateStoryItemPayload } from '../types';
import { storyService } from '../services/api';

interface StoryState {
  rings: StoryRing[];
  isLoading: boolean;
  viewerUserId: string | null;
  composerOpen: boolean;
  fetchFeed: () => Promise<void>;
  openViewer: (userId: string) => void;
  closeViewer: () => void;
  openComposer: () => void;
  closeComposer: () => void;
  createStory: (items: CreateStoryItemPayload[]) => Promise<void>;
  markViewed: (storyId: string) => Promise<void>;
  deleteStory: (storyId: string) => Promise<void>;
  replyToItem: (itemId: string, content: string) => Promise<void>;
  getRingStories: (userId: string) => Story[];
}

export const useStoryStore = create<StoryState>((set, get) => ({
  rings: [],
  isLoading: false,
  viewerUserId: null,
  composerOpen: false,

  fetchFeed: async () => {
    set({ isLoading: true });
    try {
      const rings = await storyService.getFeed();
      set({ rings });
    } catch (err) {
      console.error('Failed to load story feed', err);
    } finally {
      set({ isLoading: false });
    }
  },

  openViewer: (userId) => set({ viewerUserId: userId }),
  closeViewer: () => set({ viewerUserId: null }),
  openComposer: () => set({ composerOpen: true }),
  closeComposer: () => set({ composerOpen: false }),

  createStory: async (items) => {
    await storyService.create(items);
    await get().fetchFeed();
    set({ composerOpen: false });
  },

  markViewed: async (storyId) => {
    try {
      await storyService.markViewed(storyId);
      set((state) => ({
        rings: state.rings.map((ring) => ({
          ...ring,
          stories: ring.stories.map((s) =>
            s.storyId === storyId ? { ...s, viewedByMe: true } : s
          ),
          hasUnseen: ring.stories.some((s) => s.storyId !== storyId && !s.viewedByMe),
        })),
      }));
    } catch (err) {
      console.error('Failed to mark story viewed', err);
    }
  },

  deleteStory: async (storyId) => {
    await storyService.deleteStory(storyId);
    await get().fetchFeed();
  },

  replyToItem: async (itemId, content) => {
    await storyService.reply(itemId, content);
  },

  getRingStories: (userId) => {
    const ring = get().rings.find((r) => r.userId === userId);
    return ring?.stories ?? [];
  },
}));
