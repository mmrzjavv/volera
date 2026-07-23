---
name: Chat Features Plan
overview: "Plan for seven chat and group features: reply to messages, message reactions (emoji), forward message, saved-messages space (Telegram-style), user profile view in chat, group features (leave, admin, permissions), and pin messages. Each feature is scoped to fit the existing DDD/CQRS backend and React frontend."
todos: []
isProject: false
---

# Chat and group features (7 items)

This plan covers all seven features in one place. Implementation can be done in phases (e.g. 1–2, then 3–4, then 5–7) or per feature.

---

## Current state (brief)

- **Message:** [Message.cs](backend/src/Core.Domain/Entities/Message.cs) has SenderId, ReceiverId, GroupId, Content, Attachment*, SentAt, IsRead, IsEdited, DeletedAt. No ReplyToMessageId, reactions, forward, or pin.
- **User:** [User.cs](backend/src/Core.Domain/Entities/User.cs) has Bio, ProfilePicture, Email. [UserController](backend/src/WebAPI/Controllers/UserController.cs) exposes GET `/profile` only for current user (no public profile by userId).
- **SavedMessage:** [SavedMessage.cs](backend/src/Core.Domain/Entities/SavedMessage.cs) is “bookmark”: user saves an existing message. [SendMessageCommandHandler](backend/src/Core.Application/Handlers/SendMessageCommandHandler.cs) already auto-saves when `SenderId == ReceiverId` (send to self).
- **Group:** [Group.cs](backend/src/Core.Domain/Entities/Group.cs) has AdminId; [GroupMember](backend/src/Core.Domain/Entities/GroupMember.cs) has IsAdmin. [AddMemberCommandHandler](backend/src/Core.Application/Handlers/AddMemberCommandHandler.cs) enforces “only admins can add members.” No leave-group, remove-member, or other permissions.
- **Frontend:** [MessageBubble](backend/frontend/src/components/chat/MessageBubble.tsx) supports edit/delete/save; chat header shows selected user/group and call buttons; no reply UI, reactions, forward, or pin. [Chat.tsx](backend/frontend/src/pages/Chat.tsx) has `showSavedMessages` and uses `messageService` / SignalR for sending.

---

## 1. Reply on messages (groups and chats)

**Backend**

- **Domain:** Add to Message: `ReplyToMessageId` (nullable Guid), optional navigation `ReplyToMessage`. In constructors and `SendMessageCommand` flow, accept optional `ReplyToMessageId`; validate that the replied-to message exists and belongs to the same conversation (same ReceiverId/GroupId).
- **Persistence:** Add column and FK in [ApplicationDbContext](backend/src/Infrastructure/Persistence/ApplicationDbContext.cs); migration.
- **Application:** Extend `SendMessageCommand` and `SendMessageCommandHandler` to accept `ReplyToMessageId`; when creating Message, set it and ensure reply-to is in same chat. Extend `SendMessage` / `SendGroupMessage` in [ChatHub](backend/src/WebAPI/Hubs/ChatHub.cs) to accept optional `replyToMessageId`.
- **Queries / DTOs:** Include `ReplyToMessageId` and a **reply preview** in message DTOs (e.g. `ReplyToMessagePreview`: Id, SenderId, SenderName, ContentSnippet, DeletedAt). In [GetMessagesQueryHandler](backend/src/Core.Application/Handlers/GetMessagesQueryHandler.cs) and group message handler, load reply-to message when present and map to preview (or null if deleted).
- **SignalR:** When broadcasting new message, include reply preview so clients can render without extra request.

**Frontend**

- **Types:** Add `replyToMessageId?: string` and `replyToMessagePreview?: { id, senderId, senderName, contentSnippet }` to Message type.
- **UI:** In chat input: “Reply” action on a message (e.g. from [MessageActions](backend/frontend/src/components/chat/MessageActions.tsx)) sets a “replyingTo” state and shows a small reply bar above the input; send includes `replyToMessageId`. In [MessageBubble](backend/frontend/src/components/chat/MessageBubble.tsx), when `message.replyToMessagePreview` exists, render a compact quoted line (sender + snippet) above the main content.
- **Hub:** Pass `replyToMessageId` when calling `SendMessage` / `SendGroupMessage`.

---

## 2. Reaction to a message (emoji, WhatsApp/Telegram style)

**Backend**

- **Domain:** New entity `MessageReaction`: MessageId, UserId, Emoji (string, e.g. "👍", "❤️"). One reaction per user per message (user can change emoji); unique constraint on (MessageId, UserId).
- **Persistence:** New table, FK to Message and User; unique index (MessageId, UserId). Migration.
- **Repository:** `IMessageReactionRepository` (AddOrUpdate, Remove, GetByMessageIds).
- **Application:** Commands: `AddOrUpdateReactionCommand(MessageId, UserId, Emoji)`, `RemoveReactionCommand(MessageId, UserId)`. Query: include reactions in message DTOs (e.g. list of { userId, userName?, emoji } per message).
- **API:** POST `api/Message/{messageId}/reaction` (body: { emoji }), DELETE `api/Message/{messageId}/reaction`. Authorize: user must have access to the conversation (DM or group member).
- **SignalR:** After adding/removing reaction, notify conversation (group or both users) so other clients update in real time (e.g. “MessageReactionUpdated” with messageId and new reaction list).

**Frontend**

- **Types:** Add `reactions?: { userId: string; userName?: string; emoji: string }[]` to Message.
- **UI:** In MessageBubble or MessageActions: show existing reactions under the message; click to add/change own reaction (emoji picker or fixed set: e.g. 👍❤️😂😮😢). Call API and/or Hub for add/remove; on “MessageReactionUpdated” update local message state.
- **Emoji set:** Start with a small fixed set (e.g. 5–8 emojis); optional later: full picker.

---

## 3. Forward a message to another person

**Backend**

- **Domain:** Add to Message: `ForwardedFromMessageId` (nullable Guid), `ForwardedAt` (nullable DateTime), optionally `ForwardedFromUserId` / `ForwardedFromName` for display (or derive from original message when loading).
- **Persistence:** Add columns; migration.
- **Application:** New command `ForwardMessageCommand(MessageId, TargetReceiverId?, TargetGroupId?, ForwardedByUserId)`. Handler: load source message; create a **new** message in the target conversation with same Content/Attachment* and set ForwardedFromMessageId, ForwardedAt; publish MessageSentEvent so target chat gets it. Validate: forwarder has access to source message and can send to target (DM or group member).
- **API:** POST `api/Message/{messageId}/forward` (body: `{ receiverId?: string, groupId?: string }`). One of receiverId or groupId required.
- **Queries / DTOs:** Add to MessageDto: `forwardedFromMessageId`, `forwardedAt`, and optionally `forwardedFromName` (or similar) for display. When loading messages, if forwarded, load original sender name for “Forwarded from X” (or hide for privacy; product decision).

**Frontend**

- **Types:** Add `forwardedFromMessageId?`, `forwardedAt?`, `forwardedFromName?` to Message.
- **UI:** In message actions: “Forward” opens a modal or drawer to choose a contact/group; on confirm call forward API. In MessageBubble: show a “Forwarded” label/icon and optionally “From X” when `forwardedFromName` is present.

---

## 4. Saved messages (Telegram-style space for user data)

**Backend**

- **Current behavior:** [SendMessageCommandHandler](backend/src/Core.Application/Handlers/SendMessageCommandHandler.cs) already treats “send to self” (ReceiverId == SenderId) and auto-creates a SavedMessage. So sending a message with receiverId = currentUserId is the “saved messages” chat.
- **API:** Ensure GET conversation supports “saved” as a pseudo-chat: e.g. GET `api/Message/saved-conversation` or `api/Message/{userId}` where userId = currentUserId returns messages where both SenderId and ReceiverId are current user (conversation with self). [GetMessagesQuery](backend/src/Core.Application/Queries/GetMessagesQuery.cs) / repository: add a path or reuse existing GetConversation with (currentUserId, currentUserId) to return those messages.
- **Recent chats:** [GetRecentChatsQuery](backend/src/Core.Application/Queries/GetRecentChatsQuery.cs) should include the “Saved Messages” chat (e.g. a special entry with a flag or same userId for both sides) so it appears in the chat list.

**Frontend**

- **UI:** “Saved Messages” is already toggled via `showSavedMessages` in [Chat.tsx](backend/frontend/src/pages/Chat.tsx). Ensure when `showSavedMessages` is true: (1) load messages from “saved” conversation (API that returns self-to-self messages); (2) send new messages with receiverId = current user id (so backend creates message and auto-saves). So: use a dedicated endpoint or same GetConversation(currentUserId, currentUserId) and SendMessage with receiverId = currentUserId. Add “Saved Messages” as a visible chat in the sidebar (e.g. Bookmark icon) that sets `showSavedMessages` and selects no user/group.
- **Optional:** Allow sending files/text only to saved (no receiver picker when in saved chat). Current send flow can already do this if the client sends to self.

---

## 5. Profile data in chat (click on photo/header to see user data and bio)

**Backend**

- **API:** Add GET `api/User/{userId}/profile` (or `api/User/public-profile/{userId}`) that returns **public** profile: FirstName, LastName, Username, ProfilePicture, Bio (and optionally PhoneNumber if you want; often hidden). Do not return PasswordHash, Email (or return email only if same user). Authorize: any authenticated user can view (or restrict to contacts only; product decision).
- **Application:** Reuse or extend `GetUserByIdQuery`; add a DTO or flag for “public profile” that maps only safe fields. If you already have GetUserById, add a handler that returns PublicProfileDto (no sensitive data).

**Frontend**

- **API client:** Add `getPublicProfile(userId: string)` calling GET `/User/{userId}/profile` (or the chosen route).
- **UI:** When user clicks on the chat header (name/photo) or on a message sender avatar in [Chat.tsx](backend/frontend/src/pages/Chat.tsx): open a drawer or modal showing that user’s public profile (photo, full name, username, bio). Use the new API. Same for group chats: clicking a member or the group header can show that user’s profile (and later, group info). No backend change for “group header” beyond having public profile for members.

---

## 6. Complete group features (leave group, admin, permissions)

**Backend**

- **Leave group:** New command `LeaveGroupCommand(GroupId, UserId)`. Handler: ensure user is a member; if user is the only admin, either forbid leave until admin is transferred or assign admin to another member (e.g. next admin or first member). Remove member via `Group.RemoveMember(userId)`; persist. API: POST `api/Group/{groupId}/leave`.
- **Remove member (by admin):** New command `RemoveMemberCommand(GroupId, RemoverUserId, MemberIdToRemove)`. Handler: remover must be admin; cannot remove the last admin (or define rule: e.g. group must have at least one admin). API: POST or DELETE `api/Group/{groupId}/members/{memberId}`.
- **Group permissions (optional but recommended):** Add a simple permission model. Option A: `GroupMember` already has `IsAdmin`; add `CanSendMessages` (default true). Option B: Add a `GroupRole` or permissions table (e.g. Admin, Member) and “members can send” / “only admins can send” as group-level settings. Minimal approach: keep IsAdmin; add `Group.AllowOnlyAdminToPost` (bool). If true, only admins can send group messages; handler for SendMessage (group) checks this.
- **Transfer admin (optional):** Command `TransferGroupAdminCommand(GroupId, CurrentAdminId, NewAdminUserId)`. Handler: current user must be admin; new user must be member; set Group.AdminId to new admin, set old admin’s GroupMember.IsAdmin = false, new member’s IsAdmin = true. API: POST `api/Group/{groupId}/transfer-admin` (body: { newAdminUserId }).
- **DTOs / queries:** Extend [GroupDto](backend/src/Core.Application/DTOs/GroupDto.cs) or group detail response to include member list with IsAdmin, so frontend can show “Admin” badge and “Remove”/“Leave” actions correctly.

**Frontend**

- **API:** Add `leaveGroup(groupId)`, `removeMember(groupId, memberId)`, optional `transferAdmin(groupId, newAdminUserId)`.
- **UI:** In group chat header or group info modal: “Leave group” button (calls leave; then redirect to chat list or previous chat). For admins: list members with “Remove” and “Make admin” / “Transfer admin” where applicable. If you add “only admins can post,” show a disabled input or notice for non-admins.
- **Group info screen:** A dedicated group info page or modal showing group name, admin, member list (with roles), and actions (leave, remove, transfer admin). This can be opened from the group chat header.

---

## 7. Pin messages in chat

**Backend**

- **Domain:** Add to Message: `IsPinned` (bool), `PinnedAt` (DateTime?), `PinnedByUserId` (Guid?). Alternatively, a separate `PinnedMessage` table (MessageId, PinnedAt, PinnedByUserId); the “conversation” is derived from the message’s ReceiverId/GroupId. Single table is simpler: one message belongs to one conversation, so pinning is per message.
- **Persistence:** Add columns to Message (IsPinned, PinnedAt, PinnedByUserId); migration.
- **Domain behavior:** Add `Message.Pin(pinnedByUserId)` and `Message.Unpin()`; set IsPinned, PinnedAt, PinnedByUserId. Rule: only one pinned per conversation, or allow multiple (like Telegram). Prefer multiple: no unique constraint; “pinned” is a list per chat.
- **Application:** Commands `PinMessageCommand(MessageId, UserId)`, `UnpinMessageCommand(MessageId, UserId)`. Handler: load message; verify user can pin (e.g. same user in DM, or admin/member in group—product rule); call Pin/Unpin; save. Query: when loading conversation, also return pinned message ids or a separate “GetPinnedMessages(ConversationKey)” where key is (userId, userId) for DM or (groupId) for group. Include pinned list in conversation response or separate endpoint GET `api/Message/pinned?userId=...&groupId=...`.
- **API:** POST `api/Message/{messageId}/pin`, DELETE `api/Message/{messageId}/pin`. GET pinned: e.g. GET `api/Message/pinned` with query params for DM (otherUserId) or group (groupId).

**Frontend**

- **Types:** Add `isPinned?`, `pinnedAt?`, `pinnedByUserId?` to Message; and/or a list of pinned message ids for the current chat.
- **UI:** In message actions: “Pin” (and “Unpin” if already pinned). At top of conversation, show a “Pinned messages” bar (e.g. “1 pinned message”) that expands or navigates to pinned list; or inline show pinned messages at top (compact) with “Unpin.” Load pinned list when opening a chat and update on pin/unpin (and via SignalR if you notify others).

---

## Implementation order suggestion


| Order | Feature             | Reason                                             |
| ----- | ------------------- | -------------------------------------------------- |
| 1     | Reply (1)           | Small schema + API change; high value              |
| 2     | Profile in chat (5) | One endpoint + modal; no schema                    |
| 3     | Reactions (2)       | New table + API; good UX                           |
| 4     | Forward (3)         | Message columns + one command                      |
| 5     | Pin (7)             | Message columns + pin/unpin API                    |
| 6     | Saved messages (4)  | Mostly frontend + one conversation type            |
| 7     | Group complete (6)  | Leave, remove, optional permissions/admin transfer |


---

## File/touchpoint summary

- **Domain:** Message (ReplyToMessageId, ForwardedFromMessageId/ForwardedAt, IsPinned/PinnedAt/PinnedByUserId); MessageReaction entity; Group (optional AllowOnlyAdminToPost); GroupMember (IsAdmin already); User unchanged.
- **Infrastructure:** Migrations for Message new columns, MessageReaction table; IMessageReactionRepository; GroupCall if not yet done.
- **Application:** New commands: ForwardMessage, PinMessage, UnpinMessage, LeaveGroup, RemoveMember, (TransferAdmin, AddOrUpdateReaction, RemoveReaction). New/queries: GetPinnedMessages, public profile by userId. Extend SendMessageCommand (replyToMessageId), GetMessages/GetGroupMessages (reply preview, reactions, forwarded, pinned).
- **WebAPI:** MessageController: forward, pin, unpin, reaction endpoints; UserController: GET profile by id; GroupController: leave, remove member, transfer-admin. ChatHub: optional real-time for reactions/pin. SignalR notifications for reaction/pin if you want live updates.
- **Frontend:** Message type extensions; MessageBubble (reply preview, reactions, forwarded label, pin action); MessageActions (reply, react, forward, pin); chat input (reply bar); profile modal/drawer (public profile); group info (members, leave, remove, transfer admin); saved messages (conversation with self + sidebar entry); pinned bar or list in chat.

This keeps all features aligned with your existing DDD/CQRS structure and current Message/Group/User design.