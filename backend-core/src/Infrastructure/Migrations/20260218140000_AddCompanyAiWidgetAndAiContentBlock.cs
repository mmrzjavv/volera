using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAiWidgetAndAiContentBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyAiWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAiWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAiWidgets_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyAiWidgets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiContentBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyAiWidgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentSnippet = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiContentBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiContentBlocks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiContentBlocks_CompanyAiWidgets_CompanyAiWidgetId",
                        column: x => x.CompanyAiWidgetId,
                        principalTable: "CompanyAiWidgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiContentBlocks_BranchId_CreatedAt",
                table: "AiContentBlocks",
                columns: new[] { "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiContentBlocks_CompanyAiWidgetId",
                table: "AiContentBlocks",
                column: "CompanyAiWidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_AiContentBlocks_JobId",
                table: "AiContentBlocks",
                column: "JobId",
                filter: "\"JobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAiWidgets_BranchId",
                table: "CompanyAiWidgets",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAiWidgets_CompanyId",
                table: "CompanyAiWidgets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAiWidgets_TenantId",
                table: "CompanyAiWidgets",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiContentBlocks");

            migrationBuilder.DropTable(
                name: "CompanyAiWidgets");
        }
    }
}
