# MinIO / S3-compatible media storage

Volera stores user-generated binaries in S3-compatible object storage (MinIO locally, or another provider). PostgreSQL stores **metadata and object keys only**.

## Configuration

| Key | Purpose |
|-----|---------|
| `Storage:EndpointUrl` | e.g. `http://minio:9000` |
| `Storage:AccessKey` / `Storage:SecretKey` | Credentials (env only) |
| `Storage:BucketName` | Private bucket (e.g. `volera-media`) |
| `Storage:PublicEndpointUrl` | Browser-facing base for signed URLs. Use `auto` or keep loopback to enable request-host signing locally. |
| `Storage:PublicEndpointMode` | `RequestHost` signs against the incoming Host (needed for LAN phones). |

If any of these are missing, the API boots with `NullFileStorageService` so **text messaging still works**.

## Security model

- Objects are uploaded with **private** ACL.
- Clients receive **presigned GET/PUT** URLs after server authorization.
- MIME/extension allowlist and size limits are enforced in `MediaContentValidator`.
- Guessing an object key without a valid signature must not grant access (bucket private + no public ACL).

## API

- `POST /api/v1/Upload` — authenticated multipart upload → `{ url, objectKey }`
- `POST /api/v1/Upload/initiate` — presigned PUT
- `GET /api/v1/Upload/download-url?objectKey=` — authorized download URL
- Profile/message readers resolve stored keys to short-lived download URLs

## Local MinIO (Docker)

1. Keep MinIO on the same Docker network as `chat-webapi` (`chat-app-net`).
2. Set:
   - `Storage:EndpointUrl=http://minio:9000` (API container → MinIO)
   - `Storage:PublicEndpointUrl=http://localhost:9000` (browser → MinIO)
3. Create a private bucket matching `Storage:BucketName`.
4. Prefer `POST /api/v1/Upload` (server-side PutObject). Browser direct presigned PUT may need MinIO CORS depending on your MinIO version.

## Health

`GET /health/ready` reports `storageConfigured`.
