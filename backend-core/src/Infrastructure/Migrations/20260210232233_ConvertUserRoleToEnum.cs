using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserRoleToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleInt",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""RoleInt"" = CASE ""Role""
                    WHEN 'SuperAdmin' THEN 3
                    WHEN 'Admin' THEN 2
                    WHEN 'Moderator' THEN 1
                    WHEN 'User' THEN 0
                    ELSE 0
                END;
            ");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RoleInt",
                table: "Users",
                newName: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleStr",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "User");

            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""RoleStr"" = CASE ""Role""
                    WHEN 3 THEN 'SuperAdmin'
                    WHEN 2 THEN 'Admin'
                    WHEN 1 THEN 'Moderator'
                    WHEN 0 THEN 'User'
                    ELSE 'User'
                END;
            ");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RoleStr",
                table: "Users",
                newName: "Role");
        }
    }
}
