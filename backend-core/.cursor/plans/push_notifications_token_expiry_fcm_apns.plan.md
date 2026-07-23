# Push notifications: token expiry and background delivery (FCM/APNs)

## Overview

Users should receive message and call notifications even when they do not open the app (like Telegram/WhatsApp). Today two gaps prevent this:

1. **Token expiry**: Push subscription is done via `POST /api/v1/Push/subscribe` which requires `[Authorize]`. If the user’s JWT expires and they never open the app, they cannot refresh the token or re-register for push, so they stop getting notifications.
2. **Web Push vs native**: The backend uses **Web Push (VAPID)** only. This works for web/PWA and for in-browser contexts. Native iOS/Android apps (and often Blazor Hybrid when the app process is killed) need **FCM (Firebase Cloud Messaging)** and **APNs (Apple Push Notification service)** so the OS can wake the device and show the notification.

This plan describes how to support token expiry and background delivery so notifications work when the app is closed.

---

## 1. Current state

- **Backend**: `PushNotificationService` sends only Web Push (VAPID) to subscriptions stored in DB. `PushController.Subscribe` requires a valid JWT.
- **Web frontend**: Registers via service worker + `PushManager` and calls `POST /Push/subscribe` with endpoint/keys. Works when the user has the site open or in a PWA.
- **Mobile (Blazor Hybrid)**: No push registration in the codebase. When the app is killed, there is no SignalR connection and no Web Push subscription that the OS will wake, so users get no notifications.

---

## 2. Goals

- Notifications for **messages** (1:1 and group) and **calls** (1:1 and group) when the app is **closed or in background**.
- Notifications still work when the user has **not opened the app** for a long time (e.g. JWT expired).
- Support **Android** (FCM), **iOS** (APNs), and **Windows** (WNS or FCM) where applicable, plus keep **Web Push** for web/PWA.

---

## 3. Strategy

### 3.1 Why platform push (FCM/APNs) is required

- **Web Push**: Delivered by the browser/WebView. If the app process is killed or the user never opens the app, the browser context is not running, so the OS does not wake the app and Web Push is not delivered.
- **FCM (Android) / APNs (iOS)**: The app registers once and gets a **device token**. The backend sends to FCM/APNs with that token; the **OS** delivers the notification. This does not depend on the app being open or on the user’s JWT. So notifications can be delivered even when the token is expired and the user has not opened the app.

### 3.2 Token expiry and “no open app”

- **Access token**: Short-lived (e.g. 15–60 min). Used for API calls and SignalR. When it expires and the user does not open the app, they cannot refresh it.
- **Refresh token**: Long-lived (e.g. 7–30 days). Used only when the user opens the app to get a new access token. If the user never opens the app, refresh does not run.
- **Push device token**: Long-lived. Stored on the server and associated with `UserId` (and optionally device id). Sending a notification does **not** require the user’s JWT; the backend just looks up “user X → FCM/APNs tokens” and sends. So:
  - **As long as the device token is registered**, the user can receive notifications even if the access token is expired and they never open the app.
  - The only time we lose notifications is when the device token becomes invalid (e.g. user uninstalled app, OS invalidated token) and we have no way to re-register until the user opens the app again.

So the plan is:

1. **Register device tokens** (FCM/APNs) when the user opens the app and is authenticated; store them per user (and optionally per device).
2. **Send notifications** via FCM/APNs using those stored tokens. No JWT needed for this path.
3. **Keep refresh token** for when the user does open the app: they get a new access token and can re-register push if needed.
4. **Optional**: Extend refresh token lifetime (e.g. 30 days) so that when the user finally opens the app, they can still refresh and re-register without re-login.

---

## 4. Backend changes

### 4.1 Device token storage

- Add a table (or reuse/expand push concept) for **device push tokens**:
  - `UserId`, `Platform` (android | ios | web | windows), `Token` (FCM/APNs token), `DeviceId` (optional), `UpdatedAt`.
- Endpoints (or extend existing push API):
  - **Register device token**: `POST /api/v1/Push/register-device` (or similar) with body like `{ "platform": "android", "token": "<fcm_token>", "deviceId": "optional" }`.
  - This endpoint **must be authenticated** (user must be logged in at least once to associate token with user). After that, sending notifications does not require the user to be logged in.
  - **Unregister**: `POST /api/v1/Push/unregister-device` with `token` or `deviceId` so we remove invalid tokens when the app knows (e.g. on logout or when FCM/APNs invalidates the token).

### 4.2 Sending via FCM/APNs

- Add (or integrate) a service that can send to:
  - **FCM** (HTTP v1 API): using a Firebase service account JSON; send to one or many FCM tokens.
  - **APNs**: using .p8 key + key id + team id + bundle id; send to one or many device tokens.
- When sending a notification (message or call):
  1. Resolve target user(s) (e.g. receiver, or group members for group message/call).
  2. For each user, get:
     - Web Push subscriptions (current behavior) → send Web Push.
     - FCM/APNs device tokens (new) → send via FCM/APNs.
  3. On send failure (e.g. 410 Gone, or “invalid token”), remove that token from DB so we don’t keep trying.

### 4.3 No change to auth for “sending”

- Sending a notification is a **server-side action** (e.g. when a message is sent or a call is initiated). The backend already knows the recipient user id(s). It does not need the recipient’s JWT to look up their device tokens and send; only the **registration** of the device token needs the user to be authenticated once.

---

## 5. Mobile app changes (ChatApp.Mobile)

### 5.1 Android (FCM)

- Add **Firebase Cloud Messaging** (e.g. `Microsoft.Maui.Push` or Firebase SDK).
- On startup (when user is logged in), get FCM token and call backend `POST /api/v1/Push/register-device` with `platform: "android"` and `token: "<fcm_token>"`.
- Handle token refresh: when FCM invokes the callback with a new token, re-register with the backend.
- On logout (or when unregistering), call backend to remove the device token.

### 5.2 iOS (APNs)

- Enable **Push Notifications** capability and configure APNs (certificate or .p8 key in Apple Developer).
- Request notification permission; get device token from APNs and send it to the backend (e.g. same `register-device` with `platform: "ios"` and `token: "<apns_token>"`).
- On logout, unregister the device token on the backend.

### 5.3 Windows (optional)

- Use **WNS** (Windows Push Notification Service) or FCM for Windows; same idea: get channel/token, register with backend, send from backend.

### 5.4 Blazor Hybrid and “opening the app”

- When the user opens the app, the usual flow runs: restore stored tokens (or show login). If access token is expired, use refresh token to get a new access token, then re-register FCM/APNs token with the backend. So the next time they close the app, we still have a valid device token for push.

---

## 6. Web (existing) and Web Push

- Keep current Web Push (VAPID) for web/PWA users. Backend can send both: Web Push to browser subscriptions and FCM/APNs to native app tokens for the same user (so multiple devices get the notification).
- Web Push subscription still requires the user to have the site open (or PWA) at least once to subscribe; token expiry is less critical for web if they revisit the site and re-subscribe on load.

---

## 7. Summary checklist

| Item | Owner | Notes |
|------|--------|--------|
| DB: device token table (UserId, Platform, Token, DeviceId, UpdatedAt) | Backend | Migrations |
| API: Register device token (auth required) | Backend | POST /Push/register-device |
| API: Unregister device token | Backend | POST /Push/unregister-device |
| Service: FCM send (HTTP v1) | Backend | Use Firebase admin SDK or HTTP |
| Service: APNs send (.p8) | Backend | Use library or HTTP/2 |
| Notification send: include FCM/APNs path | Backend | After resolving recipient user ids |
| Remove invalid tokens on send failure | Backend | 410 / invalid token handling |
| Mobile: FCM integration + register on login | Mobile | Android |
| Mobile: APNs integration + register on login | Mobile | iOS |
| Mobile: Re-register on token refresh | Mobile | FCM/APNs callbacks |
| Mobile: Unregister on logout | Mobile | Both platforms |
| Optional: longer refresh token lifetime | Backend | Config (e.g. 30 days) |

---

## 8. Token expiry: user does not open app

- **Problem**: User’s access token (and eventually refresh token) expires and they never open the app.
- **Effect on push**: None for **sending**. Notifications are sent using stored device tokens; no JWT is needed.
- **Effect on re-registration**: If the OS or FCM/APNs invalidates the device token (e.g. app reinstall, OS cleanup), we cannot get a new token until the user opens the app again. When they do, they must log in again if refresh token is expired; after login we register the new device token.
- **Recommendation**: Use a long-lived refresh token (e.g. 30 days) so that when the user finally opens the app, they can refresh and avoid re-login, and we can update the device token then.

This plan, once implemented, gives “Telegram/WhatsApp-like” behavior: notifications for messages and calls even when the app is closed and the user has not opened the app for a long time, as long as the device token was ever registered and has not been invalidated by the OS.
