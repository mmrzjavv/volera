ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "ClientMessageId" uuid NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Messages_SenderId_ClientMessageId"
ON "Messages" ("SenderId", "ClientMessageId")
WHERE "ClientMessageId" IS NOT NULL;

CREATE TABLE IF NOT EXISTS "OutboxMessages" (
    "Id" uuid NOT NULL,
    "Type" character varying(100) NOT NULL,
    "Payload" text NOT NULL,
    "Status" integer NOT NULL,
    "AttemptCount" integer NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "NextAttemptAt" timestamp with time zone NULL,
    "LastError" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_CreatedAt" ON "OutboxMessages" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_Status_NextAttemptAt" ON "OutboxMessages" ("Status", "NextAttemptAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260723120000_AddMessageClientIdAndOutbox', '9.0.0'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260723120000_AddMessageClientIdAndOutbox'
);
