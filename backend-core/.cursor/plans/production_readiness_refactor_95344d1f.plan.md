---
name: Production Readiness Refactor
overview: "Refactor the .NET backend to be production-ready for millions of users and high throughput: enforce full DDD + CQRS with bounded contexts and read/write separation, introduce AutoMapper/Mapster profile-based mapping, ensure thread safety and correct async, apply DbContext/query optimizations and caching, eliminate N+1, add indexing and pagination, and align with SOLID and clean architecture for horizontal scaling."
todos:
  - id: todo-1770599024967-lw9cqc01l
    content: ""
    status: pending
isProject: false
---

# Production-ready refactor: scalability, DDD/CQRS, performance

## Current state (findings)

- **DDD:** Domain has entities (Message, Call, Group, User, etc.) with some behavior and domain events; one Value Object (PhoneNumber) exists but **User** uses `string` for phone. No formal Aggregate Root boundaries; repositories are per-entity. **RecentChatResult** lives in Domain but is a read-model shape (leak).
- **CQRS:** Commands and Queries exist (MediatR), but **same repositories and entities** serve both reads and writes. No separate read models or read-side repositories; query handlers load aggregates and map manually to DTOs.
- **Mapping:** **AutoMapper** is referenced in [Core.Application.csproj](backend/src/Core.Application/Core.Application.csproj) but **not used**; all handlers use `new Dto { ... }` (e.g. [GetGroupMessagesQueryHandler](backend/src/Core.Application/Handlers/GetGroupMessagesQueryHandler.cs), [GetUserByIdQueryHandler](backend/src/Core.Application/Handlers/GetUserByIdQueryHandler.cs), [GetRecentChatsQueryHandler](backend/src/Core.Application/Queries/GetRecentChatsQueryHandler.cs)).
- **Thread safety:** [ConnectionManager](backend/src/WebAPI/Services/ConnectionManager.cs) and [OnlineUserService](backend/src/WebAPI/Services/OnlineUserService.cs) use **ConcurrentDictionary** (good). No locks; minor race in OnlineUserService (stale zero-count) acceptable for “is online” display.
- **Async:** No `.Result` or `.Wait()` found. I/O is async. **CancellationToken** is not passed through in many repository methods or from API to handlers.
- **DbContext:** Registered **Scoped** (correct). Repositories and UnitOfWork are Scoped. No explicit read-only context for queries.
- **Queries:** [MessageRepository](backend/src/Infrastructure/Repositories/MessageRepository.cs) uses **AsNoTracking()** in GetUnreadCountsAsync and GetRecentChatsAsync; **GetConversationAsync** and **GetGroupMessagesAsync** do **not** (tracking when not needed). [UserRepository](backend/src/Infrastructure/Repositories/UserRepository.cs), [ContactRepository](backend/src/Infrastructure/Repositories/ContactRepository.cs), [CallRepository](backend/src/Infrastructure/Repositories/CallRepository.cs), [GroupRepository](backend/src/Infrastructure/Repositories/GroupRepository.cs), [SavedMessageRepository](backend/src/Infrastructure/Repositories/SavedMessageRepository.cs): **no AsNoTracking** on read-only methods. **N+1:** [GetRecentChatsQueryHandler](backend/src/Core.Application/Queries/GetRecentChatsQueryHandler.cs) calls **GetByIdAsync** and **IsUserOnline** per chat (clear N+1).
- **Indexing:** [ApplicationDbContext](backend/src/Infrastructure/Persistence/ApplicationDbContext.cs) has some indexes (Message: SentAt, SenderId+ReceiverId, GroupId+SentAt; User: Username, PhoneNumber; etc.). Missing: composite indexes for common read patterns (e.g. ReceiverId+IsRead+SentAt for unread), and no covering indexes for hot queries.
- **Caching:** No **IMemoryCache** or **IDistributedCache** usage. Session key manager comment mentions “consider Redis.”
- **Pagination:** Some endpoints use page/pageSize; **cursor-based** pagination not used (important for large feeds).
- **Transactions:** [UnitOfWork](backend/src/Infrastructure/Persistence/UnitOfWork.cs) has Begin/Commit/Rollback; **InitiateCallCommandHandler** uses UnitOfWork but most handlers only call SaveChangesAsync (no explicit transaction scope). No consistent “one transaction per command” pattern documented.

---

## 1. Bounded contexts and DDD alignment

**Goal:** Clear bounded contexts, aggregate roots, value objects, domain logic only in domain.

- **Define bounded contexts (BCs):** e.g. **Messaging** (Message, Group, GroupMember, SavedMessage), **Identity/Auth** (User, Contact), **Communication** (Call, CallParticipant if added). Each BC has its own aggregates; avoid cross-BC entity references in domain (use IDs and application services to coordinate).
- **Aggregate roots:** Message (with no child entity for now), Group (root; GroupMember as part of aggregate or child entity), User, Call. Repositories only by aggregate root; loading Group with Members is correct. Document which entities are roots and enforce “one repository per aggregate root.”
- **Value objects:** Use **PhoneNumber** (or similar) in User and Contact where applicable; replace raw strings with value objects so validation and equality live in domain. Introduce value objects for **Content** (message content with max length), **CallStatus**, etc., if they carry invariants.
- **Domain logic in domain only:** Move any validation that is “business rule” from handlers into entity/value object methods (e.g. Message.Edit checks DeletedAt; Group.AddMember checks duplicates). Handlers orchestrate: load aggregate, call method, persist, publish events.
- **RecentChatResult:** Move out of Domain into Application (read model) or a dedicated ReadModel project; Domain must not depend on read shapes.
- **Anemic model fixes:** Ensure all state changes go through domain methods (no setter-based updates from handlers); use factory methods or constructors for creation.

**Why:** Clear boundaries reduce coupling and make scaling (e.g. splitting services later) and testing easier. Business logic in domain ensures consistency and single place of truth.

---

## 2. True CQRS: separate read and write models

**Goal:** Write path uses aggregates and domain; read path uses dedicated read models and optimized queries, no domain load for pure reads.

- **Write side (current):** Keep commands, command handlers, aggregate repositories, UnitOfWork, domain events. Ensure **one transaction per command** (UnitOfWork.SaveChangesAsync after all changes; use explicit BeginTransactionAsync only when multiple operations must be atomic). Add **CancellationToken** to all repository and handler signatures and pass from API.
- **Read side:** Introduce **read models** (DTOs/projection classes) that match API/UI needs. Do **not** return domain entities from query handlers.
  - **Option A (in-process):** New project or folder **Core.Application.ReadModels** (or **ReadModel**) with query-specific DTOs. **IReadOnlyMessageRepository** (or **IMessageQueryRepository**) that returns **MessageReadDto** (and similar) using **AsNoTracking()** and **EF Core projections** (`.Select(...)` to DTO) to avoid loading full aggregates. Query handlers use these read repositories only.
  - **Option B (eventual consistency):** Separate read store (e.g. denormalized tables or views) updated by domain event handlers; query handlers read from that store. More work but scales reads independently.
  - **Recommendation for your scale:** Start with Option A: same DB, separate query repositories and projection DTOs, AsNoTracking + Select. Enables optimized, index-friendly queries without changing write model.
- **Read repositories:** e.g. **MessageQueryRepository** with methods like **GetConversationPageAsync(userId1, userId2, limit, before, ct)** returning **IReadOnlyList&lt;MessageReadDto&gt;** and **GetRecentChatsAsync(userId, limit, ct)** returning **IReadOnlyList&lt;RecentChatReadDto&gt;** using a **single** optimized query with joins/projections (eliminate N+1). No Include of full entities for read side; only columns needed for DTO.
- **Write repositories:** Keep **IMessageRepository** (and others) for commands; they work with **Message** aggregate, may use tracking for updates. Clear naming: **IMessageRepository** (write) vs **IMessageQueryRepository** (read) or **IReadOnlyMessageRepository**.

**Why:** Read-heavy workloads (chats, feeds, history) can be optimized with projections and indexes; write path stays consistent and simple. Separation avoids accidental loading of large graphs for simple reads.

---

## 3. Object mapping: AutoMapper or Mapster (profile-based)

**Goal:** No manual `new Dto` in handlers; centralized, profile-based mapping; performance- and maintainability-optimized.

- **Choose:** Keep **AutoMapper** (already referenced) or switch to **Mapster** (often faster, less reflection). If staying with AutoMapper: add **AutoMapper.Extensions.Microsoft.DependencyInjection** and register profiles.
- **Profiles:** Create mapping profiles per aggregate or per use case, e.g. **UserMappingProfile** (User -> UserDto, User -> PublicProfileDto), **MessageMappingProfile** (Message -> MessageDto, Message -> MessageReadDto), **CallMappingProfile**, **GroupMappingProfile**, **RecentChatMappingProfile**, etc. Use **ProjectTo** in read repositories for EF Core (projection at DB level) where the read model matches a projection.
- **Where to map:** Command handlers: map **command -> domain** only if needed; after loading aggregate, map **aggregate -> event payload** or **aggregate -> DTO** only when returning a DTO from a command (e.g. CreateGroup returns groupId; no mapping). Query handlers: inject **IMapper** (or **IMapsterMapper**); get **IReadOnlyList&lt;MessageReadDto&gt;** from query repository (which may use **ProjectTo&lt;MessageReadDto&gt;** in the query) so mapping happens in one place (profile) or in DB projection. Remove all **new MessageDto { ... }** and similar from handlers.
- **Configuration:** Validate mapping at startup (AutoMapper: **AssertConfigurationIsValid()**). Avoid mapping cycles and unnecessary maps; keep DTOs flat where possible for performance.

**Why:** Centralized mapping reduces duplication, avoids errors when adding fields, and allows optimization (e.g. ProjectTo). Ban on manual `new` keeps the codebase consistent and maintainable.

---

## 4. Thread safety and concurrency strategy

**Goal:** Full thread safety; no shared mutable state; minimal locking.

- **ConnectionManager:** Already uses **ConcurrentDictionary**; **GetConnectionsForUser** returns **.Keys.ToList()** (snapshot). Document that this is safe. Consider **ImmutableArray** or **IReadOnlyList** for return types to signal immutability.
- **OnlineUserService:** ConcurrentDictionary is correct. Fix **UserDisconnected** so that after decrement you only remove when count is 0 (current logic is OK; optional: use **AddOrUpdate** with a delegate that returns 0 to remove). **GetOnlineUserIds** should filter **Value > 0** and return a snapshot (e.g. `.ToList()`).
- **Stateless services:** All MediatR handlers, application services, and repositories must be **stateless** (no instance fields that are mutated per request). DbContext is scoped so each request gets its own; no cross-request state.
- **Locks:** Avoid **lock** unless necessary (e.g. a single in-process rate limiter). If needed, use **SemaphoreSlim** for async and document the critical section.
- **Document:** Add a short “Concurrency and multithreading” section to the docs: stateless services, ConcurrentDictionary for shared caches, DbContext per request, no static mutable state.

**Why:** Under high concurrency, shared mutable state causes subtle bugs. Explicit strategy and stateless design make horizontal scaling safe.

---

## 5. Async and CancellationToken

**Goal:** All I/O async; no blocking; CancellationToken propagated; no fake async.

- **Propagate CancellationToken:** Add **CancellationToken cancellationToken = default** to every repository method, **IUnitOfWork.SaveChangesAsync(CancellationToken)**, and all MediatR handlers. Controllers and SignalR hub methods receive **CancellationToken** from the framework; pass it into **IMediator.Send(..., cancellationToken)**. This allows request cancellation and avoids wasted work.
- **No .Result / .Wait:** Enforce via code review or analyzer; none found currently.
- **DbContext:** Ensure all **SaveChangesAsync**, **ToListAsync**, **FirstOrDefaultAsync**, etc., use the passed **CancellationToken** so that long-running queries and saves can be cancelled.

**Why:** Request cancellation and consistent async prevent thread-pool starvation and improve responsiveness under load.

---

## 6. DbContext and repository performance

**Goal:** Correct lifetime; read-only queries never track; no N+1; connection pooling; compiled queries where beneficial.

- **Lifetime:** Keep DbContext **Scoped** per request. Ensure **one scope per request** (default in ASP.NET Core). Do not hold DbContext in singletons or long-lived services.
- **Connection string:** Use **Max Pool Size** (e.g. 100) and **Min Pool Size** if needed; **Connection Idle Lifetime** for long-running apps. Rely on Npgsql connection pooling.
- **AsNoTracking for all read-only queries:** In every repository method that only reads (GetConversationAsync, GetGroupMessagesAsync, GetByIdAsync when used by query handlers, GetContactsByUserIdAsync, GetGroupWithMembersAsync when used for read-only checks, GetByUserIdAsync in SavedMessageRepository, GetCallsByUserIdAsync, GetUsersAsync, etc.), use **.AsNoTracking()** (or **.AsNoTrackingWithIdentityResolution()** only if you need identity resolution). This reduces memory and change-tracking overhead.
- **Separate read repositories:** As in section 2, read-side repositories use **AsNoTracking()** and **Select** to DTO/projection only. Write-side repositories use tracking only when loading an aggregate to modify.
- **Compiled queries:** For hot paths (e.g. GetConversationAsync, GetUnreadCountsAsync), consider **EF.CompileQuery** (EF Core compiled query) so the plan is cached. Apply after read repositories are in place.
- **GetByIdAsync (read):** For query handlers that only need to display data, provide **GetByIdNoTrackingAsync** or use the read repository that projects to DTO. Avoid tracking when not updating.

**Why:** AsNoTracking and projections dramatically reduce memory and CPU for read-heavy traffic. Compiled queries reduce plan compilation overhead on hot paths.

---

## 7. N+1 elimination and indexing

**Goal:** No N+1 in hot paths; indexes aligned with query patterns.

- **GetRecentChatsQueryHandler N+1:** Replace the loop that calls **GetByIdAsync** (group/user) and **IsUserOnline** per chat with a **single** read-model query: e.g. one SQL that returns recent chats with **group name**, **other user id**, **last message**, **unread count**, and optionally **is_online** (if you store presence in DB or join with a small cache). If **is_online** comes only from OnlineUserService (in-memory), batch: **GetOnlineUserIds()** once, then in-memory join when building DTOs. So: one query for recent chats (with joins/projections), one call to get online ids, no per-chat DB calls.
- **MessageRepository.GetConversationAsync / GetGroupMessagesAsync:** Already a single query; add **AsNoTracking()**. If you add reply preview, use a single query with a left join to the reply-to message (or a subquery) and project to DTO.
- **Indexing strategy:**
  - **Messages:** Composite index **(ReceiverId, IsRead, SentAt)** for unread/conv listing; **(GroupId, SentAt)** exists; consider **(SenderId, ReceiverId, SentAt)** for DM conversation.
  - **Calls:** **(CallerId, StartTime)** and **(ReceiverId, StartTime)** for “active call” and history.
  - **Contacts:** **(OwnerUserId, ContactUserId)** unique if not already.
  - **SavedMessages:** **(UserId, SavedAt)** for “saved messages” list.
  - Add indexes only after measuring; avoid over-indexing (writes get cost).
- **Include vs projection:** For read path, prefer **Select** to DTO/projection over **Include** of full entities. Use Include only on write path when loading aggregate with children for update.

**Why:** N+1 kills throughput under load; one round-trip per chat is unacceptable at scale. Indexes make hot queries predictable and fast.

---

## 8. Caching (in-memory and Redis-ready)

**Goal:** Cache hot, read-heavy data; design for Redis later without code churn.

- **Abstraction:** Introduce **ICacheService** or **IDistributedCache** (from Microsoft.Extensions.Caching.Abstractions) with two implementations: **MemoryCacheService** (wraps IMemoryCache) and **RedisCacheService** (wraps IDistributedCache with Redis). Register the one to use via config. All caching goes through this abstraction so switching to Redis is config-only.
- **What to cache:** User public profile (by userId); optional: “recent chats” per user with short TTL (e.g. 30–60 s); optional: unread counts. **Do not** cache full conversation history (too large and volatile). Cache keys: e.g. `user:profile:{userId}`, `recentchats:{userId}`.
- **Invalidation:** On **UpdateProfile**, remove `user:profile:{userId}`. On new message / mark read, invalidate `recentchats:{userId}` (and optionally unread) for affected users. Use **CancellationToken** when reading from cache if the API supports it.
- **Memory limits:** For IMemoryCache, set size limits and eviction (e.g. SizeLimit, CompactionPercentage) to avoid unbounded growth.
- **Thread safety:** IMemoryCache and IDistributedCache are thread-safe; document that cache is shared and values should be immutable or copy-on-read.

**Why:** Reduces DB load for hot profiles and lists; Redis-ready abstraction allows scaling out cache when moving to multiple app instances.

---

## 9. Pagination and cursor-based feeds

**Goal:** All list endpoints support pagination; cursor-based for feeds where applicable.

- **Offset pagination:** Keep **page, pageSize** for admin-style lists (users, calls history, saved messages). Enforce **max pageSize** (e.g. 100) to avoid huge responses.
- **Cursor-based for conversations:** For **GetConversation** and **GetGroupMessages**, use **before** (e.g. message Id or SentAt) + **limit** as a cursor. API returns **nextCursor** (e.g. last message Id or timestamp) so the client can request the next page. This avoids offset scan and keeps latency stable as data grows.
- **Consistent interface:** Document pagination contract (cursor vs offset) per endpoint so frontend and load tests align.

**Why:** Cursor-based pagination scales for feeds; offset pagination degrades with large offsets. Max page size protects the server.

---

## 10. Background processing for heavy work

**Goal:** Don’t block request pipeline for non-critical or heavy work.

- **Domain event handlers:** When **DomainEventHandler** sends SignalR or push notifications, ensure it’s async and non-blocking. If you add “write to read store” or “invalidate cache,” keep it fast or offload to a background job.
- **Background jobs:** Introduce **IBackgroundJobQueue** or use **IHostedService** with a **Channel&lt;T&gt;** for fire-and-forget tasks (e.g. push notification send, cache invalidation, analytics). Process jobs with a limited concurrency (e.g. 4 workers). Do not do heavy work in MediatR pipeline.
- **Push notifications:** If **SendPushNotificationAsync** is slow, consider queuing it and returning 202 or processing in background so the API response is not delayed.

**Why:** Keeps latency low and avoids timeouts under load; heavy or external I/O should not block the request.

---

## 11. High-load API and horizontal scaling

**Goal:** Stateless APIs; low latency; graceful degradation; ready for multiple instances.

- **Stateless:** No in-memory state that must be shared across requests (except caches and ConnectionManager/OnlineUserService, which are explicit). SignalR: use **Redis backplane** when scaling to multiple app instances so connection state is shared.
- **Graceful degradation:** Health checks (**/health**): include DB and optional cache checks. If DB is down, return 503 so load balancer can stop sending traffic. Optional: circuit breaker for external services (push, storage).
- **Rate limiting:** Add rate limiting (e.g. AspNetCoreRateLimit or YARP) per user or per IP to protect from abuse and allow fair usage.
- **Response compression:** Enable response compression for JSON APIs (e.g. Brotli) to reduce bandwidth.
- **API design:** Avoid returning huge payloads; use pagination and sparse fields if needed. Document **max pageSize** and timeouts.

**Why:** Stateless + Redis backplane allows horizontal scaling; health checks and rate limiting improve resilience and fairness.

---

## 12. Code quality and testability

**Goal:** SOLID; clean boundaries; no god classes; easy to extend and test.

- **Handlers:** Keep handlers thin: load aggregate (or call read repository), call domain method (or map), persist (write), publish events. No business rules in handlers beyond orchestration.
- **Interfaces:** All application services (notification, cache, file storage) behind interfaces in Core.Application; implementations in Infrastructure or WebAPI. This allows unit testing with mocks.
- **No god classes:** Split large handlers (e.g. GetRecentChatsQueryHandler) into smaller pieces (e.g. “build recent chats from raw result” as a dedicated service or private method with clear input/output). Same for large controllers.
- **Dependency injection:** Prefer constructor injection; avoid service locator. Register all repositories and application services in DI; use scoped for DbContext and unit of work.
- **Testing:** Unit tests for domain (entities, value objects) and for handlers (with mocked repositories and MediatR). Integration tests for critical paths (e.g. send message, get conversation) with a real or in-memory DB. Document how to run tests and what they cover.

**Why:** Clear separation and small, focused classes make it safe to change and add features without regressions; testability enforces good design.

---

## Implementation order (recommended)


| Phase | Focus                                   | Deliverables                                                                                                                 |
| ----- | --------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| 1     | Async + CancellationToken, AsNoTracking | All repos and handlers accept and pass CancellationToken; all read queries AsNoTracking.                                     |
| 2     | Mapping                                 | Introduce AutoMapper/Mapster profiles; replace manual DTO construction in handlers.                                          |
| 3     | Read models + N+1 fix                   | Read repositories with projections; GetRecentChats single-query + batch online check; Message/Group read projections.        |
| 4     | Caching + indexes                       | ICacheService; cache user profile and optional recent chats; add composite indexes from section 7.                           |
| 5     | CQRS clarity + DDD                      | Document aggregates; move RecentChatResult to read model; ensure business logic in domain only; optional value object usage. |
| 6     | Pagination + background                 | Cursor-based for conversations; max pageSize; background job queue for notifications/cache invalidation.                     |
| 7     | Resilience + scaling                    | Health checks, rate limiting, SignalR Redis backplane, connection string tuning.                                             |


---

## File and project touchpoints (summary)

- **Core.Domain:** Aggregates and value objects (PhoneNumber usage); remove or relocate RecentChatResult; no read-model types.
- **Core.Application:** CancellationToken in all handlers and interfaces; IMapper injection; remove manual DTO construction; query handlers use read repositories; new read-model DTOs and optional IReadOnly* repositories interfaces.
- **Infrastructure:** Read repositories (e.g. MessageQueryRepository, RecentChatQueryRepository) with AsNoTracking + Select/ProjectTo; write repositories add AsNoTracking for read-only methods that remain; all repository methods accept CancellationToken; UnitOfWork.SaveChangesAsync(CancellationToken); DbContext connection string and pooling.
- **WebAPI:** CancellationToken in controller actions and Hub methods; register IMapper and cache abstraction; health checks; rate limiting; optional Redis backplane for SignalR.
- **New:** Optional **ReadModel** or **Core.Application.Queries** project for read DTOs and query-only repositories; **ICacheService** and implementations; **IBackgroundJobQueue** and hosted service.

This plan addresses enterprise-grade scalability, full DDD/CQRS alignment, profile-based mapping, thread safety, correct async, DbContext and query optimization, caching, N+1 and indexing, pagination, background processing, and high-load readiness while keeping the architecture clean and extensible.