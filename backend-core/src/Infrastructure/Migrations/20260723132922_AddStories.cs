using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing DBs may already have ClientMessageId / Outbox / AI tables.
            // This migration only adds Stories (+ ReplyToStoryItemId on Messages).

            migrationBuilder.Sql("""
                ALTER TABLE "Messages"
                ADD COLUMN IF NOT EXISTS "ReplyToStoryItemId" uuid NULL;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Stories" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "ExpiresAt" timestamp with time zone NOT NULL,
                    "DeletedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Stories" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Stories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "StoryItems" (
                    "Id" uuid NOT NULL,
                    "StoryId" uuid NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "MediaType" character varying(20) NOT NULL,
                    "ObjectKey" character varying(500) NOT NULL,
                    "DurationMs" integer NOT NULL,
                    "TextOverlayJson" character varying(2000) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_StoryItems" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_StoryItems_Stories_StoryId" FOREIGN KEY ("StoryId") REFERENCES "Stories" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "StoryViews" (
                    "Id" uuid NOT NULL,
                    "StoryId" uuid NOT NULL,
                    "ViewerUserId" uuid NOT NULL,
                    "ViewedAt" timestamp with time zone NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_StoryViews" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_StoryViews_Stories_StoryId" FOREIGN KEY ("StoryId") REFERENCES "Stories" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_StoryViews_Users_ViewerUserId" FOREIGN KEY ("ViewerUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Messages_ReplyToStoryItemId" ON "Messages" ("ReplyToStoryItemId");
                CREATE INDEX IF NOT EXISTS "IX_Stories_ExpiresAt" ON "Stories" ("ExpiresAt");
                CREATE INDEX IF NOT EXISTS "IX_Stories_UserId_ExpiresAt" ON "Stories" ("UserId", "ExpiresAt");
                CREATE INDEX IF NOT EXISTS "IX_StoryItems_StoryId_SortOrder" ON "StoryItems" ("StoryId", "SortOrder");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_StoryViews_StoryId_ViewerUserId" ON "StoryViews" ("StoryId", "ViewerUserId");
                CREATE INDEX IF NOT EXISTS "IX_StoryViews_ViewerUserId" ON "StoryViews" ("ViewerUserId");
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Messages_StoryItems_ReplyToStoryItemId'
                    ) THEN
                        ALTER TABLE "Messages"
                        ADD CONSTRAINT "FK_Messages_StoryItems_ReplyToStoryItemId"
                        FOREIGN KEY ("ReplyToStoryItemId") REFERENCES "StoryItems" ("Id") ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Messages" DROP CONSTRAINT IF EXISTS "FK_Messages_StoryItems_ReplyToStoryItemId";
                DROP INDEX IF EXISTS "IX_Messages_ReplyToStoryItemId";
                DROP TABLE IF EXISTS "StoryViews";
                DROP TABLE IF EXISTS "StoryItems";
                DROP TABLE IF EXISTS "Stories";
                ALTER TABLE "Messages" DROP COLUMN IF EXISTS "ReplyToStoryItemId";
                """);
        }
    }
}
