/// <reference lib="webworker" />
import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching'

declare let self: ServiceWorkerGlobalScope

cleanupOutdatedCaches()

precacheAndRoute(self.__WB_MANIFEST)

self.addEventListener('message', (event: any) => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})

self.addEventListener('push', function(event: any) {
    if (!event.data) return;

    const payload = event.data.json();
    const data = payload.data || {};
    const isCall = data.type === 'call_initiated';
    const isGroupCall = data.type === 'group_call_initiated';
    const isGroupMessage = data.type === 'group_message';

    const title = payload.title || 'New notification';
    const body = payload.body || '';

    const tag = isCall
        ? 'incoming-call'
        : isGroupCall
            ? `group-call-${data.groupId || ''}`
            : isGroupMessage
                ? `group-${data.groupId || ''}`
                : `dm-${data.senderId || ''}`;

    const options: any = {
        body,
        icon: '/icon.svg',
        badge: '/icon.svg',
        data: { ...data },
        vibrate: isCall || isGroupCall ? [200, 100, 200, 100, 200, 100, 400] : [100, 50, 100],
        tag,
        renotify: true,
        requireInteraction: isCall || isGroupCall,
        actions: isCall || isGroupCall ? [
            { action: 'answer', title: 'Answer' },
            { action: 'decline', title: 'Decline' }
        ] : []
    };

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList: any) => {
            const visibleClient = clientList.find((c: any) => c.visibilityState === 'visible');
            if (visibleClient) {
                // App is open and focused: send to app for in-app notification (no OS toast)
                visibleClient.postMessage({
                    type: 'PUSH_RECEIVED',
                    title,
                    body,
                    data: { ...data }
                });
                return;
            }
            self.registration.showNotification(title, options);
        })
    );
});

self.addEventListener('notificationclick', function(event: any) {
    event.notification.close();

    if (event.action === 'decline') {
        // TODO: Ideally send a decline signal to backend without opening window
        return;
    }

    const data = event.notification.data || {};
    const type = data.type || '';
    const payload = { type: 'NOTIFICATION_CLICK', ...data, action: event.action };

    const buildUrl = () => {
        const base = self.location.origin + '/';
        if (type === 'call_initiated') {
            const params = new URLSearchParams({
                openCall: '1',
                callId: String(data.callId || ''),
                callerId: String(data.callerId || ''),
                callerName: String(data.callerName || ''),
                receiverId: String(data.receiverId || ''),
                isVideo: String(data.isVideo === true ? '1' : '0')
            });
            return base + '?' + params.toString();
        }
        if (type === 'message' && data.senderId) {
            return base + '?openChat=' + encodeURIComponent(data.senderId);
        }
        if (type === 'group_message' && data.groupId) {
            return base + '?openGroup=' + encodeURIComponent(data.groupId);
        }
        return base;
    };

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function(clientList: any) {
            if (clientList.length > 0) {
                const client = clientList[0];
                if (client.focus) client.focus();
                return client.postMessage(payload);
            }
            if (self.clients.openWindow) {
                return self.clients.openWindow(buildUrl());
            }
        })
    );
});
