# ADR: MongoDB usage

## Decision

MongoDB is **not** used as the system of record for users, conversations, membership, messages, or error logs.

PostgreSQL remains authoritative. Redis is used for ephemeral/cache workloads. MinIO stores binaries. **Serilog → Seq** is used for application and exception logging (`SEQ_URL` / `Seq:ServerUrl`).

## Context

A Mongo container may still exist on some hosts from historical error-log experiments. That path has been removed from the WebAPI; do not reintroduce Mongo-backed `ErrorLogs`.

## Consequences

- Leave Mongo unused for messaging and logging.
- Do not block core messaging on Mongo availability.
- Operators query exceptions in the Seq UI (`Seq:UiUrl` / `SEQ_UI_URL`).
- If analytics later need flexible documents, introduce a dedicated store with an explicit consistency model — not a second copy of chat history.
