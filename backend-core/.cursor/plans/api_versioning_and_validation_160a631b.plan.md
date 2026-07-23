---
name: API Versioning and Validation
overview: Enforce mandatory API versioning (backend and frontend) so no endpoint is reachable without a version, and mandatory FluentValidation for every Command and Query with validation enforced in the MediatR pipeline so handlers never run with invalid data.
todos: []
isProject: false
---

# API versioning and request validation refactor

## Scope

Only the following are in scope:

1. **Mandatory API versioning** – Backend: all API routes include a version (e.g. `v1`); unversioned routes not allowed. Frontend: all REST calls use a centralized versioned base URL.
2. **FluentValidation for all Commands and Queries** – Every MediatR request type has a validator; validation runs in the pipeline before the handler.
3. **Validation before handler** – ValidationBehavior guarantees invalid requests never reach handlers; handlers must not perform validation.

---

## 1. Backend: mandatory API versioning

**Current state**

- Controllers use `[Route("api/[controller]")]` or `[Route("api/system-messages")]`, `[Route("api/auth")]` (KeyExchange). No version segment.
- [Program.cs](backend/src/WebAPI/Program.cs) uses `app.MapControllers()` with no versioning configuration.

**Target state**

- Every HTTP API route includes the version (e.g. `api/v1/...`).
- No route pattern allows unversioned access (e.g. `api/Message` must not work; only `api/v1/Message` must work).

**Implementation**

**Option A (recommended, minimal): Route template only**

- Change every controller route from `api/[controller]` to `**api/v1/[controller]**`.
- Controllers to update:
  - [AuthController](backend/src/WebAPI/Controllers/AuthController.cs): `[Route("api/v1/[controller]")]`
  - [UserController](backend/src/WebAPI/Controllers/UserController.cs): `[Route("api/v1/[controller]")]`
  - [MessageController](backend/src/WebAPI/Controllers/MessageController.cs): `[Route("api/v1/[controller]")]`
  - [CallController](backend/src/WebAPI/Controllers/CallController.cs): `[Route("api/v1/[controller]")]`
  - [ContactController](backend/src/WebAPI/Controllers/ContactController.cs): `[Route("api/v1/[controller]")]`
  - [GroupController](backend/src/WebAPI/Controllers/GroupController.cs): `[Route("api/v1/[controller]")]`
  - [UploadController](backend/src/WebAPI/Controllers/UploadController.cs): `[Route("api/v1/[controller]")]`
  - [PushController](backend/src/WebAPI/Controllers/PushController.cs): `[Route("api/v1/[controller]")]`
  - [SystemMessageController](backend/src/WebAPI/Controllers/SystemMessageController.cs): `[Route("api/v1/system-messages")]` → `**api/v1/system-messages**` (already no [controller]; add `v1`)
  - [KeyExchangeController](backend/src/WebAPI/Controllers/KeyExchangeController.cs): `[Route("api/auth")]` → `**api/v1/auth**` (or align with AuthController if they are grouped)
  - [WeatherForecastController](backend/src/WebAPI/Controllers/WeatherForecast.cs): if kept, use `api/v1/[controller]` or remove if not needed.
- Do **not** add a parallel unversioned route. Only versioned routes are registered.
- SignalR hubs (`/callHub`, `/chatHub`) and `GET /health` are not REST “API endpoints”; they can stay as-is unless you explicitly decide to version them (e.g. `/v1/callHub`). For “every API endpoint” we interpret this as **HTTP API controllers only**.

**Option B (if you want multiple versions later)**

- Add package `Microsoft.AspNetCore.Mvc.Versioning` (and optionally `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer`).
- Configure in Program.cs or Startup: `services.AddApiVersioning(options => { options.AssumeDefaultVersionWhenUnspecified = false; options.DefaultApiVersion = new ApiVersion(1, 0); })` and require version via query, header, or route (e.g. `[Route("api/v{version:apiVersion}/[controller]")]`).
- Reject unversioned requests (e.g. 400 Bad Request) when no version is provided.

For “only these requirements,” Option A is enough: single version `v1` in the route, no unversioned API.

---

## 2. Frontend: mandatory API versioning

**Current state**

- [api.ts](backend/frontend/src/services/api.ts): `API_URL = API_BASE ?` ${API_BASE}/api `: '/api'`; axios `baseURL: API_URL`. So calls go to `/api/Message`, `/api/Auth/login`, etc.

**Target state**

- All REST calls go to a versioned path (e.g. `/api/v1/...`).
- Version is centralized (one place); no hardcoded unversioned `/api/` in call sites.

**Implementation**

- Define the versioned API base in one place, e.g.:
  - `const API_VERSION = 'v1';`
  - `const API_URL = API_BASE ?` ${API_BASE}/api/${API_VERSION}`:`/api/${API_VERSION}`;`
- Use `API_URL` as the axios `baseURL` (no change to how axios is used, only the value of `API_URL`).
- Update the **refresh-token** call in the response interceptor: it currently uses `${API_URL}/Auth/refresh-token`; with the change it becomes `${API_URL}/Auth/refresh-token` = `/api/v1/Auth/refresh-token` (correct).
- Ensure no other file constructs REST paths without using this base (grep for `/api/` or `API_URL` and fix any that bypass the versioned base).
- SignalR connection URLs (e.g. `getApiBase() + '/callHub'`) typically do not include `/api/v1`; keep them as-is unless you explicitly version hubs.

**Result**

- Every REST request goes to `/api/v1/...`; no unversioned API calls.

---

## 3. FluentValidation for every Command and Query

**Current state**

- [ValidationBehavior](backend/src/Core.Application/Behaviors/ValidationBehavior.cs) runs validators only when `_validators.Any()`; if no validator is registered for a request type, the handler runs without validation.
- Existing validators in [Validators](backend/src/Core.Application/Validators/) target **DTOs** (LoginDto, RegisterUserDto, InitiateCallDto, UpdateProfileDto, ChangePasswordDto), not MediatR requests. Controllers build **Commands** from DTOs and send Commands to MediatR, so the pipeline sees `LoginCommand`, `SendMessageCommand`, etc., and there is **no** `IValidator<LoginCommand>` (etc.) registered. So today many commands and all queries run without pipeline validation.

**Target state**

- Every **Command** and every **Query** (the type that implements `IRequest<T>` / `IRequest<TResponse>`) has a corresponding FluentValidation validator (e.g. `AbstractValidator<SendMessageCommand>`).
- Validation is automatic via the existing ValidationBehavior (which receives `IValidator<TRequest>` for the MediatR request type).
- No validation logic duplicated inside handlers.

**Implementation**

**3.1 Add validators for every Command (21 types)**

Create a validator class per command. Use meaningful rules (e.g. `NotEmpty()`, `NotNull()`, `MaximumLength()`, `Must()` for business rules). Examples:

- **SendMessageCommand:** SenderId NotEmpty; Content NotEmpty, MaxLength(2000); exactly one of ReceiverId or GroupId set (RuleFor with When/Must).
- **EditMessageCommand:** MessageId NotEmpty; UserId NotEmpty; NewContent NotEmpty, MaxLength(2000).
- **InitiateCallCommand:** CallerId NotEmpty; ReceiverId NotEmpty (already have InitiateCallValidator for InitiateCallDto – add **InitiateCallCommandValidator** for the command with same rules).
- **LoginCommand:** Username NotEmpty; Password NotEmpty.
- **RegisterUserCommand:** FirstName/LastName/Username/PhoneNumber/Password NotEmpty and length/format as needed.
- **RefreshTokenCommand:** AccessToken and RefreshToken NotEmpty.
- Similarly for: AcceptCallCommand, RejectCallCommand, EndCallCommand, CreateGroupCommand, AddMemberCommand, SaveMessageCommand, UnsaveMessageCommand, DeleteMessageCommand, MarkMessagesAsReadCommand, AddContactCommand, DeleteContactCommand, SyncContactsCommand, UpdateProfileCommand, ChangePasswordCommand, UploadProfilePictureCommand, DeleteOfflineUsersCommand.

**3.2 Add validators for every Query (12 types)**

Create a validator per query. Many can be “structural” only (e.g. required ids, non-negative page size):

- **GetMessagesQuery:** CurrentUserId NotEmpty; UserId NotEmpty; Limit > 0 and cap (e.g. <= 100).
- **GetGroupMessagesQuery:** GroupId NotEmpty; CurrentUserId NotEmpty; Limit > 0.
- **GetRecentChatsQuery:** UserId NotEmpty.
- **GetSavedMessagesQuery:** UserId NotEmpty; Page >= 1; PageSize > 0.
- **GetUsersQuery:** ExcludeUserId optional; Page >= 1; PageSize > 0.
- **GetContactsQuery:** UserId NotEmpty.
- **GetCallsByUserIdQuery:** UserId NotEmpty; Page >= 1; PageSize > 0.
- **GetUserGroupsQuery:** UserId NotEmpty.
- **GetUserByIdQuery:** UserId NotEmpty.
- **SearchUserByPhoneNumberQuery:** PhoneNumber NotEmpty.
- **GetUnreadCountsQuery:** UserId NotEmpty.
- **GetTotalMessagesCountQuery:** UserId NotEmpty.

**3.3 Register validators**

- Ensure all validator assemblies are scanned: `services.AddValidatorsFromAssemblyContaining<SendMessageCommandValidator>();` (or equivalent) so that `IValidator<TRequest>` is registered for every Command and Query. Typically one call that scans the assembly containing the validators is enough if all validators live in the same project/assembly as the commands/queries.

**3.4 DTO validators (optional)**

- Existing DTO validators (LoginDto, RegisterUserDto, etc.) can remain for controller-level use (e.g. if you add `[FluentValidation]` on action parameters) or be removed if you rely solely on command validation. The requirement is that **Commands and Queries** are validated before handlers; DTO validation is supplementary.

---

## 4. Validation before handler – enforce and make impossible to bypass

**Current state**

- ValidationBehavior runs validation only when `_validators.Any()`. If a new Command/Query is added without a validator, the handler runs with no validation.

**Target state**

- Validation **always** runs before the handler for every MediatR request.
- It is **impossible** to run a handler with invalid data: either a validator exists and runs, or the pipeline fails fast (no silent bypass).

**Implementation**

**4.1 Strict ValidationBehavior**

- Change the behavior so that **every** request is required to have at least one validator:
  - If `!_validators.Any()`, throw an `InvalidOperationException` (or a dedicated exception) with a message like: `"No validator registered for request type {RequestType}. Every Command and Query must have a FluentValidation validator."`
- This makes it a deployment/runtime error to add a new Command or Query without adding a validator, so validation cannot be bypassed.

**4.2 Handlers must not validate**

- Document that handlers must assume: “If execution reached this point, the request is already valid.”
- Remove any **input validation** from handler code (e.g. “if (request.UserId == Guid.Empty) throw …”). Business rules (e.g. “receiver must exist”, “user is not allowed to edit this message”) can stay in the handler or domain, but **data shape and required-field validation** belong only in validators and the pipeline.
- Optionally add a code review or simple grep rule: no `throw new ArgumentException` / `ValidationException` in handlers for request property checks; those belong in validators.

**Summary**

- Validation runs in the pipeline (ValidationBehavior).
- Every request type has a validator (new validators + strict behavior).
- Handlers do not perform validation; they assume valid input.

---

## 5. Implementation checklist

**Backend**

- Change all controller routes to `api/v1/...` (or `api/v{version:apiVersion}/...` if using versioning package). Ensure no unversioned API route remains.
- Add one FluentValidation validator for each of the 21 Commands and 12 Queries; register them (assembly scan).
- Update ValidationBehavior: when `!_validators.Any()`, throw; remove the “only validate if any” branch so validation is mandatory.
- Remove or relocate any validation logic from handler bodies into the corresponding validators.

**Frontend**

- Set `API_URL` (or equivalent) to include `/api/v1` in one central place; use it as the axios base URL and for the refresh-token URL.
- Confirm no REST call uses an unversioned `/api/` path.

**Result**

- All APIs are versioned in backend and frontend.
- Every Command and Query is validated automatically before its handler runs.
- No handler executes with invalid data; validation is centralized and enforced by the pipeline.

