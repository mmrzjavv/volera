import { useRef } from 'react';
import { useChatStore } from '../store/useChatStore';
import { useContactStore } from '../store/useContactStore';
import { useAuthStore } from '../store/useAuthStore';
import { useToastStore } from '../store/useToastStore';
import { messageService } from '../services/api';
import { MessageSquare, Bookmark, ShieldCheck, Trash2 } from 'lucide-react';
import { clsx } from 'clsx';
import type { RecentChat } from '../types';
import { getInitials } from '../utils/getInitials';
import { StoriesStrip } from './StoriesStrip';

const UNDO_DELAY_MS = 5000;

export const RecentChatList = () => {
  const { recentChats, selectUser, selectedUser, users, selectGroup, selectedGroup, groups, selectSavedMessages, selectSystemMessages, showSystemMessages, showSavedMessages, removeChatOptimistic, restoreChat, clearSelection } = useChatStore();
  const { contacts } = useContactStore();
  const { user: currentUser } = useAuthStore();
  const addToast = useToastStore((s) => s.addToast);
  const pendingRemoveRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const getChatKey = (chat: RecentChat) => chat.userId ?? chat.groupId ?? '';

  const handleRemoveChat = (e: React.MouseEvent, chat: RecentChat) => {
    e.stopPropagation();
    if (currentUser && chat.userId === currentUser.id) return;
    removeChatOptimistic(chat);
    if ((chat.userId && selectedUser?.id === chat.userId) || (chat.groupId && selectedGroup?.id === chat.groupId)) {
      clearSelection();
    }
    const key = getChatKey(chat);
    const timeoutId = setTimeout(() => {
      pendingRemoveRef.current.delete(key);
      messageService.removeChatFromRecent({ userId: chat.userId, groupId: chat.groupId }).catch((err) => {
        console.error('Failed to remove chat', err);
        restoreChat(chat);
      });
    }, UNDO_DELAY_MS);
    pendingRemoveRef.current.set(key, timeoutId);
    addToast('Chat removed', 'info', UNDO_DELAY_MS, {
      label: 'Undo',
      onClick: () => {
        const id = pendingRemoveRef.current.get(key);
        if (id) clearTimeout(id);
        pendingRemoveRef.current.delete(key);
        restoreChat(chat);
      },
    });
  };

  const handleChatClick = (chat: RecentChat) => {
      if (currentUser && chat.userId === currentUser.id) {
          selectSavedMessages();
          return;
      }

      if ((chat.isGroup || chat.isChannel) && chat.groupId) {
          const existingGroup = groups?.find(g => g.id === chat.groupId);
          const group = existingGroup || {
              id: chat.groupId,
              name: chat.name || (chat.isChannel ? 'Channel' : 'Unknown Group'),
              adminId: '',
              createdAt: '',
              isChannel: !!chat.isChannel,
              profilePictureUrl: chat.profilePicture,
              publicUsername: chat.publicUsername,
          };
          selectGroup(group as any);
      } else if (chat.userId) {
          // Find full user object if available, otherwise construct from chat info
          const existingUser = users?.find(u => u.id === chat.userId);
          
          const user = existingUser || {
              id: chat.userId,
              username: chat.username || chat.firstName,
              firstName: chat.firstName,
              lastName: chat.lastName,
              profilePicture: chat.profilePicture,
              email: '', 
              isOnline: chat.isOnline
          };
          
          selectUser(user as any);
      }
  };

  const formatTime = (dateStr: string) => {
      if (!dateStr || dateStr === '0001-01-01T00:00:00') return '';
      const date = new Date(dateStr);
      const now = new Date();
      
      const isToday = date.getDate() === now.getDate() && 
                      date.getMonth() === now.getMonth() && 
                      date.getFullYear() === now.getFullYear();
      
      if (isToday) {
          return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      }
      
      // Check if this week
      const diffTime = Math.abs(now.getTime() - date.getTime());
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)); 
      
      if (diffDays < 7) {
           return date.toLocaleDateString([], { weekday: 'short' });
      }

      return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
  };

  const getDisplayName = (chat: RecentChat) => {
      if (chat.isChannel) return chat.name || 'Channel';
      if (chat.isGroup) return chat.name || 'Group Chat';
      
      const contact = contacts?.find(c => c.contactUserId === chat.userId);
      if (contact) return contact.contactName;
      return `${chat.firstName} ${chat.lastName}`.trim() || chat.username || 'Unknown User';
  };

  return (
    <div className="flex-1 flex flex-col overflow-hidden bg-gray-100 dark:bg-gray-900 min-w-0">
      <div className="p-3 sm:p-4 border-b border-gray-300 dark:border-gray-700 flex justify-between items-center bg-gray-100 dark:bg-gray-900 shrink-0">
        <h2 className="font-bold text-base sm:text-lg text-gray-700 dark:text-gray-200 truncate">Chats</h2>
      </div>

      <StoriesStrip />

      <div className="flex-1 overflow-y-auto overflow-x-hidden min-h-0">
        {/* Application / System messages channel */}
        <div
          onClick={() => selectSystemMessages()}
          className={clsx(
            "group p-3 sm:p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 active:bg-gray-100 dark:active:bg-gray-700 transition-colors flex items-center gap-3 relative min-h-[56px] touch-manipulation",
            showSystemMessages && "bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]"
          )}
        >
          <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-emerald-100 dark:bg-emerald-900/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400 shrink-0">
            <ShieldCheck size={22} className="sm:w-6 sm:h-6" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex justify-between items-baseline gap-2 mb-0.5">
              <span className="font-medium text-gray-900 dark:text-white truncate text-sm sm:text-base">Application</span>
            </div>
            <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400 truncate">System announcements</p>
          </div>
        </div>
        {/* Saved Messages – always visible so it can be opened on mobile even when not in recent list */}
        <div
          onClick={() => selectSavedMessages()}
          className={clsx(
            "group p-3 sm:p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 active:bg-gray-100 dark:active:bg-gray-700 transition-colors flex items-center gap-3 relative min-h-[56px] touch-manipulation",
            showSavedMessages && "bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]"
          )}
        >
          <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-[var(--volera-accent)] flex items-center justify-center text-white shrink-0">
            <Bookmark size={22} className="sm:w-6 sm:h-6" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex justify-between items-baseline gap-2 mb-0.5">
              <span className="font-medium text-gray-900 dark:text-white truncate text-sm sm:text-base">Saved Messages</span>
            </div>
            <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400 truncate">Notes and bookmarks</p>
          </div>
        </div>
        {recentChats.length === 0 ? (
             <div className="p-8 text-center text-gray-400 dark:text-gray-500 flex flex-col items-center justify-center h-full">
                <div className="w-16 h-16 bg-gray-100 dark:bg-gray-700 rounded-full flex items-center justify-center mb-4">
                    <MessageSquare size={32} className="opacity-20 text-gray-600 dark:text-gray-400" />
                </div>
                <p className="font-medium text-gray-600 dark:text-gray-400">No recent chats</p>
                <p className="text-sm mt-1 text-gray-500 dark:text-gray-400">Start a conversation from your Contacts list.</p>
            </div>
        ) : (
            recentChats
                .filter((chat) => !(currentUser && chat.userId === currentUser.id))
                .map((chat) => (
                <div
                  key={chat.userId || chat.groupId}
                  onClick={() => handleChatClick(chat)}
                  className={clsx(
                    "group p-3 sm:p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 active:bg-gray-100 dark:active:bg-gray-700 transition-colors flex items-center gap-2 sm:gap-3 relative min-h-[56px] touch-manipulation",
                    (selectedUser?.id === chat.userId || selectedGroup?.id === chat.groupId || (showSavedMessages && currentUser?.id === chat.userId)) && "bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]"
                  )}
                >
                  <div className="relative shrink-0">
                    {currentUser?.id === chat.userId ? (
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-[var(--volera-accent)] flex items-center justify-center text-white">
                            <Bookmark size={20} className="sm:w-6 sm:h-6" />
                        </div>
                    ) : (
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-gray-200 dark:bg-gray-600 flex items-center justify-center text-gray-600 dark:text-gray-400 overflow-hidden font-bold text-sm sm:text-lg">
                             {chat.profilePicture ? (
                                 <img src={chat.profilePicture} alt={getDisplayName(chat)} className="w-full h-full object-cover" />
                             ) : (
                                 getInitials(getDisplayName(chat))
                             )}
                        </div>
                    )}
                    {chat.isOnline && !chat.isGroup && currentUser?.id !== chat.userId && (
                        <span className="absolute bottom-0 right-0 w-2.5 h-2.5 sm:w-3 sm:h-3 bg-green-500 border-2 border-white dark:border-gray-800 rounded-full"></span>
                    )}
                  </div>
                  <div className="flex-1 min-w-0 overflow-hidden">
                    <div className="flex items-baseline gap-2 mb-0.5">
                        <span className="font-medium text-gray-900 dark:text-white truncate text-sm sm:text-base">{currentUser?.id === chat.userId ? 'Saved Messages' : getDisplayName(chat)}</span>
                        <span className="text-[11px] sm:text-xs text-gray-400 dark:text-gray-500 shrink-0 whitespace-nowrap">{formatTime(chat.lastMessageAt)}</span>
                    </div>
                    <div className="flex items-center gap-2 min-w-0">
                        <p className={clsx(
                            "text-xs sm:text-sm truncate min-w-0",
                            chat.unreadCount > 0 ? "font-semibold text-gray-800 dark:text-gray-200" : "text-gray-500 dark:text-gray-400"
                        )}>
                            {chat.lastMessageContent}
                        </p>
                        {chat.unreadCount > 0 && (
                            <div className="w-5 h-5 rounded-full bg-[var(--volera-accent)] text-white text-[10px] flex items-center justify-center font-bold shrink-0 flex-shrink-0">
                                {chat.unreadCount}
                            </div>
                        )}
                    </div>
                  </div>
                  <button
                    onClick={(e) => handleRemoveChat(e, chat)}
                    className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center text-gray-400 dark:text-gray-500 hover:text-red-500 dark:hover:text-red-400 opacity-100 md:opacity-0 md:group-hover:opacity-100 transition-opacity touch-manipulation shrink-0"
                    title="Remove chat"
                  >
                    <Trash2 size={18} />
                  </button>
                </div>
            ))
        )}
      </div>
    </div>
  );
};
