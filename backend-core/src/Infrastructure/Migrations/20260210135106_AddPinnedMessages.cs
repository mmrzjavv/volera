using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PinnedByUserId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_GroupId_IsPinned",
                table: "Messages",
                columns: new[] { "GroupId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId_ReceiverId_IsPinned",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId", "IsPinned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_GroupId_IsPinned",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId_ReceiverId_IsPinned",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedByUserId",
                table: "Messages");
        }
    }
}
