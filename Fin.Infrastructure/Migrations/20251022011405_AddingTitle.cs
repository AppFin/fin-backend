using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Titles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Titles_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "public",
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TitleTitleCategories",
                schema: "public",
                columns: table => new
                {
                    TitleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleTitleCategories", x => new { x.TitleCategoryId, x.TitleId });
                    table.ForeignKey(
                        name: "FK_TitleTitleCategories_TitleCategories_TitleCategoryId",
                        column: x => x.TitleCategoryId,
                        principalSchema: "public",
                        principalTable: "TitleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TitleTitleCategories_Titles_TitleId",
                        column: x => x.TitleId,
                        principalSchema: "public",
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Titles_WalletId",
                schema: "public",
                table: "Titles",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTitleCategories_TitleId",
                schema: "public",
                table: "TitleTitleCategories",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TitleTitleCategories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Titles",
                schema: "public");
        }
    }
}
