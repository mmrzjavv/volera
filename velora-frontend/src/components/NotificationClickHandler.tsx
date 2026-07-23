import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useCallStore } from '../store/useCallStore';
import { useChatStore } from '../store/useChatStore';
import { useAuthStore } from '../store/useAuthStore';
import { useInAppNotificationStore } from '../store/useInAppNotificationStore';

/**
 * Handles notification clicks: when user clicks a call or message notification,
 * we navigate to Chat and show the call UI or open the relevant conversation.
 * Listens for postMessage from the service worker (existing tab) and reads
 * URL search params (new tab/window).
 * Also handles PUSH_RECEIVED when app is open: show in-app banner and suppress
 * if user is already in that chat (text messages only).
 */
export function NotificationClickHandler() {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const handlePayload = (data: Record<string, unknown>) => {
      const isAuth = !!useAuthStore.getState().user;
      navigate('/', { replace: true });
      if (!isAuth) return;

      const type = String(data.type || '');

      if (type === 'call_initiated') {
        useCallStore.getState().setIncomingFromNotification({
          callId: String(data.callId || ''),
          callerId: String(data.callerId || ''),
          callerName: String(data.callerName || ''),
          receiverId: String(data.receiverId || ''),
          isVideo: data.isVideo === true
        });
        return;
      }
      if (type === 'message' && data.senderId) {
        useChatStore.getState().selectUserById(String(data.senderId));
        return;
      }
      if (type === 'group_message' && data.groupId) {
        useChatStore.getState().selectGroupById(String(data.groupId));
      }
    };

    const handlePushReceived = (data: {
      type: string;
      title?: string;
      body?: string;
      data?: Record<string, unknown>;
    }) => {
      const isAuth = !!useAuthStore.getState().user;
      if (!isAuth) return;

      const type = String(data.data?.type ?? data.type ?? '');
      const title = data.title ?? 'New message';
      const body = data.body ?? '';

      // Call: show in-app notification (banner) so user can answer/decline
      if (type === 'call_initiated' || type === 'group_call_initiated') {
        useInAppNotificationStore.getState().add({
          type: type as 'call_initiated' | 'group_call_initiated',
          title,
          body,
          callId: String(data.data?.callId ?? ''),
          callerId: String(data.data?.callerId ?? ''),
          callerName: String(data.data?.callerName ?? ''),
          receiverId: String(data.data?.receiverId ?? ''),
          isVideo: data.data?.isVideo === true,
        });
        return;
      }

      // Text messages (DM or group): show in-app only if user is NOT in that chat
      if (type === 'message' || type === 'group_message') {
        const { selectedUser, selectedGroup } = useChatStore.getState();
        const sameChat =
          (type === 'message' && selectedUser?.id === data.data?.senderId) ||
          (type === 'group_message' && selectedGroup?.id === data.data?.groupId);
        if (sameChat) return;

        useInAppNotificationStore.getState().add({
          type: type as 'message' | 'group_message',
          title,
          body,
          senderId: data.data?.senderId != null ? String(data.data.senderId) : undefined,
          groupId: data.data?.groupId != null ? String(data.data.groupId) : undefined,
          senderName: data.data?.senderName != null ? String(data.data.senderName) : undefined,
          groupName: data.data?.groupName != null ? String(data.data.groupName) : undefined,
        });
      }
    };

    const onMessage = (event: MessageEvent) => {
      const data = event.data;
      if (data?.type === 'NOTIFICATION_CLICK') {
        handlePayload(data);
      }
      if (data?.type === 'PUSH_RECEIVED') {
        handlePushReceived(data);
      }
    };

    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
      navigator.serviceWorker.addEventListener('message', onMessage);
      return () => navigator.serviceWorker.removeEventListener('message', onMessage);
    }
  }, [navigate]);

  // Handle URL params when app opens from notification (new window)
  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const openCall = params.get('openCall');
    const openChat = params.get('openChat');
    const openGroup = params.get('openGroup');
    const isAuth = !!useAuthStore.getState().user;

    if (!openCall && !openChat && !openGroup) return;

    navigate('/', { replace: true });
    window.history.replaceState({}, '', window.location.pathname);

    if (!isAuth) return;

    if (openCall === '1') {
      useCallStore.getState().setIncomingFromNotification({
        callId: params.get('callId') || '',
        callerId: params.get('callerId') || '',
        callerName: params.get('callerName') || '',
        receiverId: params.get('receiverId') || '',
        isVideo: params.get('isVideo') === '1'
      });
      return;
    }
    if (openChat) {
      useChatStore.getState().selectUserById(openChat);
      return;
    }
    if (openGroup) {
      useChatStore.getState().selectGroupById(openGroup);
    }
  }, [location.search, navigate]);

  return null;
}
