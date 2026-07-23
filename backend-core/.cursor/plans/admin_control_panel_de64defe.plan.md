---
name: Admin Control Panel
overview: "Add a full-featured Admin/Control Panel inside the same backend and frontend codebase: dedicated /admin area with separate layout and admin auth, DDD bounded contexts (Administration, Moderation, Limits), CQRS commands/queries and read models, user/chat/message management, configurable limits with domain enforcement, audit logging, and role-based access (Admin, Moderator, SuperAdmin)."
todos: []
isProject: false
---

# Admin / Control Panel – full implementation plan

## Scope and constraints

- **Same project:** Backend and frontend remain one solution; admin is a **clearly separated area** (routes, layout, API prefix, auth policy).
- **No anemic domain:** All admin and limit business rules live in the Domain layer; controllers and UI only orchestrate and display.
- **Full CQRS:** Every admin write is a Command; every admin read is a Query with dedicated read models/handlers. SystemMessageController’s direct DbContext use should be migrated to CQRS as part of this work (optional but recommended).
- **Existing stack:** .NET 8, EF Core, PostgreSQL, MediatR, React (Vite) frontend. JWT auth with `userId` (and optionally role) in claims.

---

## 1. Bounded contexts (DDD)

Introduce or formalize these **bounded contexts** for the admin area:


| Context                  | Responsibility                     | Key aggregates / entities                            |
| ------------------------ | ---------------------------------- | ---------------------------------------------------- |
| **Identity** (existing)  | Users, auth, profile               | User (extended with status and role)                 |
| **Messaging** (existing) | Chats, messages, groups            | Message, Group, GroupMember, SavedMessage            |
| **Administration**       | Admin users, roles, audit          | AdminAuditLog (new), User (status/role)              |
| **Moderation**           | Admin actions on content and users | Same Message/User; actions recorded in AdminAuditLog |
| **Limits**               | Quotas and overrides               | SystemLimit, UserLimitOverride (new)                 |


- **Identity:** Extend `User` with `IsDisabled`, `SuspendedUntil`, and explicit role handling (Admin, Moderator, SuperAdmin, User). Add domain methods: `Disable()`, `Suspend(until)`, `Reactivate()`, `SetRole(role)` (with invariants: at least one SuperAdmin, etc.).
- **Administration:** New entity `AdminAuditLog`: Id, AdminUserId, Action (string or enum), ResourceType (User, Message, Chat, Limit, etc.), ResourceId, Details (JSON or string), Timestamp. Persisted by command handlers after each admin write.
- **Limits:** New aggregates: `SystemLimit` (Key, Value, Description) for global defaults (e.g. MaxPinnedMessages, MaxSavedMessagesSizeBytes, MaxChatDataSizeBytes). `UserLimitOverride` (UserId, LimitKey, Value) for per-user overrides. Domain or application service resolves effective limit for a user (override if present, else system default). Enforce in existing or new command handlers (e.g. PinMessage, SaveMessage, SendMessage) before performing the operation.

---

## 2. Domain layer changes

**User ([Core.Domain/Entities/User.cs](backend/src/Core.Domain/Entities/User.cs))**

- Add: `IsDisabled` (bool), `SuspendedUntil` (DateTime?).  
- Add: `Disable()`, `Suspend(DateTime until)`, `Reactivate()`, `SetRole(string role)`.  
- Enforce: cannot disable/suspend self; SuperAdmin role change rules (e.g. cannot remove last SuperAdmin).  
- Role: treat as invariant; valid values: User, Moderator, Admin, SuperAdmin (constants in domain).

**New: AdminAuditLog**

- Properties: Id, AdminUserId, Action, ResourceType, ResourceId (nullable), Details (string or JSON), CreatedAt.  
- Created by command handlers (not by domain events from User/Message) so admin actions are always logged.

**New: SystemLimit**

- Key (string, unique), Value (decimal or long for counts/bytes), Description (optional).  
- Represents global default for a limit (e.g. MaxPinnedMessages = 10).

**New: UserLimitOverride**

- UserId, LimitKey, Value. Unique (UserId, LimitKey).  
- Override for a specific user; if missing, use SystemLimit.

**Message**

- Keep existing `Delete()` (soft delete with DeletedAt). For “admin delete” use the same method and log in AdminAuditLog who performed it. Optional: add `AdminDeletedByUserId` (nullable) for traceability; if not, audit log is enough.  
- Add optional `EditByAdmin(newContent, adminUserId)` that updates content and records in audit, or reuse `Edit()` and log in command handler.

**Pinned messages (if not yet implemented)**

- If you add pinning (from chat features plan), add domain logic and enforce MaxPinnedMessages from Limits when pinning.

**Saved messages size**

- When saving a message or loading saved list, compute total size (e.g. message content + attachment size) and enforce MaxSavedMessagesSizeBytes in the command handler by calling a limit resolution service.

---

## 3. CQRS – commands (write side)

All in **Core.Application** (or an **Administration** namespace). Handlers load aggregates, call domain methods, persist, write audit log, and publish domain events where applicable.

**User management**

- `DisableUserCommand(UserId, AdminUserId)` – load User, call Disable(), save, log.  
- `SuspendUserCommand(UserId, Until, AdminUserId)`.  
- `ReactivateUserCommand(UserId, AdminUserId)`.  
- `SetUserRoleCommand(UserId, Role, AdminUserId)` – enforce “last SuperAdmin” etc. in domain.  
- `AdminUpdateUserCommand(UserId, FirstName, LastName, Email, Bio, AdminUserId)` – update profile fields, log.

**Chat and message management**

- `AdminEditMessageCommand(MessageId, NewContent, AdminUserId)` – load Message, call Edit (or EditByAdmin), save, log.  
- `AdminDeleteMessageCommand(MessageId, HardDelete: bool, AdminUserId)` – soft delete (Message.Delete()) or hard delete (remove from DB); log.  
- Optional: `AdminDeleteChatCommand(ConversationKey, AdminUserId)` – define what “delete chat” means (e.g. soft-delete all messages in a DM); implement if required.

**Limits**

- `SetSystemLimitCommand(LimitKey, Value, AdminUserId)` – upsert SystemLimit, log.  
- `SetUserLimitOverrideCommand(UserId, LimitKey, Value, AdminUserId)` – upsert or remove UserLimitOverride, log.  
- `RemoveUserLimitOverrideCommand(UserId, LimitKey, AdminUserId)`.

**Audit**

- Every admin command handler appends one row to **AdminAuditLog** (via repository or dedicated service). Use async, non-blocking insert so it does not slow the response.

---

## 4. CQRS – queries (read side)

**Optimized read models and query handlers** (no business logic; only projection and filtering).

**User management**

- `GetAdminUserListQuery(Page, PageSize, SearchTerm, RoleFilter, IsDisabledFilter, SortBy, SortDesc)` → `PagedResult<AdminUserListDto>` (Id, Username, FirstName, LastName, Role, IsDisabled, SuspendedUntil, CreatedAt, MessageCount, ChatCount, SavedMessagesCount, StorageUsedBytes – if available).  
- `GetAdminUserDetailQuery(UserId)` → `AdminUserDetailDto` (full profile + same stats + limit overrides for that user).  
- Use **AsNoTracking**, single query with joins/aggregates or separate lightweight queries to avoid N+1. Consider a dedicated read repository or SQL view for heavy dashboard lists.

**Chat and message management**

- `GetAdminChatListQuery(Page, PageSize, SearchTerm, TypeFilter: Dm | Group)` → list of “chats” (DM: two user ids + last message; Group: group id + name + last message). Implement as a query that aggregates from Messages and Groups.  
- `GetAdminChatDetailQuery(ConversationKey)` → messages (or summary) for that chat; support cursor pagination.  
- `SearchMessagesQuery(Page, PageSize, ContentSearch, SenderId, GroupId, DateFrom, DateTo)` → `PagedResult<AdminMessageDto>` (Id, SenderId, ReceiverId, GroupId, Content, SentAt, IsEdited, DeletedAt, etc.).  
- Handlers use indexes (e.g. Message.SentAt, GroupId, SenderId); avoid full table scan.

**Limits**

- `GetSystemLimitsQuery()` → list of SystemLimit (Key, Value, Description).  
- `GetUserLimitOverridesQuery(UserId)` → list of overrides for that user.  
- `GetEffectiveLimitsQuery(UserId)` → computed effective limits (system + overrides) for display.

**Monitoring and audit**

- `GetSystemStatsQuery()` → `SystemStatsDto` (TotalUsers, TotalMessages, TotalGroups, StorageUsed, UsersOverLimit, etc.).  
- `GetUsersOverLimitQuery(LimitKey)` → users whose current usage exceeds their effective limit.  
- `GetAdminAuditLogQuery(Page, PageSize, AdminUserId, Action, ResourceType, From, To)` → `PagedResult<AdminAuditLogDto>`.

---

## 5. Backend – API and auth

**Base URL and area**

- All admin endpoints under `**/api/admin/...**` (e.g. `[Route("api/admin/[controller]")]` or a single `AdminController` with sub-routes). Keep existing `/api/*` for user area.

**Authorization**

- Define **policy** `"Admin"` requiring one of: Admin, Moderator, SuperAdmin (e.g. claim `role` or `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`).  
- Ensure **JWT** includes role (e.g. in LoginCommandHandler / token generation add role claim).  
- Apply `[Authorize(Policy = "Admin")]` on all admin controllers or a base class.  
- **No separate “admin login” backend:** Reuse existing `POST /api/Auth/login`. Admin panel uses same token; only the **frontend** shows a dedicated admin login page at `/admin/login` and redirects to `/admin` after login if user has admin role.

**Controllers (thin)**

- **AdminUserController:** GET list, GET detail, PUT update, POST disable/suspend/reactivate, POST set-role. Each action sends a Command or Query via MediatR and returns result.  
- **AdminChatController:** GET chats, GET chat detail (messages).  
- **AdminMessageController:** GET search, PUT edit, DELETE (soft/hard).  
- **AdminLimitsController:** GET system limits, GET user overrides, PUT system limit, PUT user override, DELETE user override.  
- **AdminMonitoringController:** GET system stats, GET users over limit.  
- **AdminAuditController:** GET audit log (paginated, filtered).  
- No business logic in controllers; only parameter binding, MediatR send, and HTTP response.

**Audit**

- In command handlers: after successful save, call `IAdminAuditLogService.Append(adminUserId, action, resourceType, resourceId, details)`. Implement as repository insert. Optionally use a MediatR behavior that logs every admin command (command type + payload) for extra traceability.

**Thread safety and async**

- All handlers and repositories async; pass CancellationToken; use scoped DbContext; no shared mutable state. Same as existing production-readiness plan.

---

## 6. Infrastructure

**Persistence**

- New tables: **AdminAuditLogs**, **SystemLimits**, **UserLimitOverrides**.  
- User: add columns **IsDisabled**, **SuspendedUntil**.  
- EF configuration in ApplicationDbContext; new migrations.  
- Repositories: `IAdminAuditLogRepository`, `ISystemLimitRepository`, `IUserLimitOverrideRepository` (or generic key-value for limits).  
- Indexes: AdminAuditLogs (AdminUserId, CreatedAt), (ResourceType, ResourceId); SystemLimits (Key); UserLimitOverrides (UserId, LimitKey).

**Limit resolution**

- `ILimitResolutionService` (application layer): `GetEffectiveLimit(userId, limitKey)`. Reads SystemLimit and UserLimitOverride; returns value. Used by PinMessage, SaveMessage, and any “usage” command to enforce quotas.  
- Optional: cache system limits (short TTL) and per-user overrides to reduce DB hits at scale.

---

## 7. Frontend – admin area

**Routing**

- **Path prefix:** All admin UI under `**/admin**`.  
- **Routes:**  
  - `/admin` or `/admin/dashboard` – dashboard (system stats, quick links).  
  - `/admin/login` – admin login page (same credentials as main app; redirect to `/admin` on success if role is admin; otherwise show “Forbidden”).  
  - `/admin/users` – user list (paginated, filters).  
  - `/admin/users/:id` – user detail (profile, stats, limits, actions: edit, disable, suspend, set role).  
  - `/admin/chats` – chat list (DMs + groups).  
  - `/admin/chats/:key` – chat detail / message list.  
  - `/admin/messages` – message search (filters, pagination).  
  - `/admin/limits` – system limits and per-user overrides (view/edit).  
  - `/admin/monitoring` – system stats, users over limit.  
  - `/admin/audit` – audit log (filters, pagination).

**Layout and navigation**

- **Admin layout:** Separate layout component used only for routes under `/admin` (except `/admin/login`).  
- Sidebar or top nav: Dashboard, Users, Chats, Messages, Limits, Monitoring, Audit Log, (Logout → back to main app or admin logout).  
- Clear visual separation from the main user UI (different theme or section label).

**Auth**

- **AdminRoute (or equivalent):** For any `/admin/*` except `/admin/login`, check: 1) user is authenticated (same token as main app), 2) user role is Admin, Moderator, or SuperAdmin (from token or GET /api/User/profile). If not, redirect to `/admin/login` or show “Access denied”.  
- **Admin login page:** Username + password; call `POST /api/Auth/login`; store token; then GET profile or decode role from token; if admin role, redirect to `/admin`; else show “You do not have admin access”.

**API client**

- Reuse existing HTTP client with same base URL and auth header. Add **admin API helpers** (e.g. `adminApi.getUserList()`, `adminApi.getSystemStats()`, `adminApi.setSystemLimit()`, etc.) that call `/api/admin/*` endpoints.  
- Handle 403 (forbidden) globally for admin routes (e.g. redirect to `/admin/login` or show error).

**UI behavior**

- **Pagination:** All lists (users, chats, messages, audit) use server-side pagination (page, pageSize) and optional filters.  
- **Filtering and search:** Users (by role, status, search term); messages (content, sender, date range); audit (admin user, action, date).  
- **Destructive actions:** Disable user, delete message, hard delete: always **confirmation dialog** (e.g. “Are you sure? This will …”).  
- **Limits:** Form to edit system limit (key, value); form to add/edit/remove user override. Show effective limit per user on user detail.  
- **Monitoring:** Cards or tables for total users, messages, storage; list of users over limit with link to user detail.  
- **Audit log:** Table with columns Admin, Action, Resource, Time, Details; filters and export (optional).

**Performance**

- Lazy load tabs or heavy data; use pagination and debounced search where applicable.  
- Admin-only endpoints can use slightly higher timeouts if needed for heavy reports; keep default timeouts for consistency.

---

## 8. Limits enforcement (domain and application)

**Where to enforce**

- **Domain:** Invariants (e.g. “suspended user cannot send message”) can be enforced in User or in command handlers by loading User and checking before performing the action.  
- **Application:** Resolve effective limit via `ILimitResolutionService` inside the handler of the **user-facing** command (e.g. SaveMessageCommand, future PinMessageCommand). Before adding a save or a pin, compute current usage (e.g. count of saved messages or total size, count of pinned messages), get effective limit, and throw a domain or application exception if over limit.  
- **Graceful handling:** API returns 400/409 with a clear message (“Saved messages storage limit reached”); frontend shows a toast or inline message. Admin can raise the user’s override or system default.

**Limit keys (examples)**

- `MaxPinnedMessages` (int).  
- `MaxSavedMessagesSizeBytes` (long).  
- `MaxSavedMessagesCount` (int).  
- `MaxChatDataSizeBytes` (long) – optional; define “chat data” (e.g. sum of message + attachment size for a user’s conversations).  
- Add others as needed; store in SystemLimit with sensible defaults and allow admin to change.

---

## 9. Implementation order (suggested)


| Phase | Focus                    | Deliverables                                                                                                                                                                                                                    |
| ----- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1     | Domain and auth          | User status (IsDisabled, SuspendedUntil), SetRole; AdminAuditLog, SystemLimit, UserLimitOverride; role in JWT; Admin policy.                                                                                                    |
| 2     | Admin commands and audit | Disable/Suspend/Reactivate/SetRole/AdminUpdateUser; AdminEditMessage, AdminDeleteMessage; SetSystemLimit, SetUserLimitOverride; audit log insert in handlers.                                                                   |
| 3     | Admin queries and API    | GetAdminUserList, GetAdminUserDetail; GetAdminChatList, GetAdminChatDetail, SearchMessages; GetSystemLimits, GetUserLimitOverrides, GetEffectiveLimits; GetSystemStats, GetUsersOverLimit; GetAdminAuditLog; admin controllers. |
| 4     | Limits enforcement       | ILimitResolutionService; enforce in SaveMessage (and future Pin) commands; graceful API and UI handling.                                                                                                                        |
| 5     | Frontend admin area      | /admin routes, AdminLayout, admin login, Dashboard, Users, UserDetail, Chats, Messages, Limits, Monitoring, Audit; admin API client and AdminRoute.                                                                             |
| 6     | Refinements              | Migrate SystemMessageController to CQRS (optional); add indexes for admin queries; caching for system limits.                                                                                                                   |


---

## 10. File and project summary

**Core.Domain**

- User: add IsDisabled, SuspendedUntil; methods Disable, Suspend, Reactivate, SetRole.  
- New: AdminAuditLog, SystemLimit, UserLimitOverride (entities and/or value objects as needed).  
- Message: optional EditByAdmin or rely on Edit + audit.

**Core.Application**

- New commands: DisableUser, SuspendUser, ReactivateUser, SetUserRole, AdminUpdateUser, AdminEditMessage, AdminDeleteMessage, SetSystemLimit, SetUserLimitOverride, RemoveUserLimitOverride.  
- New queries: GetAdminUserList, GetAdminUserDetail, GetAdminChatList, GetAdminChatDetail, SearchMessages, GetSystemLimits, GetUserLimitOverrides, GetEffectiveLimits, GetSystemStats, GetUsersOverLimit, GetAdminAuditLog.  
- New DTOs: AdminUserListDto, AdminUserDetailDto, AdminMessageDto, AdminAuditLogDto, SystemStatsDto, etc.  
- New: IAdminAuditLogRepository, ILimitResolutionService (and implementations).  
- Enforce limits in existing SaveMessage (and future Pin) handler via ILimitResolutionService.

**Infrastructure**

- AdminAuditLogRepository, SystemLimitRepository, UserLimitOverrideRepository.  
- DbSets and migrations for new tables and User columns.  
- LimitResolutionService (reads SystemLimit + UserLimitOverride).

**WebAPI**

- Admin policy (role in [Admin, Moderator, SuperAdmin]).  
- Controllers under `/api/admin`: AdminUserController, AdminChatController, AdminMessageController, AdminLimitsController, AdminMonitoringController, AdminAuditController.  
- Optionally: MediatR behavior to log admin commands to audit.

**Frontend**

- Routes: /admin, /admin/login, /admin/users, /admin/users/:id, /admin/chats, /admin/chats/:key, /admin/messages, /admin/limits, /admin/monitoring, /admin/audit.  
- AdminLayout, AdminRoute, AdminLogin page.  
- Pages: Dashboard, UserList, UserDetail, ChatList, ChatDetail, MessageSearch, Limits, Monitoring, AuditLog.  
- Admin API client and confirmation dialogs for destructive actions.

This yields a full-featured admin panel inside the same project, with clear separation from the user area, DDD bounded contexts, strict CQRS, configurable limits enforced in the domain/application layer, and audit logging for production use.