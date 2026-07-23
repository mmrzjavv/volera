---
name: Database Optimization Scalability
overview: "Analyze and optimize the PostgreSQL database: verify no unused objects remain (CallParticipants already removed by migration), organize tables into domain schemas, add indexes and data-type improvements for millions of rows and high concurrency, and apply security and scalability recommendations. All changes are safe and justified; no removal of actively used structures."
todos: []
isProject: false
---

# Database optimization and scalability plan

## Context

- **Database:** PostgreSQL (Npgsql from [appsettings](backend/src/WebAPI/appsettings.json)). Default schema is **public** (PostgreSQL has no `dbo`; the equivalent is `public`).
- **ORM:** Entity Framework Core 8; schema is defined in [ApplicationDbContext](backend/src/Infrastructure/Persistence/ApplicationDbContext.cs) and [ApplicationDbContextModelSnapshot](backend/src/Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs).
- **Current tables:** Users, Calls, Messages, Contacts, Groups, GroupMembers, SystemMessages, SystemMessageReads, PushSubscriptions, SavedMessages. All are referenced in code and actively used.

---

## 1. Analysis: unused and redundant structures

**Already removed by existing migrations**

- **CallParticipants** table was added in migration `AddCallParticipants` and **dropped** in `AddMessageAttachments` ([20260201234344_AddMessageAttachments.cs](backend/src/Infrastructure/Migrations/20260201234344_AddMessageAttachments.cs)). The current model and snapshot do **not** include CallParticipants; there is no `CallParticipant` entity in [Core.Domain](backend/src/Core.Domain). So the database should already have no CallParticipants table if all migrations were applied.
- **Calls.IsGroupCall** column was **dropped** in the same migration; **Calls.ReceiverId** was made non-nullable. No leftover column in the current model.

**Verification step (before any cleanup)**

- Run against the **actual database** (not only the codebase):
  - List all tables: `SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog','information_schema') ORDER BY 1,2;`
  - List all indexes: `SELECT indexname, tablename FROM pg_indexes WHERE schemaname = 'public' ORDER BY tablename;`
- Compare with the tables and indexes defined in ApplicationDbContext and the snapshot. **If** you find tables (e.g. `CallParticipants`) or columns (e.g. `IsGroupCall` on `Calls`) that are not in the current EF model, they are candidates for **safe removal** via a new migration that drops them. Do **not** remove anything that is still in the snapshot or used in repositories/services.

**Column usage (all current columns are used)**

- **Users:** Id, FirstName, LastName, Username, PhoneNumber, Email, Bio, PasswordHash, ProfilePicture, RefreshToken, RefreshTokenExpiryTime, Role, CreatedAt, UpdatedAt — all read or written (Role for admin check; RefreshToken* for auth).
- **Calls, Messages, Contacts, Groups, GroupMembers, SystemMessages, SystemMessageReads, PushSubscriptions, SavedMessages:** Every column in the snapshot is referenced by entities and repositories. **No column identified as unused.**

**Conclusion**

- There are **no unused tables** in the current EF model. Any legacy table (e.g. CallParticipants) that still exists in the DB would be from an inconsistent migration history and should be dropped only after confirmation. No redundant or duplicated tables in the schema design.

---

## 2. Schema organization (domain-oriented schemas)

**Goal:** Move tables out of a single schema (e.g. `public`) into multiple schemas by domain. In PostgreSQL, use **schemas** (e.g. `identity`, `messaging`, `communications`, `notifications`, `admin`); this improves clarity, access control, and future splitting.

**Proposed mapping**


| Schema             | Tables                                                  | Rationale                                |
| ------------------ | ------------------------------------------------------- | ---------------------------------------- |
| **identity**       | Users                                                   | Auth, profile, identity.                 |
| **messaging**      | Messages, Groups, GroupMembers, SavedMessages, Contacts | Chat, groups, contacts, saved messages.  |
| **communications** | Calls                                                   | Voice/video call records.                |
| **notifications**  | PushSubscriptions                                       | Push subscription storage.               |
| **admin**          | SystemMessages, SystemMessageReads                      | Admin/broadcast messages and read state. |


**Implementation (requires backend changes)**

- EF Core: configure each entity to a schema in `OnModelCreating`, e.g. `entity.ToTable("Users", "identity");`. Then add a **new migration** that:
  1. Creates schemas: `CREATE SCHEMA IF NOT EXISTS identity;` (and same for messaging, communications, notifications, admin).
  2. Moves tables: `ALTER TABLE public."Users" SET SCHEMA identity;` (and similarly for all tables).
- Connection string: ensure `search_path` includes these schemas (e.g. `search_path=identity,messaging,communications,notifications,admin,public`) or use qualified names. Default: `search_path` can be set to `public` and EF will use `schema.table` in generated SQL once configured.
- **Important:** Moving tables to new schemas is a **breaking change** for any raw SQL, backups, or tools that assume `public`. Plan a maintenance window and update all references.

**If you cannot change the backend yet**

- Document the target schema layout and add a **future migration** that only creates empty schemas and optional placeholder objects, then a follow-up migration to move tables once the app is configured for multi-schema.

---

## 3. Indexing strategy (performance for millions of rows)

**Current indexes (from snapshot and DbContext)**

- **Users:** PK(Id), Unique(Username), Unique(PhoneNumber).
- **Messages:** PK(Id), (SentAt), (SenderId, ReceiverId), (GroupId, SentAt), FK indexes on SenderId, ReceiverId, GroupId.
- **Calls:** PK(Id), FK CallerId, ReceiverId (no composite for “calls by user + time”).
- **Contacts:** FK OwnerUserId, ContactUserId.
- **Groups:** PK(Id), FK AdminId.
- **GroupMembers:** PK(Id), Unique(GroupId, UserId), FK GroupId, UserId.
- **SystemMessages:** PK(Id), FK AuthorId.
- **SystemMessageReads:** PK(Id), Unique(MessageId, UserId), FK MessageId, UserId.
- **PushSubscriptions:** PK(Id), Unique(Endpoint), FK UserId.
- **SavedMessages:** PK(Id), Unique(UserId, MessageId), FK UserId, MessageId.

**Add (for high read volume and common query patterns)**

- **Messages**
  - **Composite for unread/conversation list:** `(ReceiverId, IsRead, SentAt DESC)` — supports “unread counts” and “conversation list by last message” filtered by receiver. Optionally `(ReceiverId, IsRead)` if you only need counts.
  - **DM conversation:** Existing `(SenderId, ReceiverId)` is used; add `**(ReceiverId, SenderId, SentAt DESC)**` if you often query by receiver first (e.g. “all messages where I am receiver”) to avoid full scan. Alternatively a single composite `(Least(SenderId,ReceiverId), Greatest(SenderId,ReceiverId), SentAt)` for “conversation” key (more complex; implement only if needed).
- **Calls**
  - **Call history by user:** `(CallerId, StartTime DESC)` and `(ReceiverId, StartTime DESC)` — or one composite that supports both: two indexes as above. Supports “my call history” and “active call” style lookups.
- **SavedMessages**
  - **List by user:** `(UserId, SavedAt DESC)` — supports “saved messages” feed. If there is already a unique (UserId, MessageId), add a separate non-unique index on (UserId, SavedAt) for the ordered list.
- **Contacts**
  - **By owner:** `(OwnerUserId)` — may already be implied by FK; explicit index helps `GetContactsByUserIdAsync`.
- **SystemMessages**
  - **Active list:** `(IsActive, ExpiresAt)` or `(IsActive)` and filter ExpiresAt in query — for “active system messages” query.

**Remove or avoid**

- Do **not** add indexes that duplicate the same key (e.g. two indexes with the same columns in different order unless both orders are needed). Review after adding: no index that is never used (check with `pg_stat_user_indexes` after load testing).
- **Unused indexes:** Only drop after confirming with `pg_stat_user_indexes` that idx_scan is 0 over a representative period; not recommended blindly.

**Clustered vs non-clustered (PostgreSQL)**

- PostgreSQL has **one** clustered index per table (the primary key by default with default index type). So “clustered” is already the PK. All others are non-clustered. For very large Messages table, consider **CLUSTER** on an index that matches the main access pattern (e.g. (GroupId, SentAt) or (ReceiverId, SentAt)); run during maintenance. No schema change required.

---

## 4. Data types and constraints (efficiency and clarity)

**Sizing (avoid oversized types)**

- **Message.Content:** Already `varchar(2000)`. Keep.
- **Message.AttachmentUrl, AttachmentType:** Snapshot has `text`. Use `varchar(500)` for URL and `varchar(100)` for type if lengths are bounded — reduces storage and can help with index size if you ever index.
- **User.PasswordHash, RefreshToken:** Often long; `text` is acceptable. If you standardize length (e.g. bcrypt hash length), `varchar(255)` or similar is fine.
- **User.Role:** `text` in snapshot. Use `varchar(50)` and default `'User'` for consistency.
- **PushSubscription.Endpoint, P256dh, Auth:** `text` is used; if specs define max length, use varchar to avoid accidental huge values.
- **SystemMessage.Content, Title:** Content can stay `text`; Title has max length 255 — ensure column is `varchar(255)`.

**Constraints**

- Keep all FKs and unique constraints. Add **CHECK** constraints where useful (e.g. `Duration >= 0` on Calls, `SentAt <= COALESCE(DeletedAt, 'infinity')` on Messages) only if the app guarantees them; otherwise they can block invalid legacy data.
- **Normalization:** Current design is normalized. Optional **controlled denormalization:** e.g. store `SenderDisplayName` on Message for display in lists (reduces joins); add only if read load justifies and you maintain it on write. Not required for initial optimization.

---

## 5. Partitioning (for very large tables)

**When to consider**

- **Messages:** Once row count reaches tens of millions and queries are primarily by (GroupId or conversation) and SentAt, **range partitioning by SentAt** (e.g. monthly) can improve query and maintenance (e.g. drop old partitions). Requires application and migration design (partition key in primary key or unique constraints in PostgreSQL).
- **Calls:** Similar if call history grows very large; partition by StartTime (e.g. yearly).
- **Implementation:** Create partitioned table, create partitions, migrate data, swap; EF Core 8 supports partitioning in some scenarios. Plan as a **separate project** after baseline optimization; not a first step.

**Recommendation**

- Document partitioning as a **future step** for Messages (and optionally Calls). First deliver index and schema improvements; add partitioning when metrics show need.

---

## 6. Foreign keys and constraints (performance and integrity)

**Current behavior**

- Delete behaviors: Cascade on Group->Messages, Group->GroupMembers, Message->SavedMessage, etc.; Restrict on User references to avoid accidental user deletion. This is appropriate.
- **Performance:** FKs create indexes on the referencing side in PostgreSQL for the referenced column(s). No need to duplicate those as separate indexes unless you have a composite that includes the FK column in a different order for a specific query.
- **Locking:** Avoid long-running transactions that hold locks across many tables; keep transactions short. Use `READ COMMITTED` (default) or `REPEATABLE READ` only where needed. No change to FK definitions required for “deadlock avoidance” beyond keeping transactions small and ordered (e.g. always lock in same order: Users before Messages when updating both).

---

## 7. Scalability and high-load readiness

**Query patterns to keep index-friendly**

- **Messages:** All filters (ReceiverId, SenderId, GroupId, SentAt, IsRead) should use indexes above. Avoid `SELECT *` in hot paths; select only needed columns (EF projection).
- **Pagination:** Use **cursor-based** (e.g. `WHERE SentAt < @before ORDER BY SentAt DESC LIMIT @limit`) for feeds; avoid large OFFSET.
- **Avoid:** Patterns that cause full table scan (e.g. `WHERE LOWER(Content) LIKE '%x%'`) on large tables; use full-text search or dedicated search store if needed.
- **Connection pooling:** Use Npgsql connection pooling (default); tune pool size per app server (e.g. 20–100 per instance). Set in connection string.
- **Stateless app:** No DB-level change; ensures multiple app instances can share the same DB.

**Monitoring**

- Use **pg_stat_user_tables**, **pg_stat_user_indexes** to see scan counts and identify missing or unused indexes after load testing.
- Set **statement_timeout** and **lock_timeout** at role or session level to avoid runaway queries and long lock waits.

---

## 8. Security and access control

**Schema-level separation**

- Once tables are in schemas (identity, messaging, etc.), create **roles** (e.g. `app_readwrite`, `app_readonly`, `reporting_readonly`).
- **Least privilege:**
  - `app_readwrite`: USAGE on all schemas, SELECT/INSERT/UPDATE/DELETE on all tables in those schemas (and sequences if any).
  - `app_readonly`: USAGE on schemas, SELECT only on tables needed for read-only paths.
  - Application connection string uses `app_readwrite`; reporting or analytics use `app_readonly` with a different connection string.
- **Revoke** from `PUBLIC` on sensitive tables/schemas if desired; grant explicitly to roles.
- **No unnecessary public access:** Avoid granting to PUBLIC on internal tables; use the app role.

**Separation of read/write**

- Same database; separate **roles** (read-only vs read-write) and optionally separate **connection strings** for read-heavy services (e.g. reporting) so they never hold write locks. No schema duplication required.

---

## 9. Cleanup and migration execution order

**Safe cleanup (only if verification finds leftovers)**

1. **Verify** in the live DB: list tables and columns (e.g. `information_schema.columns`).
2. **If** `CallParticipants` table exists: create a migration that drops it (and its FK constraints if any). Run in maintenance window.
3. **If** column `Calls.IsGroupCall` exists: add migration to drop it. (Snapshot says it was dropped; if your DB was migrated from an older state, it might still exist.)
4. Do **not** drop any table or column that exists in the current ApplicationDbContextModelSnapshot and is used in code.

**Optimization migration (recommended order)**

1. **Indexes only (low risk):** Add new indexes (Messages, Calls, SavedMessages, etc.) in one migration. No table move, no column drop. Deploy and monitor.
2. **Data type / constraint (medium risk):** Add migration to alter column types (e.g. Role to varchar(50), AttachmentUrl to varchar(500)) and add CHECKs if desired. Test on copy of production data first; some changes require rewrite.
3. **Schemas (higher risk):** Create schemas, then move tables in a separate migration. Update EF configuration and connection string; deploy and test thoroughly.

---

## 10. Expected output summary


| Deliverable             | Description                                                                                                                                                                                    |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Cleanup**             | Remove only objects confirmed unused (e.g. CallParticipants table or IsGroupCall column **if** still present in DB). No removal of any table/column that is in current model and used in code. |
| **Schema organization** | Document and (optionally) implement domain schemas: identity, messaging, communications, notifications, admin. Implement via EF + migrations when ready.                                       |
| **Indexes**             | Add composite indexes for Messages (ReceiverId, IsRead, SentAt), Calls (CallerId/ReceiverId + StartTime), SavedMessages (UserId, SavedAt), and any other justified by query patterns.          |
| **Data types**          | Tighten column types (varchar lengths) where safe; add CHECK constraints only where business rules are clear and data is clean.                                                                |
| **Partitioning**        | Document as future work for Messages (and Calls); implement when scale and metrics justify.                                                                                                    |
| **Security**            | Document and apply schema/role separation and least-privilege grants; use read-only role for reporting.                                                                                        |
| **Recommendations**     | Cursor-based pagination, connection pooling, monitoring (pg_stat_*), statement_timeout, and transaction sizing for high-load readiness.                                                        |


**Why each change**

- **Remove CallParticipants/IsGroupCall only if present:** They were deprecated by a prior migration; if still in DB, they add confusion and consume space.
- **Schemas:** Clear domain boundaries and easier access control and future service splitting.
- **Indexes:** Support unread counts, conversation lists, call history, and saved-message lists without full scans at scale.
- **Data types:** Smaller, predictable types reduce storage and improve cache efficiency.
- **Security:** Least privilege and read-only roles limit blast radius and support reporting without write access.

This plan keeps the database production-grade and ready for millions of rows and high concurrency without removing any actively used structures.