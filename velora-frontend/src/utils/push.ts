import api from '../services/api';

const urlBase64ToUint8Array = (base64String: string) => {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
};

export const registerServiceWorker = async () => {
    // In development mode, VitePWA's virtual module handles registration automatically.
    // We only need to manually register if we're not using the plugin's auto-registration
    // or if we have specific custom needs.
    // However, since we're using 'injectManifest' with 'sw.ts', and 'devOptions' enabled,
    // the plugin injects a registration script.
    
    // If you are seeing errors about MIME types in dev, it's often because
    // we are trying to register '/sw.js' manually while Vite is serving it differently.
    
    if ('serviceWorker' in navigator) {
        // Only log, let vite-plugin-pwa handle registration if possible.
        // Or if you MUST manually register, use the correct path.
        // In dev, with injectManifest, it's a bit tricky.
        
        // For now, let's TRY to rely on vite-plugin-pwa's virtual registration
        // if we are importing 'virtual:pwa-register'. 
        // But since we are not, let's keep it simple.
        
        // The error 'The script has an unsupported MIME type ('text/html')' usually means
        // the server returned the index.html (404 fallback) instead of the JS file.
        // This happens if sw.js is not found at that path.
        
        // When using strategies: 'injectManifest' and filename: 'sw.ts', 
        // in dev mode, the file might be compiled to something else or handled in memory.
        
        try {
            // Check if we are in dev mode
            if (import.meta.env.DEV) {
                 console.log('Service Worker registration skipped in DEV (handled by VitePWA plugin)');
                 return;
            }

            const registration = await navigator.serviceWorker.register('/sw.js');
            console.log('Service Worker registered with scope:', registration.scope);
            return registration;
        } catch (error) {
            console.error('Service Worker registration failed:', error);
        }
    }
    return null;
};

/**
 * Subscribe to push and register with backend. Call when user is authenticated.
 * Idempotent: if already subscribed, re-sends current subscription to backend (refreshes server state).
 */
export const subscribeToPushNotifications = async (): Promise<boolean> => {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.warn('Push notifications not supported');
        return false;
    }

    try {
        let registration;
        try {
            registration = await navigator.serviceWorker.ready;
        } catch (e) {
            if (e instanceof Error && e.name === 'InvalidStateError') {
                console.warn('Service Worker not ready due to InvalidStateError. Skipping subscription.');
                return false;
            }
            throw e;
        }

        let subscription = await registration.pushManager.getSubscription();

        if (!subscription) {
            const response = await api.get('/Push/vapid-public-key');
            const publicKey = response.data?.data?.publicKey || response.data?.publicKey;
            if (!publicKey) {
                console.warn('VAPID public key not found in response');
                return false;
            }
            const convertedVapidKey = urlBase64ToUint8Array(publicKey);
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: convertedVapidKey
            });
        }

        const subscriptionJson = subscription.toJSON();
        await api.post('/Push/subscribe', {
            endpoint: subscriptionJson.endpoint,
            keys: {
                p256dh: subscriptionJson.keys?.p256dh,
                auth: subscriptionJson.keys?.auth
            }
        });

        console.log('Push notification subscribed successfully');
        return true;
    } catch (error: any) {
        if (error.name === 'AbortError' || error.message?.includes('push service not available')) {
            console.warn('Push notifications skipped: Push service not available or blocked.');
        } else {
            console.error('Failed to subscribe to push notifications:', error);
        }
        return false;
    }
};

/**
 * Request permission (if needed) and subscribe. Call when user is on any private page
 * so 1:1 notifications work even if they never opened Chat first.
 */
export const ensurePushSubscription = async (): Promise<void> => {
    if (!('Notification' in window)) return;
    const permission = Notification.permission;
    if (permission === 'denied') return;
    if (permission === 'granted') {
        await subscribeToPushNotifications();
        return;
    }
    // default: request and then subscribe
    const result = await Notification.requestPermission();
    if (result === 'granted') {
        await subscribeToPushNotifications();
    }
};
