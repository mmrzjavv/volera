---
name: Blazor Hybrid Mobile Rebuild
overview: Create a new Blazor Hybrid mobile application in a separate folder with full feature parity to the existing frontend (React/Vite), targeting Android and iOS, using only existing backend APIs and SignalR. No changes to the backend.
todos: []
isProject: false
---

# Blazor Hybrid mobile application rebuild

## Scope and constraints

- **Source of truth for features:** The existing frontend lives in [backend/frontend](backend/frontend) (React + Vite). Rebuild with **full feature parity** in a new Blazor Hybrid app. (If you have a separate Next.js app, the same feature list applies.)
- **New app location:** Create a **new, separate folder** for the Blazor Hybrid solution (e.g. `ChatApp.Mobile` at repo root or `mobile` alongside `backend`). Do **not** modify, move, or refactor any backend or existing frontend code.
- **Backend:** Treat as a black box. Use only existing REST APIs, Auth (JWT + refresh), and SignalR hubs. No new endpoints, no backend changes.

---

## 1. Project structure and setup

**Folder layout (example):**

```
Chat-app/
  backend/                    # UNCHANGED
  frontend/ or (existing)     # UNCHANGED (can keep or remove per your choice)
  ChatApp.Mobile/             # NEW – Blazor Hybrid solution
    src/
      ChatApp.Mobile/         # Shared Blazor UI (Razor components, pages, services)
      ChatApp.Mobile.Android/ # Android project
      ChatApp.Mobile.iOS/     # iOS project
    (or single project with -f net8.0-android;net8.0-ios if using single-project MAUI-style)
```

**Technology:**

- **.NET 8** (or latest LTS) with **Blazor Hybrid** (BlazorWebView inside native Android/iOS shell).
- Use **.NET MAUI** with Blazor (recommended for one codebase → Android + iOS): create a **MAUI Blazor App** so the UI is built with Razor/Blazor and the host is MAUI. This gives a single shared project (`*.Razor`, `*.razor.cs`) and platform projects for Android and iOS.
- Alternative: **Blazor Hybrid in separate Android/iOS projects** (e.g. Android WebView + Blazor, iOS WKWebView + Blazor) if you prefer not to use MAUI; then you need two host projects that reference the same Blazor UI project.

**Initialization steps (to be done when implementing):**

1. Create new solution and folder **outside** the backend tree.
2. Create a **MAUI Blazor App** (or Blazor Hybrid template) and add Android + iOS targets.
3. Add shared Blazor UI project (pages, components, services) and ensure Android/iOS projects reference it and host the BlazorWebView with the correct base path and root component.
4. Configure **base URL** for API and SignalR (e.g. from config or build constants) pointing to the existing backend (e.g. `https://api-voice-call-app.liara.run` or dev URL). No backend code changes.

---

## 2. Feature parity checklist (from existing frontend)

Every feature below must exist in the Blazor Hybrid app, with mobile-first, native-like UX.

**Authentication and authorization**

- Login page: username + password; call `POST /api/Auth/login`; store token, refreshToken, user in secure storage (Preferences or secure storage).
- Register page: fields per `RegisterRequest`; call `POST /api/Auth/register`; then redirect to login or auto-login if backend supports.
- Refresh token: on 401, call `POST /api/Auth/refresh-token` with current access + refresh token; update stored tokens and retry request; on failure clear storage and redirect to login.
- Protected routes: if no valid token, redirect to login. Equivalent of `PrivateRoute`: render main app only when authenticated.
- Logout: clear token/refresh/user; navigate to login.

**Navigation and routing**

- Routes to implement (Blazor equivalent): Login, Register, Main Chat (default), Profile, Admin Messages. Use Blazor router or MAUI Shell with equivalent paths (e.g. `/`, `/login`, `/register`, `/profile`, `/admin/messages`).

**Main chat (core)**

- Tabs or sections: Chats (recent), Contacts, Groups (match [Chat.tsx](backend/frontend/src/pages/Chat.tsx) structure).
- Recent chats list: from `GET /api/Message/recent`; show last message, time, unread count; tap to select conversation.
- Saved Messages: special “chat” for messages where sender = receiver; `GET /api/Message/saved` and send to self for new messages (receiverId = current user).
- Select user (DM): load conversation `GET /api/Message/{userId}` with pagination (before/limit); show messages; send via SignalR `SendMessage(receiverId, content, attachmentUrl, attachmentType)`.
- Select group: load `GET /api/Group/{groupId}/messages`; send via SignalR `SendGroupMessage(groupId, content, ...)`; ensure client is in group (JoinGroup when opening group).
- Message list: infinite scroll / load more (previous messages) using `before` + limit.
- Send text: ChatHub `SendMessage` or `SendGroupMessage` with content.
- Attachments: upload via `POST /api/Upload` or initiate + presigned URL flow; then send message with `attachmentUrl` and `attachmentType`.
- Voice messages: record audio (platform APIs in MAUI/Hybrid); upload file; send message with content e.g. "Voice Message" and attachment URL/type.
- Message actions: edit (owner), delete (owner), save/unsave (`POST/DELETE /api/Message/{id}/save`). Use existing API contracts.
- Message bubble UI: text, image preview, file download, voice player (play/pause), RTL support if applicable, show sender name in groups.

**Contacts**

- Contact list: `GET /api/Contact` or equivalent; display with contact user info.
- Add contact: modal/screen with identifier (phone/user); call add-contact API.
- Sync contacts: if backend supports sync by phone numbers, implement; otherwise match current behavior.

**Groups**

- Group list: `GET /api/Group` (my groups).
- Create group: modal with name + member selection; `POST /api/Group` with memberIds.
- Group chat: same as “Select group” above; add member via `POST /api/Group/{groupId}/members` (if current user is admin).

**Calls (1:1 voice and video)**

- Initiate call: `POST /api/Call/initiate` (receiverId, isVideo); then SignalR `JoinCallGroup(callId)`; send signaling (offer/answer/ICE) via CallHub (`SendSignal`, or SendOffer/SendAnswer/SendIceCandidate if used).
- Incoming call: handle `CallInitiated` from CallHub; show incoming UI; accept → `POST /api/Call/{callId}/accept` + JoinCallGroup + WebRTC answer; reject → `POST /api/Call/{callId}/reject`.
- End call: `POST /api/Call/{callId}/end`; leave group; cleanup media.
- Call history: `GET /api/Call/history` with pagination.
- WebRTC: implement in Blazor Hybrid using **native WebRTC** or a bridge. MAUI/Blazor runs in a WebView; for true native WebRTC you may need a **native bridge** (Android/iOS WebRTC SDK) and pass signaling over from Blazor, or use a WebView that runs WebRTC (if the host WebView supports it). This is a known complexity in Hybrid; document approach (e.g. “signaling in Blazor, media in native module” or “in-app browser for WebRTC”).
- Ringtone: play incoming/outgoing sound (platform audio APIs).

**Profile**

- View/edit profile: `GET /api/User/profile`, `PUT /api/User/profile` (firstName, lastName, email, bio, profilePicture).
- Upload profile picture: `POST /api/User/upload-profile-picture` (multipart).
- Change password: `PUT /api/User/change-password`.
- Navigate back to chat.

**Admin / system messages**

- Page equivalent to [AdminMessages.tsx](backend/frontend/src/pages/AdminMessages.tsx): list active system messages; create/update/delete if backend supports; mark as read. Use existing system message APIs.

**State management**

- Auth state: current user, token, isAuthenticated; persist tokens in secure storage; restore on app start.
- Chat state: selectedUser, selectedGroup, showSavedMessages, messages, recentChats, users, groups, connection (SignalR), loading, hasMore, unreadCounts. Replicate behavior of [useChatStore](backend/frontend/src/store/useChatStore.ts) (initialize connection, fetch recent/unread, send message, add message from hub, update user status).
- Call state: callId, status, caller/receiver, streams, connection; replicate [useCallStore](backend/frontend/src/store/useCallStore.ts) for 1:1 calls.
- Contacts, theme, toasts, confirmation dialog: equivalent state and UI.

**SignalR integration**

- **ChatHub:** Connect with JWT (`accessTokenFactory`). Handle `ReceiveMessage`, `ReceiveGroupMessage` (or equivalent); update message list. Call `SendMessage`, `SendGroupMessage`, `JoinGroup` as per existing backend.
- **CallHub:** Connect with JWT; `JoinCallGroup`/`LeaveCallGroup`; send/receive signaling (offer, answer, ICE); handle `CallInitiated`, `CallAccepted`, `CallRejected`, `CallEnded`, `UserOnline`, `UserOffline`. Backend contract unchanged.

**Error handling and UX**

- Global error handler / error boundary (Blazor equivalent): catch unhandled errors; show friendly message; optionally report to backend if error-report API exists (no backend change).
- API errors: parse `ApiResponse.message`; show toast or inline message. 401 → refresh or logout and redirect to login.
- Network errors: retry or show “no connection” message; do not change backend.
- Validations: client-side validation for forms (login, register, profile, create group); match existing rules where visible in current app.

**UI/UX (mobile-first)**

- Responsive layouts: list/detail or master-detail for chat list vs conversation; bottom or top nav for tabs.
- Native-like: use platform-appropriate controls (MAUI controls or Blazor components styled for mobile); smooth scrolling, pull-to-refresh for chat list if applicable.
- Theme: support light/dark if current app does; persist preference.
- Toasts/snackbars: success and error feedback.
- Confirmation dialogs: for delete message, leave call, etc.

---

## 3. Backend integration (black box)

**REST**

- Base URL: configurable (e.g. `https://api-voice-call-app.liara.run` or dev). Use `HttpClient` with `BaseAddress`; add `Authorization: Bearer {token}` from secure storage.
- DTOs: define C# models that match existing API responses and requests (e.g. `ApiResponse<T>`, `AuthResponse`, `User`, `Message`, `RecentChat`, `Group`, etc.) so JSON (de)serialization matches. Do **not** change backend DTOs.
- Refresh token: centralize in a delegating handler or service: on 401, try refresh once, then retry; on refresh failure clear storage and redirect to login. Same logic as current [api.ts](backend/frontend/src/services/api.ts) interceptor.
- File upload: use `MultipartFormDataContent` or `StreamContent` for profile picture and attachments; use presigned URL upload if that’s what the backend uses (from Upload controller).

**SignalR**

- Use **Microsoft.AspNetCore.SignalR.Client** from the Blazor/MAUI project. Connect to `{baseUrl}/chatHub` and `{baseUrl}/callHub` with `AccessTokenProvider`. Reconnect policy; pass token from secure storage.
- Message types: deserialize payloads to C# types that match what the backend sends (e.g. same property names). No backend change.

**Auth**

- Store tokens and user in **secure storage** (e.g. MAUI SecureStorage, or platform-specific secure storage). On startup, restore and optionally validate with a lightweight API call or just try the next request and rely on 401/refresh.

---

## 4. Recommended code structure (Blazor Hybrid / MAUI Blazor)

- **Pages:** Login, Register, Chat (main), Profile, AdminMessages. One Razor page per screen; use `@page "/..."` or Shell routes.
- **Components:** Reusable pieces: MessageBubble, ContactList, RecentChatList, GroupList, CallModal, ConfirmationDialog, Toast container, Input, Button. Match current UI building blocks.
- **Services (injected):** `IAuthService`, `IApiClient` (wraps HttpClient + refresh), `IChatHubClient`, `ICallHubClient`, `ISecureStorageService`. Implement in shared project; no backend dependency except HTTP and SignalR URLs.
- **State:** Use a state container (e.g. `ChatState`, `AuthState`) or a small state library (e.g. Fluxor, or simple cascading values) to hold selected chat, messages, connection status, etc., so multiple components can react. Keep logic consistent with current stores.
- **Models:** Shared DTOs in a folder (e.g. `Models/` or `Contracts/`) matching backend API and SignalR payloads.

---

## 5. Mobile platform requirements

- **Single codebase:** One Blazor UI codebase; Android and iOS projects only differ by host and platform config (icons, permissions, signing).
- **Lifecycle:** Handle app resume/suspend (e.g. reconnect SignalR on resume; pause media on suspend). MAUI lifecycle events can be used.
- **Permissions:** Request microphone (and camera for video call) on Android/iOS when entering call or recording voice message; follow store guidelines.
- **Performance:** Virtualize long message lists where possible; avoid loading thousands of messages at once; use cursor/offset pagination as the backend supports. Ensure scrolling is smooth.
- **Builds:** Produce Android APK/AAB and iOS IPA for testing and store submission; configure signing and provisioning outside this plan (no backend impact).

---

## 6. Quality and practices

- **Separation of concerns:** UI (Razor) → state/services → API and SignalR. No business logic in views; services call backend only.
- **Async:** All API and SignalR calls async; use `CancellationToken` where applicable; no `.Result` or `.Wait()`.
- **Errors:** Centralized API error handling (refresh, toast); try/catch in SignalR handlers; optional retry for transient failures.
- **Reuse:** Shared components and services across pages; single place for API base URL and auth header setup.

---

## 7. Deliverables summary


| Deliverable         | Description                                                                                                                     |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| New solution folder | Blazor Hybrid (MAUI Blazor) app in a separate folder; backend and existing frontend untouched.                                  |
| Android app         | Buildable Android project; all features working; uses existing backend APIs and hubs.                                           |
| iOS app             | Buildable iOS project; same behavior; uses existing backend.                                                                    |
| Feature parity      | Auth, chat (DM + group + saved), contacts, groups, 1:1 calls (voice/video), profile, admin messages, state, errors, navigation. |
| Backend             | No changes to backend code, structure, or database.                                                                             |


---

## 8. Note on WebRTC in Hybrid

The current web app uses WebRTC in the browser. In Blazor Hybrid/MAUI, the Blazor UI runs in a WebView or native WebView; **WebRTC in a hybrid WebView** may be limited on mobile. Options: (1) Use a **native WebRTC library** (e.g. libwebrtc bindings for Android/iOS) and implement only signaling in Blazor; (2) Use a **in-app browser** or separate WebView dedicated to the call page with WebRTC; (3) Use a **third-party binding** (e.g. WebRTC NuGet for MAUI if available). Plan for one of these so 1:1 voice/video remains functional with the **same backend** (CallHub and REST call APIs unchanged).

This plan gives you a complete roadmap to deliver Android and iOS apps with feature parity to the existing frontend, without modifying the backend.