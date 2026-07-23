import { create } from 'zustand';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import type { Message, User, Group, RecentChat } from '../types';
import { messageService, groupService, userService, channelService, getApiBase } from '../services/api';
import { useAuthStore } from './useAuthStore';
import {
  enqueueOutgoingMessage,
  scheduleProcessQueue,
  startOutgoingQueueWatchers,
  subscribeOutgoingLifecycle,
} from '../offline/messageSendPipeline';
import { invalidateReachabilityCache } from '../offline/serverReachability';
import { createClientMessageId } from '../utils/createClientMessageId';

let outgoingLifecycleBound = false;

interface ChatState {
  messages: Message[];
  users: User[];
  groups: Group[];
  channels: Group[];
  recentChats: RecentChat[];
  selectedUser: User | null;
  selectedGroup: Group | null;
  showSavedMessages: boolean;
  showSystemMessages: boolean;
  connection: HubConnection | null;
  isLoadingMessages: boolean;
  hasMoreMessages: boolean;
  unreadCounts: Record<string, number>;
  
  initializeConnection: () => void;
  selectUser: (user: User) => Promise<void>;
  selectGroup: (group: Group) => Promise<void>;
  /** Select chat by user id (e.g. from notification); fetches user if not in list. */
  selectUserById: (userId: string) => Promise<void>;
  /** Select chat by group id (e.g. from notification); fetches group if not in list. */
  selectGroupById: (groupId: string) => Promise<void>;
  selectSavedMessages: () => Promise<void>;
  selectSystemMessages: () => void;
  loadMoreMessages: () => Promise<void>;
  sendMessage: (content: string, attachmentUrl?: string, attachmentType?: string, replyToMessageId?: string, sendAsChannelId?: string) => Promise<void>;
  ingestRealtimeMessage: (message: Message) => void;
  syncOpenConversation: () => Promise<void>;
  setUsers: (users: User[]) => void;
  fetchGroups: () => Promise<void>;
  fetchChannels: () => Promise<void>;
  fetchRecentChats: () => Promise<void>;
  removeChatOptimistic: (chat: RecentChat) => void;
  restoreChat: (chat: RecentChat) => void;
  addMessage: (message: Message) => void;
  updateMessage: (id: string, updates: Partial<Message>) => void;
  setMessageReactions: (id: string, reactions: { userId: string; userName?: string; emoji: string }[]) => void;
  toggleMessageSaved: (messageId: string, isSaved: boolean) => void;
  updateUserStatus: (userId: string, isOnline: boolean) => void;
  clearUnread: (id: string) => void;
  fetchUnreadCounts: () => Promise<void>;
  disconnect: () => void;
  clearSelection: () => void;
  /** Merge details (e.g. profilePictureUrl from getGroupDetails) into selectedGroup */
  updateSelectedGroup: (updates: Partial<Group>) => void;
}

export const useChatStore = create<ChatState>((set, get) => ({
  messages: [],
  users: [],
  groups: [],
  channels: [],
  recentChats: [],
  selectedUser: null,
  selectedGroup: null,
  showSavedMessages: false,
  showSystemMessages: false,
  connection: null,
  isLoadingMessages: false,
  hasMoreMessages: false,
  unreadCounts: {},

  initializeConnection: () => {
    const token = localStorage.getItem('token');
    if (!token) return;

    // Fetch initial data
    get().fetchUnreadCounts();
    get().fetchRecentChats();

    if (!outgoingLifecycleBound) {
      outgoingLifecycleBound = true;
      startOutgoingQueueWatchers();
      subscribeOutgoingLifecycle((item) => {
        set((state) => ({
          messages: state.messages.map((msg) => {
            if (msg.clientMessageId !== item.clientMessageId) return msg;
            return {
              ...msg,
              id: item.serverMessageId ?? msg.id,
              deliveryStatus: item.status === 'accepted' ? 'accepted' : item.status,
            };
          }),
        }));
      });
    }

    // Check if connection already exists
    if (get().connection) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${getApiBase() || window.location.origin}/chatHub`, {
        accessTokenFactory: () => localStorage.getItem('token') || token
      })
      .withAutomaticReconnect()
      .configureLogging({
          log: (logLevel: LogLevel, message: string) => {
              // Suppress "stopped during negotiation" error which is common in React StrictMode
              if (logLevel === LogLevel.Error && message.includes('stopped during negotiation')) {
                  return;
              }
              if (logLevel >= LogLevel.Information) {
                  console.log(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
              }
          }
      })
      .build();

    connection.onreconnected(() => {
      invalidateReachabilityCache();
      scheduleProcessQueue(0);
      void get().syncOpenConversation();
      void get().fetchRecentChats();
    });

    if (connection.state === HubConnectionState.Disconnected) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected');
          scheduleProcessQueue(0);
          
          connection.on('ReceiveMessage', (message: Message) => {
            get().ingestRealtimeMessage(message);
        });

        connection.on('MessageSent', (message: Message) => {
            get().ingestRealtimeMessage(message);
            get().fetchRecentChats();
        });

        connection.on('MessageEdited', (data: { messageId: string, newContent: string, editedAt: string, groupId?: string, receiverId?: string }) => {
            get().updateMessage(data.messageId, { content: data.newContent, isEdited: true });
        });

        connection.on('MessageDeleted', (data: { messageId: string, deletedAt: string, groupId?: string, receiverId?: string }) => {
             get().updateMessage(data.messageId, { deletedAt: data.deletedAt });
        });

        connection.on('MessagesRead', (data: { userId: string, readAt: string }) => {
             set((state) => ({
                 messages: state.messages.map(msg => 
                     msg.receiverId === data.userId && !msg.isRead 
                     ? { ...msg, isRead: true, readAt: data.readAt } 
                     : msg
                 )
             }));
        });

        connection.on('MessageReactionsUpdated', (data: { messageId: string; reactions: { userId: string; userName?: string; emoji: string }[] }) => {
            get().setMessageReactions(data.messageId, data.reactions);
        });
        connection.on('MessagePinnedUpdated', (data: { messageId: string; isPinned: boolean; pinnedAt?: string; pinnedByUserId?: string }) => {
            get().updateMessage(data.messageId, {
                isPinned: data.isPinned,
                pinnedAt: data.pinnedAt,
                pinnedByUserId: data.pinnedByUserId,
            });
        });
        connection.on('StoryCreated', () => {
            void import('./useStoryStore').then(({ useStoryStore }) => {
              void useStoryStore.getState().fetchFeed();
            });
        });
        connection.on('StoryDeleted', () => {
            void import('./useStoryStore').then(({ useStoryStore }) => {
              void useStoryStore.getState().fetchFeed();
            });
        });
      })
      .catch(err => {
        if (err.message && (err.message.includes('AbortError') || err.message.includes('negotiation'))) {
            return;
        }
        console.error('SignalR Connection Error: ', err);
        // HTTP sync fallback when realtime cannot start
        void get().syncOpenConversation();
        scheduleProcessQueue(1000);
      });
    }

    set({ connection });
  },

  disconnect: () => {
      const { connection } = get();
      if (connection) {
          connection.stop();
          set({ connection: null });
      }
  },

  selectUser: async (user) => {
    if (!user) {
        set({ selectedUser: null, messages: [], showSystemMessages: false });
        return;
    }
    set({ selectedUser: user, selectedGroup: null, showSavedMessages: false, showSystemMessages: false, isLoadingMessages: true, hasMoreMessages: true });
    
    // Optimistically clear unread count
    set((state) => {
        const newUnread = { ...state.unreadCounts };
        delete newUnread[user.id];
        
        const newRecentChats = state.recentChats.map(chat => 
            chat.userId === user.id ? { ...chat, unreadCount: 0 } : chat
        );
        
        return { unreadCounts: newUnread, recentChats: newRecentChats };
    });

    try {
      const pageSize = 20;
      const [messages] = await Promise.all([
          messageService.getConversation(user.id, pageSize),
          messageService.markAsRead(user.id)
      ]);

      set({
          messages: messages,
          isLoadingMessages: false,
          hasMoreMessages: messages.length === pageSize
      });
    } catch (error) {
      console.error(error);
      set({ isLoadingMessages: false });
    }
  },

  selectGroup: async (group) => {
      if (!group) {
          set({ selectedGroup: null, messages: [], showSystemMessages: false });
          return;
      }
      set({ selectedGroup: group, selectedUser: null, showSavedMessages: false, showSystemMessages: false, isLoadingMessages: true, hasMoreMessages: true });
      try {
          const pageSize = 20;
          let enriched = group;
          try {
              if (group.isChannel) {
                  const details = await channelService.getChannelDetails(group.id);
                  enriched = {
                      ...group,
                      name: details.name || group.name,
                      adminId: details.adminId || group.adminId,
                      profilePictureUrl: details.profilePictureUrl,
                      isChannel: true,
                      canPost: details.canPost,
                      isAdmin: details.isAdmin,
                      isPublic: details.isPublic,
                      publicUsername: details.publicUsername,
                      signaturesEnabled: details.signaturesEnabled,
                      linkedDiscussionGroupId: details.linkedDiscussionGroupId,
                      subscriberCount: details.subscriberCount,
                  };
                  set({ selectedGroup: enriched });
              }
          } catch { /* keep thin group */ }

          const messages = await groupService.getGroupMessages(enriched.id, pageSize);
          set({
              messages: messages,
              isLoadingMessages: false,
              hasMoreMessages: messages.length === pageSize
          });
          if (enriched.isChannel && messages.length > 0) {
              void channelService.recordViews(enriched.id, messages.map((m) => m.id)).catch(() => undefined);
          }
      } catch (error) {
          console.error(error);
          set({ isLoadingMessages: false });
      }
  },

  selectUserById: async (userId: string) => {
      const { users } = get();
      let user = users.find((u) => u.id === userId);
      if (!user) {
          try {
              user = await userService.getPublicProfile(userId);
          } catch (e) {
              console.error('Failed to load user for notification', e);
              return;
          }
      }
      await get().selectUser(user);
  },

  selectGroupById: async (groupId: string) => {
      const { groups, channels } = get();
      let group = groups.find((g) => g.id === groupId) ?? channels.find((g) => g.id === groupId);
      if (!group) {
          try {
              try {
                  const details = await channelService.getChannelDetails(groupId);
                  group = {
                      id: details.id,
                      name: details.name,
                      adminId: details.adminId,
                      createdAt: details.createdAt,
                      profilePictureUrl: details.profilePictureUrl,
                      isChannel: true,
                      canPost: details.canPost,
                      isAdmin: details.isAdmin,
                  };
              } catch {
                  const details = await groupService.getGroupDetails(groupId) as Record<string, unknown>;
                  const id = details?.id ?? details?.Id ?? groupId;
                  group = {
                      id: String(id),
                      name: String(details?.name ?? details?.Name ?? 'Group'),
                      adminId: String(details?.adminId ?? details?.AdminId ?? ''),
                      createdAt: details?.createdAt ? String(details.createdAt) : new Date().toISOString()
                  };
              }
          } catch (e) {
              console.error('Failed to load group', e);
              return;
          }
      }
      await get().selectGroup(group);
  },

  selectSystemMessages: () => {
      set({ selectedUser: null, selectedGroup: null, showSavedMessages: false, showSystemMessages: true, messages: [] });
  },

  selectSavedMessages: async () => {
      const currentUser = useAuthStore.getState().user;
      if (!currentUser) {
          set({ showSavedMessages: false, showSystemMessages: false, isLoadingMessages: false, hasMoreMessages: false });
          return;
      }
      set({ selectedUser: null, selectedGroup: null, showSavedMessages: true, showSystemMessages: false, isLoadingMessages: true, hasMoreMessages: true });
      try {
          const pageSize = 20;
          // Saved messages space is just conversation with self
          const messages = await messageService.getConversation(currentUser.id, pageSize);
          set({
              messages,
              isLoadingMessages: false,
              hasMoreMessages: messages.length === pageSize
          });
      } catch (error) {
          console.error("Failed to load saved conversation", error);
          set({ isLoadingMessages: false });
      }
  },

  loadMoreMessages: async () => {
      const { selectedUser, selectedGroup, showSavedMessages, messages, isLoadingMessages, hasMoreMessages } = get();
      if (isLoadingMessages || !hasMoreMessages || messages.length === 0) return;

      set({ isLoadingMessages: true });
      try {
          const oldestMessage = messages[0];
          const before = oldestMessage.sentAt;
          const pageSize = 20;

          let newMessages: Message[] = [];

          if (selectedUser) {
              newMessages = await messageService.getConversation(selectedUser.id, pageSize, before);
          } else if (selectedGroup) {
              newMessages = await groupService.getGroupMessages(selectedGroup.id, pageSize, before);
          } else if (showSavedMessages) {
              const currentUser = useAuthStore.getState().user;
              if (currentUser) {
                  newMessages = await messageService.getConversation(currentUser.id, pageSize, before);
              }
          }

          if (newMessages.length > 0) {
              set(state => ({
                  messages: [...newMessages, ...state.messages],
                  hasMoreMessages: newMessages.length === pageSize
              }));
          } else {
              set({ hasMoreMessages: false });
          }
      } catch (error) {
          console.error("Failed to load more messages", error);
      } finally {
          set({ isLoadingMessages: false });
      }
  },

  fetchGroups: async () => {
      try {
          const groups = await groupService.getMyGroups();
          set({ groups: groups as Group[] });
      } catch (error) {
          console.error(error);
      }
  },

  fetchChannels: async () => {
      try {
          const list = await channelService.getMyChannels();
          set({
              channels: (list || []).map((c) => ({
                  ...c,
                  isChannel: true,
                  kind: 'Channel',
              })),
          });
      } catch (error) {
          console.error(error);
          set({ channels: [] });
      }
  },

  fetchRecentChats: async () => {
      try {
          const recentChats = await messageService.getRecentChats();
          set({ recentChats });
      } catch (error) {
          console.error("Failed to fetch recent chats", error);
      }
  },
  removeChatOptimistic: (chat) =>
      set((state) => ({
          recentChats: state.recentChats.filter(
              (c) => !((chat.userId && c.userId === chat.userId) || (chat.groupId && c.groupId === chat.groupId))
          ),
      })),
  restoreChat: (chat) =>
      set((state) => {
          const exists = state.recentChats.some(
              (c) => (chat.userId && c.userId === chat.userId) || (chat.groupId && c.groupId === chat.groupId)
          );
          if (exists) return state;
          return { recentChats: [...state.recentChats, chat].sort((a, b) =>
              new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
          ) };
      }),

  sendMessage: async (content, attachmentUrl, attachmentType, replyToMessageId, sendAsChannelId) => {
    const { selectedUser, selectedGroup, showSavedMessages } = get();
    const currentUser = useAuthStore.getState().user;
    if (!currentUser) throw new Error('Not authenticated');

    const clientMessageId = createClientMessageId();
    let receiverId: string | undefined;
    let groupId: string | undefined;

    if (selectedUser) receiverId = selectedUser.id;
    else if (selectedGroup) groupId = selectedGroup.id;
    else if (showSavedMessages) receiverId = currentUser.id;
    else throw new Error('No conversation selected');

    const optimistic: Message = {
      id: clientMessageId,
      clientMessageId,
      senderId: currentUser.id,
      receiverId,
      groupId,
      content,
      attachmentUrl,
      attachmentType,
      replyToMessageId,
      sendAsChannelId,
      sentAt: new Date().toISOString(),
      isRead: false,
      deliveryStatus: 'queued',
    };
    get().addMessage(optimistic);

    await enqueueOutgoingMessage({
      clientMessageId,
      receiverId,
      groupId,
      content,
      attachmentUrl,
      attachmentType,
      replyToMessageId,
      sendAsChannelId,
    });
  },

  ingestRealtimeMessage: (message) => {
    const { selectedUser, selectedGroup, showSavedMessages, users } = get();
    const currentUser = useAuthStore.getState().user;

    let mergedExisting = false;
    set((state) => {
      const byClient = message.clientMessageId
        ? state.messages.findIndex((m) => m.clientMessageId === message.clientMessageId)
        : -1;
      const byId = state.messages.findIndex((m) => m.id === message.id);
      const idx = byClient >= 0 ? byClient : byId;
      if (idx >= 0) {
        mergedExisting = true;
        const next = [...state.messages];
        next[idx] = {
          ...next[idx],
          ...message,
          deliveryStatus: 'accepted',
          clientMessageId: message.clientMessageId ?? next[idx].clientMessageId,
        };
        return { messages: next };
      }
      return state;
    });

    const inOpenDm =
      !!selectedUser &&
      !!message.receiverId &&
      (message.senderId === selectedUser.id || message.receiverId === selectedUser.id);
    const inOpenGroup = !!selectedGroup && message.groupId === selectedGroup.id;
    const inSaved =
      showSavedMessages &&
      !!currentUser &&
      message.senderId === currentUser.id &&
      message.receiverId === currentUser.id;

    if (!mergedExisting && (inOpenDm || inOpenGroup || inSaved)) {
      get().addMessage({ ...message, deliveryStatus: message.deliveryStatus ?? 'accepted' });
    } else if (!mergedExisting && message.receiverId && message.senderId !== currentUser?.id) {
      set((state) => ({
        unreadCounts: {
          ...state.unreadCounts,
          [message.senderId]: (state.unreadCounts[message.senderId] || 0) + 1,
        },
      }));
    }

    if (message.receiverId) {
      get().fetchRecentChats();
      if (!users.some((u) => u.id === message.senderId)) {
        userService.getUsers().then((data) => set({ users: data.items }));
      }
    }
  },

  syncOpenConversation: async () => {
    const { selectedUser, selectedGroup, messages } = get();
    try {
      const last = messages.length
        ? messages.reduce((a, b) => (a.sentAt > b.sentAt ? a : b))
        : undefined;
      if (selectedUser) {
        const result = await messageService.syncMessages({
          peerUserId: selectedUser.id,
          afterSentAt: last?.sentAt,
          afterId: last?.id,
          limit: 100,
        });
        for (const msg of result.messages) get().ingestRealtimeMessage(msg);
      } else if (selectedGroup) {
        const result = await messageService.syncMessages({
          groupId: selectedGroup.id,
          afterSentAt: last?.sentAt,
          afterId: last?.id,
          limit: 100,
        });
        for (const msg of result.messages) get().ingestRealtimeMessage(msg);
      }
    } catch (error) {
      console.error('Conversation sync failed', error);
    }
  },

  setUsers: (users) => set({ users }),
  
  addMessage: (message) => set((state) => {
      // Avoid duplicates
      if (state.messages.some(m => m.id === message.id)) return state;
      return { messages: [...state.messages, message] };
  }),

  updateMessage: (id, updates) =>
    set((state) => ({
      messages: state.messages.map((msg) =>
        msg.id === id ? { ...msg, ...updates } : msg
      ),
    })),
  setMessageReactions: (id, reactions) =>
    set((state) => ({
      messages: state.messages.map((msg) =>
        msg.id === id ? { ...msg, reactions } : msg
      ),
    })),
  toggleMessageSaved: (messageId, isSaved) => {
    set((state) => ({
      messages: state.messages.map((msg) =>
        msg.id === messageId ? { ...msg, isSaved } : msg
      ),
    }));
  },
  updateUserStatus: (userId, isOnline) => set((state) => ({
    users: state.users.map((user) => 
      user.id === userId ? { ...user, isOnline } : user
    ),
    recentChats: state.recentChats.map((chat) =>
      chat.userId === userId ? { ...chat, isOnline } : chat
    )
  })),

  clearUnread: (id) => set((state) => {
      const newUnread = { ...state.unreadCounts };
      delete newUnread[id];
      return { unreadCounts: newUnread };
  }),

  fetchUnreadCounts: async () => {
      try {
          const counts = await messageService.getUnreadCounts();
          const unreadMap = counts.reduce((acc, curr) => {
              acc[curr.senderId] = curr.count;
              return acc;
          }, {} as Record<string, number>);
          set({ unreadCounts: unreadMap });
      } catch (error) {
          console.error("Failed to fetch unread counts", error);
      }
  },
  updateSelectedGroup: (updates) => set((state) => ({
    selectedGroup: state.selectedGroup ? { ...state.selectedGroup, ...updates } as Group : null
  })),
  clearSelection: () => {
      set({
          selectedUser: null,
          selectedGroup: null,
          showSavedMessages: false,
          showSystemMessages: false,
          messages: [],
      });
  },
}));
