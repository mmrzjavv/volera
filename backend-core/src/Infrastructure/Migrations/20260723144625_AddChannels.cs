using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MessageReactions_MessageId_SupportUserId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MessageReactions_MessageId_UserId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_AiContentBlocks_JobId"";");

            migrationBuilder.AddColumn<Guid>(
                name: "SendAsChannelId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureDisplayName",
                table: "Messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedDiscussionGroupId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicUsername",
                table: "Groups",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SignaturesEnabled",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAddAdmins",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanChangeInfo",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDeleteMessages",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEditMessages",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageSubscribers",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPost",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MessageViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageViews_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestedPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttachmentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PublishedMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestedPosts_Groups_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestedPosts_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SendAsChannelId",
                table: "Messages",
                column: "SendAsChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_SupportUserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "SupportUserId" },
                unique: true,
                filter: "\"SupportUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_UserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Kind_IsPublic",
                table: "Groups",
                columns: new[] { "Kind", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_LinkedDiscussionGroupId",
                table: "Groups",
                column: "LinkedDiscussionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PublicUsername",
                table: "Groups",
                column: "PublicUsername",
                unique: true,
                filter: "\"PublicUsername\" IS NOT NULL");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF to_regclass('public.""AiContentBlocks""') IS NOT NULL THEN
    CREATE INDEX IF NOT EXISTS ""IX_AiContentBlocks_JobId""
    ON ""AiContentBlocks"" (""JobId"")
    WHERE ""JobId"" IS NOT NULL;
  END IF;
END $$;");

            migrationBuilder.CreateIndex(
                name: "IX_MessageViews_MessageId_UserId",
                table: "MessageViews",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageViews_UserId",
                table: "MessageViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedPosts_ChannelId_Status",
                table: "SuggestedPosts",
                columns: new[] { "ChannelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedPosts_FromUserId",
                table: "SuggestedPosts",
                column: "FromUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Groups_LinkedDiscussionGroupId",
                table: "Groups",
                column: "LinkedDiscussionGroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Groups_SendAsChannelId",
                table: "Messages",
                column: "SendAsChannelId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Groups_LinkedDiscussionGroupId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Groups_SendAsChannelId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessageViews");

            migrationBuilder.DropTable(
                name: "SuggestedPosts");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SendAsChannelId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_MessageReactions_MessageId_SupportUserId",
                table: "MessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_MessageReactions_MessageId_UserId",
                table: "MessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_Groups_Kind_IsPublic",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_LinkedDiscussionGroupId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PublicUsername",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_AiContentBlocks_JobId",
                table: "AiContentBlocks");

            migrationBuilder.DropColumn(
                name: "SendAsChannelId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SignatureDisplayName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LinkedDiscussionGroupId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PublicUsername",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SignaturesEnabled",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CanAddAdmins",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CanChangeInfo",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CanDeleteMessages",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CanEditMessages",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CanManageSubscribers",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CanPost",
                table: "GroupMembers");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_SupportUserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "SupportUserId" },
                unique: true,
                filter: "[SupportUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_UserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiContentBlocks_JobId",
                table: "AiContentBlocks",
                column: "JobId",
                filter: "[JobId] IS NOT NULL");
        }
    }
}
