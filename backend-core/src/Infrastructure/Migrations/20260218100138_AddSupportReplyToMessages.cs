using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportReplyToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var systemUserId = new Guid("B1A1A1A1-1111-1111-1111-111111111111");
            var now = DateTime.UtcNow;
            migrationBuilder.Sql($@"
                INSERT INTO ""Users"" (""Id"", ""FirstName"", ""LastName"", ""Username"", ""PhoneNumber"", ""PasswordHash"", ""Email"", ""Role"", ""IsDisabled"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('{systemUserId}', 'Support', 'System', 'SystemSupport', '0000000000', 'nologin', NULL, 0, true, '{now:O}', '{now:O}')
                ON CONFLICT (""Id"") DO NOTHING;
            ".Replace("nologin", "no-login-placeholder"));

            migrationBuilder.AddColumn<Guid>(
                name: "SupportSenderId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetReceiverUserId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SupportSenderId",
                table: "Messages",
                column: "SupportSenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TargetReceiverUserId",
                table: "Messages",
                column: "TargetReceiverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_SupportUsers_SupportSenderId",
                table: "Messages",
                column: "SupportSenderId",
                principalTable: "SupportUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_TargetReceiverUserId",
                table: "Messages",
                column: "TargetReceiverUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_SupportUsers_SupportSenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_TargetReceiverUserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SupportSenderId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TargetReceiverUserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SupportSenderId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TargetReceiverUserId",
                table: "Messages");
        }
    }
}
