# Message state machine (client)

Volera end-user clients persist outgoing messages before network I/O.

## States

| State | Meaning |
|-------|---------|
| `draft` | Composer text not submitted |
| `queued` | Stored in IndexedDB, waiting for network/send |
| `sending` | Request in flight |
| `accepted` | Server returned message id (REST/hub ack) |
| `delivered` | Peer delivery signal (when available) |
| `read` | Peer read receipt (when available) |
| `retrying` | Backoff after failure |
| `failed` | Exhausted retries or non-retryable error |
| `cancelled` | User cancelled |

## Rules

1. Persist to IndexedDB **before** invoking API/hub.
2. Include `clientMessageId` for idempotent create.
3. Never mark success before server acknowledgement.
4. On reconnect: drain queue + call `GET /api/v1/Message/sync` for gap fill.
5. Duplicate server responses must upsert by id / clientMessageId.

## Server counterpart

- Unique `(SenderId, ClientMessageId)` when set
- Transactional outbox for `MessageSent`, `MessageEdited`, `MessageDeleted`, `MessageReactionsUpdated`
- Outbox processor fans out SignalR/push after durable commit

See also: [`resilience-international-shutdown.md`](./resilience-international-shutdown.md)
