import React, { useEffect, useState, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { useChatStore } from '../store/useChatStore';
import { useContactStore } from '../store/useContactStore';
import { useCallStore } from '../store/useCallStore';
import axios from 'axios';
import { userService, messageService, fileService, groupService, systemMessageService } from '../services/api';
import { MessageBubble } from '../components/chat/MessageBubble';
import { LogOut, Send, User as UserIcon, ArrowLeft, Phone, X, Users, MessageSquare, Paperclip, MessageCircle, Mic, Trash2, Video, Bookmark, Info, ShieldCheck, Loader2, CornerDownRight, ChevronRight, Bell, Shield, Moon } from 'lucide-react';
import { getInitials } from '../utils/getInitials';
import type { SystemMessage, Message, User, Group } from '../types';
import { clsx } from 'clsx';
import { CallModal } from '../components/CallModal';
import { CallHistory } from '../components/CallHistory';
import { ContactList } from '../components/ContactList';
import { RecentChatList } from '../components/RecentChatList';
import { GroupList } from '../components/GroupList';
import { CreateGroupModal } from '../components/CreateGroupModal';
import { CreateChannelModal } from '../components/CreateChannelModal';
import { ChannelInfoModal } from '../components/ChannelInfoModal';
import { AddContactModal } from '../components/AddContactModal';
import { useConfirmationStore } from '../store/useConfirmationStore';
import { useToastStore } from '../store/useToastStore';
import { useFileTransferStore } from '../store/useFileTransferStore';
import { useOnlineStatus } from '../hooks/useOnlineStatus';
import { NetworkStatusIndicator } from '../components/NetworkStatusIndicator';
import { FileTransferLoader } from '../components/ui/FileTransferLoader';
import { UserProfileModal } from '../components/UserProfileModal';
import { GroupInfoModal } from '../components/GroupInfoModal';
import { ImageViewer } from '../components/chat/ImageViewer';
import { getCachedImageBlobUrl } from '../utils/imageCache';
import { Profile } from './Profile';
import { splitMessage } from '../utils/messageSplitter';
import { requestCallMedia } from '../utils/mediaPermissions';

/** When user scrolls within this many px of the top, load next page of messages */
const SCROLL_LOAD_THRESHOLD = 80;
function SystemMessagesChannelContent({ onMarkRead }: { onMarkRead: (id: string) => Promise<void> }) {
  const [list, setList] = useState<SystemMessage[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    systemMessageService.getActive().then((data) => {
      if (!cancelled) setList((data as SystemMessage[]).filter((m) => m.isActive));
    }).catch(() => { if (!cancelled) setList([]); }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);
  const onMarkReadRef = useRef(onMarkRead);
  onMarkReadRef.current = onMarkRead;
  useEffect(() => {
    list.filter((m) => !m.isRead).forEach((m) => { onMarkReadRef.current(m.id).catch(() => {}); });
  }, [list]);

  if (loading) {
    return (
      <div className="flex justify-center items-center h-full text-gray-500 dark:text-gray-400">Loading...</div>
    );
  }
  if (list.length === 0) {
    return (
      <div className="flex justify-center items-center h-full text-gray-400 dark:text-gray-500">No active system messages.</div>
    );
  }
  return (
    <div className="space-y-4">
      {list.map((m) => (
          <div key={m.id} className="flex flex-col items-start gap-1">
            <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
              <span className="font-medium text-gray-800 dark:text-gray-200">Application</span>
              <span title="Verified"><ShieldCheck size={14} className="text-emerald-500 dark:text-emerald-400 shrink-0" /></span>
            </div>
            <div className="rounded-2xl rounded-tl-md px-4 py-3 max-w-[85%] bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-200 shadow-sm">
              {m.title && <p className="font-semibold text-sm mb-1">{m.title}</p>}
              <p className="text-sm whitespace-pre-wrap break-words">{m.content}</p>
              {m.expiresAt && (
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">Expires: {new Date(m.expiresAt).toLocaleString()}</p>
              )}
            </div>
          </div>
        ))}
    </div>
  );
}

export function Chat() {
  const { addTransfer, updateProgress, completeTransfer, failTransfer, removeTransfer } = useFileTransferStore();
  const { openDialog } = useConfirmationStore();
  const { addToast } = useToastStore();
  const isOnline = useOnlineStatus();
  const { user, logout } = useAuthStore();
  const { initializeCallConnection, initiateCall, initiateGroupCall } = useCallStore();
  const { 
    selectedUser,
    selectedGroup, 
    users,
    groups,
    channels,
    recentChats,
    messages, 
    initializeConnection, 
    disconnect, 
    sendMessage, 
    setUsers,
    fetchGroups,
    fetchChannels,
    fetchRecentChats,
    isLoadingMessages,
    hasMoreMessages,
    loadMoreMessages,
    showSavedMessages,
    showSystemMessages,
    toggleMessageSaved,
    clearSelection,
    selectGroupById,
    updateSelectedGroup,
  } = useChatStore();
  const location = useLocation();
  const navigate = useNavigate();
  const { contacts } = useContactStore();
  const [messageInput, setMessageInput] = useState('');
  const [editingMessage, setEditingMessage] = useState<Message | null>(null);
  const [replyingTo, setReplyingTo] = useState<Message | null>(null);
  const [messageLengthLimit, setMessageLengthLimit] = useState<number>(2000); // Default to 2000
  const [activeTab, setActiveTab] = useState<'chats' | 'contacts' | 'groups' | 'calls' | 'profile'>('chats');
  const [profileSection, setProfileSection] = useState<'profile' | 'appearance' | 'notifications' | 'security'>('profile');
  const [isCreateGroupModalOpen, setIsCreateGroupModalOpen] = useState(false);
  const [isCreateChannelModalOpen, setIsCreateChannelModalOpen] = useState(false);
  const [isChannelInfoOpen, setIsChannelInfoOpen] = useState(false);
  const [sendAsChannelId, setSendAsChannelId] = useState('');
  const [sendAsOptions, setSendAsOptions] = useState<Group[]>([]);
  const [isAddContactModalOpen, setIsAddContactModalOpen] = useState(false);
  const [viewedImageMessage, setViewedImageMessage] = useState<Message | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingDuration, setRecordingDuration] = useState(0);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const recordingTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const mimeTypeRef = useRef<string>('');
  
  // Scroll restoration state
  const [isRestoringScroll, setIsRestoringScroll] = useState(false);
  const [oldScrollHeight, setOldScrollHeight] = useState(0);
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  // Open group when navigating from invite page after join
  const openGroupId = (location.state as { openGroupId?: string } | null)?.openGroupId;
  useEffect(() => {
    if (!openGroupId) return;
    Promise.all([fetchGroups(), fetchChannels()])
      .then(() => selectGroupById(openGroupId))
      .then(() => setActiveTab('groups'))
      .finally(() => navigate('.', { state: {}, replace: true }));
  }, [openGroupId]);

  useEffect(() => {
    setSendAsOptions(channels.filter((c) => c.isPublic && c.canPost));
  }, [channels]);

  const getSupportedMimeType = () => {
      const types = [
          'audio/webm;codecs=opus',
          'audio/webm',
          'audio/mp4',
          'audio/aac',
          'audio/ogg',
          'audio/wav'
      ];
      for (const type of types) {
          if (MediaRecorder.isTypeSupported(type)) {
              return type;
          }
      }
      return '';
  };

  const fileInputRef = useRef<HTMLInputElement>(null);
  const messageInputRef = useRef<HTMLTextAreaElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [profileUser, setProfileUser] = useState<User | null>(null);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [isGroupInfoOpen, setIsGroupInfoOpen] = useState(false);
  const [groupMembers, setGroupMembers] = useState<User[]>([]);
  const [groupInviteCode, setGroupInviteCode] = useState<string | null>(null);
  const [forwardingMessage, setForwardingMessage] = useState<Message | null>(null);
  const [forwardingMessages, setForwardingMessages] = useState<Message[]>([]);
  const [forwardingTo, setForwardingTo] = useState<string | null>(null);
  const [selectionMode, setSelectionMode] = useState(false);
  const [selectedMessageIds, setSelectedMessageIds] = useState<Set<string>>(new Set());
  const [isSending, setIsSending] = useState(false);
  const [isGroupDetailsLoading, setIsGroupDetailsLoading] = useState(false);
  const [groupActionInProgress, setGroupActionInProgress] = useState<string | null>(null);
  const [pendingReactionMessageId, setPendingReactionMessageId] = useState<string | null>(null);
  const [pendingSaveMessageId, setPendingSaveMessageId] = useState<string | null>(null);
  const [pendingPinMessageId, setPendingPinMessageId] = useState<string | null>(null);

  useEffect(() => {
    initializeConnection();
    initializeCallConnection();
    
    // Fetch users and groups
    userService.getUsers().then(data => setUsers(data.items)).catch(console.error);
    fetchGroups();
    fetchChannels();
    fetchRecentChats();

    return () => {
        disconnect();
    };
  }, []);

  // Fetch message length limit on mount (guard against StrictMode double-invoke in dev)
  const messageLimitFetchedRef = useRef(false);
  useEffect(() => {
    const enableLimitFetch = import.meta.env.VITE_ENABLE_MESSAGE_LENGTH_LIMIT === 'true';
    if (!enableLimitFetch) return;
    if (messageLimitFetchedRef.current) return;
    messageLimitFetchedRef.current = true;
    userService.getMessageLengthLimit()
      .then(limit => setMessageLengthLimit(limit))
      .catch(() => {
        // If fetch fails, keep default 2000
        console.warn('Failed to fetch message length limit, using default 2000');
      });
  }, []);

  // Refresh data when app resumes from background (PWA/tab switch)
  useEffect(() => {
    let debounceTimer: ReturnType<typeof setTimeout>;
    const handleVisibilityChange = () => {
      if (document.visibilityState !== 'visible') return;
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        fetchRecentChats();
        useChatStore.getState().fetchUnreadCounts();
      }, 300);
    };
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      clearTimeout(debounceTimer);
    };
  }, [fetchRecentChats]);

  // Auto-grow textarea up to CSS max-height (responsive), then scroll
  const resizeMessageInput = () => {
    const ta = messageInputRef.current;
    if (!ta) return;
    ta.style.height = '0px';
    const maxH = parseFloat(getComputedStyle(ta).maxHeight);
    const limit = Number.isFinite(maxH) && maxH > 0 ? maxH : 200;
    const next = Math.min(ta.scrollHeight, limit);
    ta.style.height = `${next}px`;
    ta.style.overflowY = ta.scrollHeight > limit ? 'auto' : 'hidden';
  };

  useEffect(() => {
    resizeMessageInput();
  }, [messageInput]);

  useEffect(() => {
    const onResize = () => resizeMessageInput();
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  // Load group details (e.g. profile picture) when a group is selected
  useEffect(() => {
    if (!selectedGroup?.id || selectedGroup.profilePictureUrl !== undefined) return;
    groupService.getGroupDetails(selectedGroup.id).then((details: any) => {
      updateSelectedGroup({ profilePictureUrl: details?.profilePictureUrl ?? undefined });
    }).catch(() => {});
  }, [selectedGroup?.id]);

  const getDisplayName = (u: any) => {
      if (!u) return '';
      const contact = contacts.find(c => c.contactUserId === u.id);
      if (contact) return contact.contactName;
      return `${u.firstName} ${u.lastName}`.trim() || u.username || 'Unknown User';
  };

  const lastMessageIdRef = useRef<string | null>(null);

  useEffect(() => {
    const lastMessage = messages[messages.length - 1];
    const isLastMessageNew = lastMessage && lastMessage.id !== lastMessageIdRef.current;
    
    // Always update the ref
    lastMessageIdRef.current = lastMessage?.id || null;

    if (messagesEndRef.current && !isRestoringScroll) {
      // Only scroll to bottom if the last message is new (prevents scrolling down when loading older messages)
      if (isLastMessageNew) {
        messagesEndRef.current.scrollIntoView({ behavior: 'smooth' });
      }
    }
  }, [messages, isRestoringScroll]);

  // Restore scroll position after loading more messages
  useEffect(() => {
    if (isRestoringScroll && !isLoadingMessages && scrollContainerRef.current) {
        const container = scrollContainerRef.current;
        const newScrollHeight = container.scrollHeight;
        container.scrollTop = newScrollHeight - oldScrollHeight;
        setIsRestoringScroll(false);
    }
  }, [messages, isLoadingMessages, isRestoringScroll, oldScrollHeight]);

  // Pagination: load next 20 older messages when user scrolls near the top
  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
      const { scrollTop, scrollHeight } = e.currentTarget;
      if (scrollTop <= SCROLL_LOAD_THRESHOLD && hasMoreMessages && !isLoadingMessages) {
          setOldScrollHeight(scrollHeight);
          setIsRestoringScroll(true);
          loadMoreMessages();
      }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageInput.trim() || (!selectedUser && !selectedGroup && !showSavedMessages) || isSending || !isOnline) return;
    
    setIsSending(true);
    try {
      if (editingMessage) {
        // For editing, don't split - just validate length
        if (messageInput.length > messageLengthLimit) {
          addToast(`Message is too long (max ${messageLengthLimit} characters). Please shorten your message.`, "error");
          setIsSending(false);
          return;
        }
        await messageService.editMessage(editingMessage.id, messageInput);
        setEditingMessage(null);
        setMessageInput('');
      } else {
        // Split message if it exceeds the limit
        const messageChunks = splitMessage(messageInput, messageLengthLimit);
        
        if (messageChunks.length > 1) {
          // Show info toast when splitting
          addToast(`Message is long and will be sent as ${messageChunks.length} separate messages.`, "info");
        }
        
        // Send all chunks sequentially
        // Only attach replyTo to the first message
        for (let i = 0; i < messageChunks.length; i++) {
          const chunk = messageChunks[i];
          const replyToId = i === 0 ? replyingTo?.id : undefined;
          await sendMessage(chunk, undefined, undefined, replyToId, sendAsChannelId || undefined);
          
          // Small delay between messages to ensure proper ordering
          if (i < messageChunks.length - 1) {
            await new Promise(resolve => setTimeout(resolve, 100));
          }
        }
        
        setMessageInput('');
        setReplyingTo(null);
      }
    } catch (error) {
      console.error("Failed to send/edit message", error);
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (errorMessage.includes('exceeds maximum length')) {
        addToast(errorMessage, "error");
      } else {
        addToast(editingMessage ? "Failed to edit message" : "Failed to send message. Please try again.", "error");
      }
      // Keep messageInput and replyingTo on error so user can retry
    } finally {
      setIsSending(false);
    }
  };

  const startEditing = (msg: Message) => {
      setEditingMessage(msg);
      setMessageInput(msg.content);
      // setActiveMessageId(null);
  };

  const startReplying = (msg: Message) => {
      setReplyingTo(msg);
      // Do not overwrite current input; user may want to keep what they typed
  };

  const cancelEditing = () => {
      setEditingMessage(null);
      setMessageInput('');
  };

  const deleteMessage = async (msgId: string) => {
      openDialog({
          title: 'Delete Message',
          message: 'Are you sure you want to delete this message? This action cannot be undone.',
          confirmText: 'Delete',
          variant: 'danger',
          onConfirm: async () => {
              try {
                  await messageService.deleteMessage(msgId);
                  // setActiveMessageId(null);
              } catch (error) {
                  console.error("Failed to delete message", error);
              }
          }
      });
  };

  const reactToMessage = async (message: Message, emoji: string) => {
      setPendingReactionMessageId(message.id);
      try {
          const myUserId = user?.id;
          const hasSameReaction = message.reactions?.some(r => r.userId === myUserId && r.emoji === emoji);
          if (hasSameReaction) {
              await messageService.removeReaction(message.id);
          } else {
              await messageService.addOrUpdateReaction(message.id, emoji);
          }
          // Store will be updated by SignalR "MessageReactionsUpdated"
      } catch (error) {
          console.error("Failed to react to message", error);
          addToast("Failed to react", "error");
      } finally {
          setPendingReactionMessageId(null);
      }
  };

  const startForwarding = (msg: Message) => {
      setForwardingMessage(msg);
      setForwardingMessages([]);
  };

  const startSelectionMode = (msg: Message) => {
      setSelectionMode(true);
      setSelectedMessageIds(new Set([msg.id]));
  };

  const toggleMessageSelection = (messageId: string) => {
      setSelectedMessageIds((prev) => {
          const next = new Set(prev);
          if (next.has(messageId)) next.delete(messageId);
          else next.add(messageId);
          return next;
      });
  };

  const exitSelectionMode = () => {
      setSelectionMode(false);
      setSelectedMessageIds(new Set());
  };

  const forwardSelectedMessages = () => {
      const msgs = messages.filter((m) => selectedMessageIds.has(m.id));
      if (msgs.length === 0) return;
      setForwardingMessages(msgs);
      setForwardingMessage(msgs[0]);
      exitSelectionMode();
  };

  const deleteSelectedMessages = () => {
      const ids = Array.from(selectedMessageIds);
      if (ids.length === 0) return;
      openDialog({
          title: 'Delete messages',
          message: `Delete ${ids.length} message(s)?`,
          confirmText: 'Delete',
          variant: 'danger',
          onConfirm: async () => {
              for (const id of ids) await messageService.deleteMessage(id);
              exitSelectionMode();
          },
      });
  };

  const handleCopyImage = async (message: Message) => {
      if (!message.attachmentUrl || !message.attachmentType?.startsWith('image/')) return;
      try {
          const response = await fetch(message.attachmentUrl, { mode: 'cors' });
          const blob = await response.blob();
          await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
          addToast('Image copied to clipboard', 'success');
      } catch (err) {
          console.warn('Copy image failed:', err);
          addToast('Could not copy image', 'error');
      }
  };

  const handleImageClick = (message: Message) => {
      if (!message.attachmentUrl || !message.attachmentType?.startsWith('image/')) return;
      setViewedImageMessage(message);
  };

  const handleImageViewerDownload = async () => {
      const msg = viewedImageMessage;
      if (!msg?.attachmentUrl) return;
      const url = msg.attachmentUrl;
      try {
          const size = await fileService.checkFileSize(url);
          const isHeavy = size > 1024 * 1024;
          if (!isHeavy) {
              window.open(url, '_blank');
              return;
          }
          const cancelTokenSource = axios.CancelToken.source();
          addTransfer(url, 'download', () => cancelTokenSource.cancel());
          const blob = await fileService.downloadFile(url, (p) => updateProgress(url, p), cancelTokenSource.token);
          completeTransfer(url);
          const objectUrl = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = objectUrl;
          link.download = url.split('/').pop()?.split('_').slice(1).join('_') || 'image';
          document.body.appendChild(link);
          link.click();
          link.remove();
          window.URL.revokeObjectURL(objectUrl);
          setTimeout(() => removeTransfer(url), 2000);
      } catch (err) {
          if (axios.isCancel(err)) {
              removeTransfer(url);
          } else {
              failTransfer(url, 'Download failed');
              addToast('Download failed', 'error');
          }
      }
  };

  const forwardToUser = async (targetUserId: string) => {
      const toForward = forwardingMessages.length > 0 ? forwardingMessages : (forwardingMessage ? [forwardingMessage] : []);
      if (toForward.length === 0 || forwardingTo) return;
      setForwardingTo(`user-${targetUserId}`);
      try {
          for (const msg of toForward) {
              await messageService.forwardMessage(msg.id, { receiverId: targetUserId });
          }
          setForwardingMessage(null);
          setForwardingMessages([]);
      } catch (error) {
          console.error("Failed to forward message", error);
          addToast("Failed to forward message", "error");
      } finally {
          setForwardingTo(null);
      }
  };

  const forwardToGroup = async (groupId: string) => {
      const toForward = forwardingMessages.length > 0 ? forwardingMessages : (forwardingMessage ? [forwardingMessage] : []);
      if (toForward.length === 0 || forwardingTo) return;
      setForwardingTo(`group-${groupId}`);
      try {
          for (const msg of toForward) {
              await messageService.forwardMessage(msg.id, { groupId });
          }
          setForwardingMessage(null);
          setForwardingMessages([]);
      } catch (error) {
          console.error("Failed to forward message", error);
          addToast("Failed to forward message", "error");
      } finally {
          setForwardingTo(null);
      }
  };

  const handleSaveMessage = async (messageId: string, isSaved: boolean) => {
      setPendingSaveMessageId(messageId);
      try {
          if (isSaved) {
              await messageService.unsaveMessage(messageId);
              toggleMessageSaved(messageId, false);
          } else {
              await messageService.saveMessage(messageId);
              toggleMessageSaved(messageId, true);
          }
      } catch (error) {
          console.error("Failed to toggle save message", error);
          addToast("Failed to save message", "error");
      } finally {
          setPendingSaveMessageId(null);
      }
  };

  const togglePinMessage = async (message: Message) => {
      setPendingPinMessageId(message.id);
      try {
          if (message.isPinned) {
              await messageService.unpinMessage(message.id);
          } else {
              await messageService.pinMessage(message.id);
          }
          // Realtime event will update local state
      } catch (error) {
          console.error("Failed to toggle pin", error);
          addToast("Failed to pin message", "error");
      } finally {
          setPendingPinMessageId(null);
      }
  };

  const scrollToMessage = (messageId: string) => {
      const el = document.getElementById(`message-${messageId}`);
      if (el && scrollContainerRef.current) {
          el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
  };

  const startRecording = async (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      try {
          // First await must be getUserMedia (same gesture rules as calls)
          const media = await requestCallMedia(false);
          if (!media.ok) {
              addToast(media.message, 'error');
              return;
          }
          const stream = media.stream;
          const mimeType = getSupportedMimeType();
          mimeTypeRef.current = mimeType;
          console.log('Using MIME type:', mimeType || 'default');
          
          const options = mimeType ? { mimeType } : undefined;
          const mediaRecorder = new MediaRecorder(stream, options);
          mediaRecorderRef.current = mediaRecorder;
          audioChunksRef.current = [];

          mediaRecorder.ondataavailable = (event) => {
              if (event.data.size > 0) {
                  audioChunksRef.current.push(event.data);
              }
          };

          mediaRecorder.start();
          setIsRecording(true);
          setRecordingDuration(0);
          
          recordingTimerRef.current = setInterval(() => {
              setRecordingDuration(prev => prev + 1);
          }, 1000);

      } catch (err) {
          console.error("Error accessing microphone:", err);
          addToast("Could not access microphone", "error");
      }
  };

  const stopRecording = async (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      console.log('Stopping recording...');
      if (!mediaRecorderRef.current || mediaRecorderRef.current.state === 'inactive') {
          console.warn('MediaRecorder not active');
          return;
      }

      // If duration is too short (< 1s), cancel instead
      if (recordingDuration < 1 && audioChunksRef.current.length === 0) { // Check chunks too just in case
           console.log('Recording too short, cancelling');
           cancelRecording();
           return;
      }

      return new Promise<void>((resolve) => {
          mediaRecorderRef.current!.onstop = async () => {
              console.log('MediaRecorder stopped, chunks:', audioChunksRef.current.length);
              
              const usedMimeType = mediaRecorderRef.current!.mimeType || mimeTypeRef.current || 'audio/webm';
              // Determine extension
              let extension = 'webm';
              if (usedMimeType.includes('mp4')) extension = 'mp4';
              else if (usedMimeType.includes('aac')) extension = 'aac';
              else if (usedMimeType.includes('ogg')) extension = 'ogg';
              else if (usedMimeType.includes('wav')) extension = 'wav';
              
              const audioBlob = new Blob(audioChunksRef.current, { type: usedMimeType });
              console.log('Audio blob size:', audioBlob.size, 'Type:', usedMimeType);
              
              // Don't send empty or super small files
              if (audioBlob.size < 100) {
                  console.warn('Audio blob too small');
                  cancelRecording();
                  resolve();
                  return;
              }

              setIsUploading(true);
              const fileName = `voice_${Date.now()}.${extension}`;
              addTransfer(fileName, 'upload');
              try {
                  // Upload via API → MinIO (avoids browser CORS to MinIO for voice blobs).
                  const voiceFile = new File([audioBlob], fileName, { type: usedMimeType });
                  const { attachmentRef, url } = await fileService.upload(voiceFile, (progress) => {
                      updateProgress(fileName, progress);
                  });
                  console.log('Voice uploaded to MinIO');
                  
                  completeTransfer(fileName);
                  await sendMessage("Voice Message", attachmentRef || url, usedMimeType);
                  console.log('Message sent');
              } catch (error) {
                  console.error("Failed to send voice message", error);
                  alert("Failed to send voice message: " + (error instanceof Error ? error.message : String(error)));
                  failTransfer(fileName, 'Failed to upload voice message');
              } finally {
                  setIsUploading(false);
              }
              
              const tracks = mediaRecorderRef.current?.stream.getTracks();
              tracks?.forEach(track => track.stop());
              resolve();
          };
          mediaRecorderRef.current!.stop();
          setIsRecording(false);
          setRecordingDuration(0);
          if (recordingTimerRef.current) {
              clearInterval(recordingTimerRef.current);
              recordingTimerRef.current = null;
          }
      });
  };

  const cancelRecording = () => {
      if (mediaRecorderRef.current) {
          mediaRecorderRef.current.onstop = null;
          mediaRecorderRef.current.stop();
          const tracks = mediaRecorderRef.current.stream.getTracks();
          tracks?.forEach(track => track.stop());
      }
      setIsRecording(false);
      setRecordingDuration(0);
      if (recordingTimerRef.current) {
          clearInterval(recordingTimerRef.current);
          recordingTimerRef.current = null;
      }
  };

  const formatDuration = (seconds: number) => {
      const mins = Math.floor(seconds / 60);
      const secs = seconds % 60;
      return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (!file) return;

      if (file.size > 50 * 1024 * 1024) { // 50MB limit
          alert("File size too large (max 50MB)");
          return;
      }

      const uploadId = `${file.name}-${Date.now()}`;
      addTransfer(uploadId, 'upload');

      setIsUploading(true);
      try {
          const { attachmentRef, url } = await fileService.upload(file, (progress) => {
              updateProgress(uploadId, progress);
          });
          
          completeTransfer(uploadId);
          
          // Prefer durable object key; fall back to download URL.
          await sendMessage(messageInput, attachmentRef || url, file.type);
          setMessageInput('');
      } catch (error) {
          console.error("Failed to upload file", error);
          failTransfer(uploadId, error instanceof Error ? error.message : 'Upload failed');
      } finally {
          setIsUploading(false);
          if (fileInputRef.current) fileInputRef.current.value = '';
      }
  };

  const openUserProfile = async (userId: string) => {
      try {
          const profile = await userService.getPublicProfile(userId);
          setProfileUser(profile as User);
          setIsProfileOpen(true);
      } catch (error) {
          console.error("Failed to load profile", error);
      }
  };

  const loadGroupDetails = async (groupId: string) => {
      setIsGroupDetailsLoading(true);
      try {
          const details = await groupService.getGroupDetails(groupId);
          setGroupMembers((details.members as User[]) || []);
          setGroupInviteCode(details.inviteCode || null);
          updateSelectedGroup({ profilePictureUrl: details.profilePictureUrl ?? undefined });
      } catch (error) {
          console.error("Failed to load group details", error);
          addToast("Failed to load group info. Please try again.", "error");
      } finally {
          setIsGroupDetailsLoading(false);
      }
  };

  return (
    <div className={clsx(
      "volera-shell flex h-screen max-h-[100dvh] overflow-hidden [padding:env(safe-area-inset-top)_env(safe-area-inset-right)_env(safe-area-inset-bottom)_env(safe-area-inset-left)]",
      activeTab === 'profile' ? "flex-col md:flex-row" : ""
    )}>
      {/* Sidebar - User List */}
      <div className={clsx(
        "w-full md:w-80 flex-shrink-0 bg-gray-100 dark:bg-gray-900 border-r border-gray-300 dark:border-gray-700 flex flex-col min-w-0 max-w-full min-h-0",
        (selectedUser || selectedGroup || showSavedMessages || showSystemMessages) && activeTab !== 'profile' && activeTab !== 'calls' ? "hidden md:flex" : "flex",
        activeTab === 'profile' ? "max-h-[45vh] md:max-h-none overflow-hidden" : "overflow-hidden"
      )}>
        <div className="p-3 sm:p-4 border-b border-gray-300 dark:border-gray-700 flex items-center gap-2 sm:gap-3 bg-gray-100 dark:bg-gray-900 shrink-0">
          {/* Logo + app name */}
          <div className="flex items-center gap-2 shrink-0 min-w-0">
            <img src="/icon.svg" alt="Volera" className="w-6 h-6 sm:w-7 sm:h-7 rounded-lg shrink-0" />
            <span className="font-semibold text-gray-800 dark:text-gray-200 text-sm truncate hidden sm:block">Volera</span>
          </div>
          <div className="flex-1 min-w-0" />
          <div className="flex items-center gap-1.5 shrink-0">
            <NetworkStatusIndicator />
            <button onClick={logout} className="p-2.5 min-w-[44px] min-h-[44px] flex items-center justify-center text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded-lg transition-colors touch-manipulation -mr-1" title="Logout" aria-label="Log out">
              <LogOut size={20} />
            </button>
          </div>
        </div>
        
        <div className="flex border-b border-gray-300 dark:border-gray-700 shrink-0">
          <button
            onClick={() => setActiveTab('chats')}
            className={clsx(
              "flex-1 min-h-[48px] py-3 flex justify-center items-center border-b-2 transition-colors touch-manipulation",
              activeTab === 'chats' ? "border-[var(--volera-accent)] text-[var(--volera-accent)]" : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300"
            )}
            title="Chats"
            aria-label="Chats"
          >
            <MessageCircle size={20} />
          </button>
          <button
            onClick={() => setActiveTab('contacts')}
            className={clsx(
              "flex-1 min-h-[48px] py-3 flex justify-center items-center border-b-2 transition-colors touch-manipulation",
              activeTab === 'contacts' ? "border-[var(--volera-accent)] text-[var(--volera-accent)]" : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300"
            )}
            title="Contacts"
            aria-label="Contacts"
          >
            <Users size={20} />
          </button>
          <button
            onClick={() => setActiveTab('groups')}
            className={clsx(
              "flex-1 min-h-[48px] py-3 flex justify-center items-center border-b-2 transition-colors touch-manipulation",
              activeTab === 'groups' ? "border-[var(--volera-accent)] text-[var(--volera-accent)]" : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300"
            )}
            title="Groups"
            aria-label="Groups"
          >
            <MessageSquare size={20} />
          </button>
          <button
            onClick={() => setActiveTab('calls')}
            className={clsx(
              "flex-1 min-h-[48px] py-3 flex justify-center items-center border-b-2 transition-colors touch-manipulation",
              activeTab === 'calls' ? "border-[var(--volera-accent)] text-[var(--volera-accent)]" : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300"
            )}
            title="Calls"
            aria-label="Calls"
          >
            <Phone size={20} />
          </button>
          <button
            onClick={() => setActiveTab('profile')}
            className={clsx(
              "flex-1 min-h-[48px] py-3 flex justify-center items-center border-b-2 transition-colors touch-manipulation",
              activeTab === 'profile' ? "border-[var(--volera-accent)] text-[var(--volera-accent)]" : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300"
            )}
            title="Profile"
            aria-label="Profile"
          >
            <UserIcon size={20} />
          </button>
        </div>

        {activeTab === 'profile' ? (
          <div className="flex-1 flex flex-col overflow-y-auto min-h-0 bg-gray-200 dark:bg-gray-950">
            <div className="p-3 overflow-y-auto flex-1 min-h-0">
              <div className="bg-gray-100 dark:bg-gray-900 rounded-xl overflow-hidden shadow-sm border border-gray-300 dark:border-gray-700">
                <div className="p-4 flex items-center gap-4 border-b border-gray-300 dark:border-gray-700">
                  <div className="w-14 h-14 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-600 flex-shrink-0">
                    {user?.profilePicture ? (
                      <img src={user.profilePicture} alt="" className="w-full h-full object-cover" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-500 dark:text-gray-400 text-xl font-semibold">
                        {user?.firstName?.[0]}{user?.lastName?.[0]}
                      </div>
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <h3 className="font-semibold text-gray-900 dark:text-white truncate">{user?.firstName} {user?.lastName}</h3>
                    <p className="text-sm text-gray-500 dark:text-gray-400 truncate">{user?.email}</p>
                  </div>
                </div>
                <div className="divide-y divide-gray-300 dark:divide-gray-700">
                  {[
                    { id: 'profile' as const, label: 'Edit Profile', icon: UserIcon },
                    { id: 'appearance' as const, label: 'Appearance', icon: Moon },
                    { id: 'notifications' as const, label: 'Notifications', icon: Bell },
                    { id: 'security' as const, label: 'Security', icon: Shield },
                  ].map(({ id, label, icon: Icon }) => (
                    <button
                      key={id}
                      onClick={() => setProfileSection(id)}
                      className={clsx(
                        "w-full flex items-center gap-3 px-4 py-3 text-left transition-colors",
                        profileSection === id ? "bg-[var(--volera-accent)]/10 text-[var(--volera-accent)]" : "text-gray-800 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/50 active:bg-gray-100 dark:active:bg-gray-700"
                      )}
                    >
                      <Icon size={20} className="flex-shrink-0 text-gray-500 dark:text-gray-400" />
                      <span className="flex-1 font-medium">{label}</span>
                      <ChevronRight size={18} className="text-gray-400 dark:text-gray-500" />
                    </button>
                  ))}
                </div>
                <div className="border-t border-gray-300 dark:border-gray-700">
                  <button
                    onClick={logout}
                    className="w-full flex items-center gap-3 px-4 py-3 text-left text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 active:bg-red-100 dark:active:bg-red-900/30 transition-colors"
                  >
                    <LogOut size={20} className="flex-shrink-0" />
                    <span className="flex-1 font-medium">Log out</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        ) : activeTab === 'chats' ? (
          <RecentChatList />
        ) : activeTab === 'contacts' ? (
          <ContactList />
        ) : activeTab === 'calls' ? (
          <CallHistory embedded />
        ) : (
          <GroupList
            onCreateGroup={() => setIsCreateGroupModalOpen(true)}
            onCreateChannel={() => setIsCreateChannelModalOpen(true)}
          />
        )}
      </div>

      {/* Main Chat Area */}
      <div className={clsx(
        "flex-1 flex flex-col min-w-0 min-h-0",
        (!selectedUser && !selectedGroup && !showSavedMessages && !showSystemMessages && activeTab !== 'profile') ? "hidden md:flex" : "flex",
        activeTab === 'profile' ? "md:min-h-0" : ""
      )}>
        <FileTransferLoader />
        {activeTab === 'profile' ? (
          <div className="flex-1 flex flex-col min-h-0 overflow-hidden bg-gray-200 dark:bg-gray-950">
            <Profile
              embedded
              activeSubTab={profileSection}
              onSubTabChange={setProfileSection}
            />
          </div>
        ) : (selectedUser || selectedGroup || showSavedMessages || showSystemMessages) ? (
          <>
            <div className="p-3 sm:p-4 border-b border-gray-300 dark:border-gray-700 bg-gray-100 dark:bg-gray-900 flex items-center gap-2 sm:gap-3 min-h-0 shadow-sm z-10">
               <button 
                 onClick={clearSelection} 
                 className="md:hidden p-2 -ml-2 shrink-0 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white rounded-lg"
                 aria-label="Back"
               >
                 <ArrowLeft size={20} />
               </button>
               
               {showSystemMessages ? (
                   <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
                        <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-emerald-100 dark:bg-emerald-900/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400 shrink-0">
                            <ShieldCheck size={20} className="shrink-0" />
                        </div>
                        <div className="flex items-center gap-2 min-w-0 flex-1">
                            <h3 className="font-bold text-base sm:text-lg text-gray-800 dark:text-white truncate">Application</h3>
                            <span title="Verified" className="shrink-0"><ShieldCheck size={18} className="text-emerald-500 dark:text-emerald-400" /></span>
                        </div>
                   </div>
               ) : showSavedMessages ? (
                   <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
                        <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-[var(--volera-accent)] flex items-center justify-center text-white shrink-0">
                            <Bookmark size={20} className="shrink-0" />
                        </div>
                        <div className="min-w-0 flex-1">
                            <h3 className="font-bold text-base sm:text-lg text-gray-800 dark:text-white truncate">Saved Messages</h3>
                            <p className="text-xs text-gray-500 dark:text-gray-400 truncate hidden sm:block">Cloud Storage</p>
                        </div>
                   </div>
               ) : selectedUser ? (
                   <>
                       <div className="relative shrink-0">
                          <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] overflow-hidden font-bold text-sm">
                            {selectedUser.profilePicture ? (
                              <img src={selectedUser.profilePicture} alt={getDisplayName(selectedUser)} className="w-full h-full object-cover" />
                            ) : (
                              getInitials(getDisplayName(selectedUser))
                            )}
                          </div>
                          {selectedUser.isOnline && (
                              <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white dark:border-gray-800 rounded-full"></span>
                          )}
                       </div>
                      <h3 className="font-bold text-base sm:text-lg text-gray-800 dark:text-white flex-1 min-w-0 truncate">{getDisplayName(selectedUser)}</h3>
                      <button 
                        onClick={() => openUserProfile(selectedUser.id)}
                        className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors"
                        title="View Profile"
                      >
                        <Info size={22} />
                      </button>
                      <button 
                        onClick={() => initiateCall(selectedUser.id, getDisplayName(selectedUser), false, selectedUser.profilePicture ?? undefined)}
                        className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 dark:hover:bg-gray-700 rounded-full transition-colors"
                        title="Start Voice Call"
                      >
                        <Phone size={24} />
                      </button>
                      <button 
                        onClick={() => initiateCall(selectedUser.id, getDisplayName(selectedUser), true, selectedUser.profilePicture ?? undefined)}
                        className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 dark:hover:bg-gray-700 rounded-full transition-colors"
                        title="Start Video Call"
                      >
                        <Video size={24} />
                      </button>
                   </>
               ) : (
               <>
                       <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-[var(--volera-accent)]/15 dark:bg-gray-700 flex items-center justify-center text-[var(--volera-accent)] dark:text-gray-300 overflow-hidden shrink-0 font-bold text-sm">
                           {selectedGroup?.profilePictureUrl ? (
                               <img src={selectedGroup.profilePictureUrl} alt={selectedGroup.name} className="w-full h-full object-cover" />
                           ) : (
                               getInitials(selectedGroup?.name ?? '')
                           )}
                       </div>
                       <div className="flex-1 min-w-0">
                           <h3 className="font-bold text-base sm:text-lg text-gray-800 dark:text-white truncate">
                             {selectedGroup?.name}
                             {selectedGroup?.isChannel ? (
                               <span className="ml-2 text-xs font-normal text-gray-500">Channel</span>
                             ) : null}
                           </h3>
                       </div>
                       {!selectedGroup?.isChannel && (
                       <>
                       <button
                         onClick={() => {
                           if (!selectedGroup) return;
                           initiateGroupCall(selectedGroup.id, selectedGroup.name, false, selectedGroup.profilePictureUrl ?? undefined);
                         }}
                         className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-[var(--volera-accent)] dark:text-gray-300 dark:hover:bg-gray-700 hover:bg-[var(--volera-accent)]/10 rounded-full transition-colors"
                         title="Start Group Voice Call"
                       >
                         <Phone size={24} />
                       </button>
                       <button
                         onClick={() => {
                           if (!selectedGroup) return;
                           initiateGroupCall(selectedGroup.id, selectedGroup.name, true, selectedGroup.profilePictureUrl ?? undefined);
                         }}
                         className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-[var(--volera-accent)] dark:text-gray-300 dark:hover:bg-gray-700 hover:bg-[var(--volera-accent)]/10 rounded-full transition-colors"
                         title="Start Group Video Call"
                       >
                         <Video size={24} />
                       </button>
                       </>
                       )}
                       <button
                         onClick={() => {
                           if (!selectedGroup) return;
                           if (selectedGroup.isChannel) {
                             setIsChannelInfoOpen(true);
                           } else {
                             setIsGroupInfoOpen(true);
                             loadGroupDetails(selectedGroup.id);
                           }
                         }}
                         className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center shrink-0 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors"
                         title={selectedGroup?.isChannel ? 'Channel info' : 'Group info'}
                       >
                         <Info size={22} />
                       </button>
                   </>
               )}
            </div>

            <div 
                ref={scrollContainerRef}
                onScroll={handleScroll}
                className="flex-1 overflow-y-auto p-3 sm:p-4 min-h-0 chat-messages-area"
            >
              {showSystemMessages ? (
                <SystemMessagesChannelContent onMarkRead={async (id) => { try { await systemMessageService.markAsRead(id); } catch { /* ignore */ } }} />
              ) : (
              <>
              {/* Selection action bar (FORWARD N, DELETE N, CANCEL) */}
              {selectionMode && (
                  <div className="sticky top-0 z-10 mb-3 flex items-center justify-between gap-2 px-3 py-2 rounded-xl bg-[var(--volera-accent)] text-white shadow-lg">
                      <div className="flex items-center gap-2">
                          <button
                              type="button"
                              onClick={forwardSelectedMessages}
                              disabled={selectedMessageIds.size === 0}
                              className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white/20 hover:bg-white/30 font-medium text-sm disabled:opacity-50 disabled:pointer-events-none"
                          >
                              <CornerDownRight size={18} />
                              Forward {selectedMessageIds.size}
                          </button>
                          <button
                              type="button"
                              onClick={deleteSelectedMessages}
                              disabled={selectedMessageIds.size === 0}
                              className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white/20 hover:bg-red-500/80 font-medium text-sm disabled:opacity-50 disabled:pointer-events-none"
                          >
                              <Trash2 size={18} />
                              Delete {selectedMessageIds.size}
                          </button>
                      </div>
                      <button
                          type="button"
                          onClick={exitSelectionMode}
                          className="px-4 py-2 text-sm font-medium hover:bg-white/20 rounded-lg"
                      >
                          Cancel
                      </button>
                  </div>
              )}
              {/* Pinned messages header – modern, minimal, supports multiple pins */}
              {!showSavedMessages && !selectionMode && (() => {
                  const pinnedMessages = messages.filter(m => m.isPinned);
                  if (!pinnedMessages.length) return null;

                  const sortedPinned = [...pinnedMessages].sort((a, b) => {
                      const aTime = a.pinnedAt || a.sentAt;
                      const bTime = b.pinnedAt || b.sentAt;
                      return aTime.localeCompare(bTime) * -1; // newest first
                  });

                  const primary = sortedPinned[0];
                  const rest = sortedPinned.slice(1);

                  return (
                      <div className="sticky top-0 z-10 mb-3">
                          <div className="rounded-2xl bg-white/95 border border-gray-200 shadow-sm px-3 py-2 flex flex-col gap-1 backdrop-blur">
                              <div className="flex items-center justify-between gap-2">
                                  <div className="flex items-center gap-2 min-w-0">
                                      <span className="text-sm">📌</span>
                                      <div className="min-w-0">
                                          <div className="text-xs font-semibold text-gray-800 truncate">
                                              Pinned message
                                          </div>
                                          <button
                                              type="button"
                                              onClick={() => scrollToMessage(primary.id)}
                                              className="text-xs text-gray-600 truncate hover:text-[var(--volera-accent)] transition-colors"
                                              title={primary.content || '(attachment)'}
                                          >
                                              {primary.content || '(attachment)'}
                                          </button>
                                      </div>
                                  </div>
                                  <span className="text-[10px] text-gray-400 whitespace-nowrap">
                                      {sortedPinned.length} pinned
                                  </span>
                              </div>
                              {rest.length > 0 && (
                                  <div className="flex items-center gap-1 overflow-x-auto pt-1">
                                      {rest.map(m => (
                                          <button
                                              key={m.id}
                                              type="button"
                                              onClick={() => scrollToMessage(m.id)}
                                              className="px-2 py-0.5 rounded-full bg-gray-100 text-[11px] text-gray-700 hover:bg-[var(--volera-accent)]/10 hover:text-[var(--volera-accent-hover)] transition-colors whitespace-nowrap max-w-[150px] truncate"
                                              title={m.content || '(attachment)'}
                                          >
                                              {m.content || '(attachment)'}
                                          </button>
                                      ))}
                                  </div>
                              )}
                          </div>
                      </div>
                  );
              })()}
              {isLoadingMessages && messages.length > 0 && (
                  <div className="flex justify-center p-2">
                      <div className="w-6 h-6 border-2 border-[var(--volera-accent)] border-t-transparent rounded-full animate-spin"></div>
                  </div>
              )}
              {isLoadingMessages && (!messages || !Array.isArray(messages) || messages.length === 0) ? (
                <div className="flex justify-center items-center h-full text-gray-500">Loading messages...</div>
              ) : (!messages || !Array.isArray(messages) || messages.length === 0) ? (
                <div className="flex justify-center items-center h-full text-gray-400">No messages yet. Say hello!</div>
              ) : (
                messages.map((msg) => {
                  const isMyMessage = msg.senderId === user?.id;
                  let senderProfilePicture;
                  let senderName;

                  if (isMyMessage) {
                      senderProfilePicture = user?.profilePicture;
                      senderName = user?.firstName;
                  } else if (selectedUser && msg.senderId === selectedUser.id) {
                      senderProfilePicture = selectedUser.profilePicture;
                      senderName = getDisplayName(selectedUser);
                  } else {
                      const sender = users?.find(u => u.id === msg.senderId);
                      senderProfilePicture = sender?.profilePicture;
                      senderName = sender ? getDisplayName(sender) : 'Unknown';
                  }

                  return (
                    <MessageBubble 
                      key={msg.id}
                      message={msg}
                      isMyMessage={isMyMessage}
                      senderProfilePicture={senderProfilePicture}
                      senderName={senderName}
                      onEdit={startEditing}
                      onDelete={deleteMessage}
                      onSave={handleSaveMessage}
                      onReply={startReplying}
                      onReact={reactToMessage}
                      onForward={startForwarding}
                      onTogglePin={togglePinMessage}
                      onCopyImage={handleCopyImage}
                      onImageClick={handleImageClick}
                      onSelect={startSelectionMode}
                      showSave={!selectedUser}
                      selectionMode={selectionMode}
                      isSelected={selectedMessageIds.has(msg.id)}
                      onToggleSelect={toggleMessageSelection}
                      isReactionPending={pendingReactionMessageId === msg.id}
                      isSavePending={pendingSaveMessageId === msg.id}
                      isPinPending={pendingPinMessageId === msg.id}
                    />
                  );
                })
              )}
              <div ref={messagesEndRef} />
              </>
              )}
            </div>

            {!showSystemMessages && selectedGroup?.isChannel && selectedGroup.canPost === false ? (
            <div className="p-3 sm:p-4 bg-gray-100 dark:bg-gray-900 border-t border-gray-300 dark:border-gray-700 shrink-0 text-center">
              <p className="text-sm text-gray-500 dark:text-gray-400">Only admins can post in this channel.</p>
            </div>
            ) : !showSystemMessages && (
            <div className="p-2 sm:p-4 bg-gray-100 dark:bg-gray-900 border-t border-gray-300 dark:border-gray-700 shrink-0">
              {editingMessage && (
                  <div className="flex justify-between items-center mb-2 px-4 py-2 bg-[var(--volera-accent)]/10 rounded-lg border border-[var(--volera-accent)]/20">
                      <div className="flex flex-col">
                          <span className="text-xs font-semibold text-[var(--volera-accent)]">Editing message</span>
                          <span className="text-xs text-gray-500 dark:text-gray-400 truncate max-w-[200px]">{editingMessage.content}</span>
                      </div>
                      <button onClick={cancelEditing} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                          <X size={16} />
                      </button>
                  </div>
              )}
              {!editingMessage && replyingTo && (
                  <div className="flex justify-between items-center mb-2 px-4 py-2 bg-green-50 dark:bg-green-900/30 rounded-lg border border-green-100 dark:border-green-800">
                      <div className="flex flex-col">
                          <span className="text-xs font-semibold text-green-700 dark:text-green-400">Replying to</span>
                          <span className="text-xs text-gray-600 dark:text-gray-300 truncate max-w-[220px]">
                              {replyingTo.content}
                          </span>
                      </div>
                      <button onClick={() => setReplyingTo(null)} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                          <X size={16} />
                      </button>
                  </div>
              )}
              {sendAsOptions.length > 0 && !selectedGroup?.isChannel && (
                <div className="mb-2 px-1">
                  <label className="text-xs text-gray-500 dark:text-gray-400 mr-2">Send as</label>
                  <select
                    value={sendAsChannelId}
                    onChange={(e) => setSendAsChannelId(e.target.value)}
                    className="text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 px-2 py-1"
                  >
                    <option value="">Myself</option>
                    {sendAsOptions.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}{c.publicUsername ? ` (@${c.publicUsername})` : ''}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              <form onSubmit={handleSendMessage} className="min-w-0">
                <input
                    type="file"
                    ref={fileInputRef}
                    onChange={handleFileSelect}
                    className="hidden"
                />

                <div className="flex gap-1.5 sm:gap-2 items-end min-w-0">
                {isRecording ? (
                    <div className="flex-1 flex items-center gap-4 px-4 py-3 bg-red-50 dark:bg-red-900/30 rounded-full border border-red-100 dark:border-red-800 animate-pulse">
                        <div className="w-3 h-3 rounded-full bg-red-500 animate-pulse" />
                        <span className="text-red-600 dark:text-red-400 font-medium font-mono">{formatDuration(recordingDuration)}</span>
                        <span className="flex-1 text-center text-gray-400 dark:text-gray-500 text-sm hidden md:block">Release to send, slide out to cancel</span>
                         <span className="flex-1 text-center text-gray-400 dark:text-gray-500 text-sm md:hidden">Recording...</span>
                        <button 
                            type="button" 
                            onClick={cancelRecording}
                            className="p-2 text-red-500 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-800/50 rounded-full transition-colors z-20"
                            title="Cancel recording"
                        >
                            <Trash2 size={20} />
                        </button>
                    </div>
                ) : (
                    <>
                        <button
                            type="button"
                            onClick={() => fileInputRef.current?.click()}
                            className="shrink-0 p-2.5 sm:p-3 text-gray-500 dark:text-gray-400 hover:text-[var(--volera-accent)] dark:hover:text-gray-200 hover:bg-[var(--volera-accent)]/10 dark:hover:bg-gray-700 rounded-full transition-colors"
                            disabled={isUploading}
                            title="Attach file"
                        >
                            <Paperclip size={20} />
                        </button>
                        <textarea
                            ref={messageInputRef}
                            value={messageInput}
                            onChange={(e) => setMessageInput(e.target.value)}
                            onInput={resizeMessageInput}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter' && !e.shiftKey) {
                                e.preventDefault();
                                e.currentTarget.form?.requestSubmit();
                              }
                            }}
                            onFocus={(e) => e.target.scrollIntoView({ block: 'nearest', behavior: 'smooth' })}
                            placeholder={editingMessage ? "Edit your message..." : "Type a message..."}
                            rows={1}
                            className="message-input-scrollbar flex-1 min-w-0 w-full px-3 py-2.5 sm:px-4 sm:py-3 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-2xl text-gray-900 dark:text-gray-100 placeholder:text-gray-500 placeholder:dark:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)] dark:focus:ring-gray-500 focus:border-transparent transition-[box-shadow,border-color,background-color] text-base scroll-mb-4 resize-none overflow-y-hidden min-h-[44px] max-h-[min(30vh,8.5rem)] sm:max-h-[min(35vh,12.5rem)] leading-relaxed"
                          />
                    </>
                )}

                {(!messageInput.trim() && !editingMessage && !isRecording) ? (
                     <button
                        type="button"
                        onMouseDown={startRecording}
                        onMouseUp={stopRecording}
                        onMouseLeave={typeof window !== 'undefined' && 'ontouchstart' in window ? undefined : (isRecording ? cancelRecording : undefined)}
                        onTouchStart={startRecording}
                        onTouchEnd={stopRecording}
                        className="shrink-0 p-2.5 sm:p-3 bg-[var(--volera-accent)] dark:bg-gray-600 text-white rounded-full hover:bg-[var(--volera-accent-hover)] dark:hover:bg-gray-500 transition-colors shadow-sm cursor-pointer select-none touch-none"
                        title="Hold to record"
                    >
                        <Mic size={20} />
                    </button>
                ) : (
                    <button
                      type="submit"
                      disabled={(!messageInput.trim() && !isRecording) || isSending || !isOnline}
                      className={clsx(
                          "shrink-0 p-2.5 sm:p-3 rounded-full transition-colors shadow-sm flex items-center justify-center min-w-[44px] min-h-[44px]",
                          isRecording 
                            ? "bg-red-500 text-white hover:bg-red-600 dark:bg-red-900/70 dark:hover:bg-red-800 animate-pulse" 
                            : "bg-[var(--volera-accent)] dark:bg-gray-600 text-white hover:bg-[var(--volera-accent-hover)] dark:hover:bg-gray-500 disabled:opacity-50 disabled:cursor-not-allowed"
                      )}
                      // If recording, clicking this acts as manual send (though release usually handles it)
                      onClick={isRecording ? stopRecording : undefined}
                    >
                      {isSending ? <Loader2 size={20} className="animate-spin" /> : <Send size={20} />}
                    </button>
                )}
                </div>
                {!isRecording && (
                  <div className="text-xs text-right text-gray-500 dark:text-gray-400 pr-12 sm:pr-14 h-4 mt-0.5">
                    {messageInput.length > 0 && `${messageInput.length} / ${messageLengthLimit}`}
                  </div>
                )}
              </form>
            </div>
            )}
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-[var(--volera-text-muted)] px-4 volera-empty">
            <div className="volera-empty__mark" aria-hidden="true">V</div>
            <p className="text-lg text-center font-medium text-[var(--volera-text)]">Select a chat to start messaging</p>
            <p className="text-sm text-center max-w-sm">Your conversations appear here. Volera keeps drafts and queued sends available offline.</p>
          </div>
        )}
      </div>
      
      <CallModal />
      <ImageViewer
        isOpen={!!viewedImageMessage}
        src={viewedImageMessage ? (getCachedImageBlobUrl(viewedImageMessage.attachmentUrl!) ?? viewedImageMessage.attachmentUrl!) : null}
        alt="Chat attachment"
        downloadFilename={viewedImageMessage?.attachmentUrl?.split('/').pop()?.split('_').slice(1).join('_') || 'image'}
        onClose={() => setViewedImageMessage(null)}
        onDownload={handleImageViewerDownload}
      />
      <UserProfileModal
        user={profileUser}
        isOpen={isProfileOpen}
        onClose={() => setIsProfileOpen(false)}
      />
      <GroupInfoModal
        group={selectedGroup as Group | null}
        members={groupMembers}
        contacts={contacts}
        currentUserId={user?.id}
        isOpen={isGroupInfoOpen}
        onClose={() => setIsGroupInfoOpen(false)}
        onLeave={async () => {
          if (!selectedGroup) return;
          setGroupActionInProgress('leave');
          try {
            await groupService.leaveGroup(selectedGroup.id);
            setIsGroupInfoOpen(false);
            clearSelection();
            fetchGroups();
            fetchRecentChats();
          } catch (error) {
            console.error("Failed to leave group", error);
            addToast("Failed to leave group", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        }}
        onAddMember={async (newMemberId: string) => {
          if (!selectedGroup) return;
          setGroupActionInProgress('add');
          try {
            await groupService.addMember(selectedGroup.id, newMemberId);
            await loadGroupDetails(selectedGroup.id);
            fetchGroups();
          } catch (error) {
            console.error("Failed to add member to group", error);
            addToast("Failed to add member to group", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        }}
        onRemoveMember={async (memberId: string) => {
          if (!selectedGroup) return;
          setGroupActionInProgress('remove');
          try {
            await groupService.removeMember(selectedGroup.id, memberId);
            await loadGroupDetails(selectedGroup.id);
            fetchGroups();
          } catch (error) {
            console.error("Failed to remove member from group", error);
            addToast("Failed to remove member from group", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        }}
        onMakeAdmin={async (newAdminId: string) => {
          if (!selectedGroup) return;
          setGroupActionInProgress('makeAdmin');
          try {
            await groupService.changeAdmin(selectedGroup.id, newAdminId);
            await loadGroupDetails(selectedGroup.id);
            fetchGroups();
          } catch (error) {
            console.error("Failed to change admin", error);
            addToast("Failed to change admin", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        }}
        onUpdateProfile={selectedGroup ? async (data) => {
          setGroupActionInProgress('updateProfile');
          try {
            await groupService.updateProfile(selectedGroup.id, data);
            await loadGroupDetails(selectedGroup.id);
            updateSelectedGroup(data);
            fetchGroups();
          } catch (error) {
            console.error("Failed to update group profile", error);
            addToast("Failed to update group profile", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        } : undefined}
        onDeleteGroup={selectedGroup ? async () => {
          if (!selectedGroup) return;
          setGroupActionInProgress('delete');
          try {
            await groupService.deleteGroup(selectedGroup.id);
            setIsGroupInfoOpen(false);
            clearSelection();
            fetchGroups();
            fetchRecentChats();
            addToast("Group deleted", "success");
          } catch (error) {
            console.error("Failed to delete group", error);
            addToast("Failed to delete group", "error");
          } finally {
            setGroupActionInProgress(null);
          }
        } : undefined}
        inviteCode={groupInviteCode || undefined}
        onGetInviteLink={async () => {
          if (!selectedGroup) return null;
          const { inviteCode: code } = await groupService.generateInviteLink(selectedGroup.id);
          if (code) {
            setGroupInviteCode(code);
            return `${window.location.origin}/invite/${code}`;
          }
          return null;
        }}
        actionInProgress={groupActionInProgress}
        isLoadingDetails={isGroupDetailsLoading}
      />
      {forwardingMessage && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col">
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
              <h3 className="font-semibold text-gray-800 dark:text-white text-sm">
                {forwardingMessages.length > 1 ? `Forward ${forwardingMessages.length} messages` : 'Forward message'}
              </h3>
              <button
                onClick={() => { if (!forwardingTo) { setForwardingMessage(null); setForwardingMessages([]); } }}
                disabled={!!forwardingTo}
                className="p-1 rounded-full text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors disabled:opacity-50 disabled:pointer-events-none"
              >
                <X size={18} />
              </button>
            </div>
            <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                {forwardingMessages.length > 1 ? 'Messages:' : 'Message:'}
              </p>
              <p className="text-sm text-gray-800 dark:text-gray-200 line-clamp-3">
                {forwardingMessages.length > 1
                  ? `${forwardingMessages.length} message(s) selected`
                  : forwardingMessage.content}
              </p>
            </div>
            <div className="flex-1 overflow-y-auto px-4 py-3">
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 uppercase tracking-wide">
                Forward to user
              </p>
              <div className="space-y-1 mb-4">
                {recentChats
                  .filter((rc) => rc.userId && !rc.isGroup && rc.userId !== user?.id)
                  .map((rc) => {
                    const id = `user-${rc.userId!}`;
                    const isForwarding = forwardingTo === id;
                    return (
                      <button
                        key={rc.userId}
                        onClick={() => forwardToUser(rc.userId!)}
                        disabled={!!forwardingTo}
                        className="w-full flex items-center justify-between px-3 py-2 rounded-lg border border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 text-left text-sm disabled:opacity-50 disabled:pointer-events-none"
                      >
                        <span className="truncate text-gray-800 dark:text-gray-200">{getDisplayName({ id: rc.userId!, firstName: rc.firstName, lastName: rc.lastName, username: rc.username })}</span>
                        {isForwarding && <Loader2 size={16} className="animate-spin shrink-0 ml-2" />}
                      </button>
                    );
                  })}
              </div>
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 uppercase tracking-wide">
                Forward to group
              </p>
              <div className="space-y-1">
                {groups.map((g) => {
                  const id = `group-${g.id}`;
                  const isForwarding = forwardingTo === id;
                  return (
                    <button
                      key={g.id}
                      onClick={() => forwardToGroup(g.id)}
                      disabled={!!forwardingTo}
                      className="w-full flex items-center justify-between px-3 py-2 rounded-lg border border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 text-left text-sm disabled:opacity-50 disabled:pointer-events-none"
                    >
                      <span className="truncate text-gray-800 dark:text-gray-200">{g.name}</span>
                      {isForwarding && <Loader2 size={16} className="animate-spin shrink-0 ml-2" />}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      )}
      {isCreateGroupModalOpen && (
        <CreateGroupModal
          isOpen={true}
          onClose={() => setIsCreateGroupModalOpen(false)}
          onRequestAddContact={() => {
            setIsCreateGroupModalOpen(false);
            setIsAddContactModalOpen(true);
          }}
        />
      )}
      {isCreateChannelModalOpen && (
        <CreateChannelModal
          isOpen={true}
          onClose={() => setIsCreateChannelModalOpen(false)}
        />
      )}
      {selectedGroup?.isChannel && (
        <ChannelInfoModal
          channelId={selectedGroup.id}
          isOpen={isChannelInfoOpen}
          onClose={() => setIsChannelInfoOpen(false)}
        />
      )}
      <AddContactModal isOpen={isAddContactModalOpen} onClose={() => setIsAddContactModalOpen(false)} />
    </div>
  );
}
