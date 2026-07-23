using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddSupportUserIdToMessageReactions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MessageReactions_MessageId_UserId",
            table: "MessageReactions");

        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "MessageReactions",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "SupportUserId",
            table: "MessageReactions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MessageReactions_MessageId_UserId",
            table: "MessageReactions",
            columns: new[] { "MessageId", "UserId" },
            unique: true,
            filter: "\"UserId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_MessageReactions_MessageId_SupportUserId",
            table: "MessageReactions",
            columns: new[] { "MessageId", "SupportUserId" },
            unique: true,
            filter: "\"SupportUserId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_MessageReactions_SupportUserId",
            table: "MessageReactions",
            column: "SupportUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_MessageReactions_SupportUsers_SupportUserId",
            table: "MessageReactions",
            column: "SupportUserId",
            principalTable: "SupportUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MessageReactions_SupportUsers_SupportUserId",
            table: "MessageReactions");

        migrationBuilder.DropIndex(
            name: "IX_MessageReactions_MessageId_UserId",
            table: "MessageReactions");

        migrationBuilder.DropIndex(
            name: "IX_MessageReactions_MessageId_SupportUserId",
            table: "MessageReactions");

        migrationBuilder.DropIndex(
            name: "IX_MessageReactions_SupportUserId",
            table: "MessageReactions");

        migrationBuilder.DropColumn(
            name: "SupportUserId",
            table: "MessageReactions");

        migrationBuilder.AlterColumn<Guid>(
            name: "UserId",
            table: "MessageReactions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MessageReactions_MessageId_UserId",
            table: "MessageReactions",
            columns: new[] { "MessageId", "UserId" },
            unique: true);
    }
}
