using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Person : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Person",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Inactivated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitlePerson",
                schema: "public",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitlePerson", x => new { x.PersonId, x.TitleId });
                    table.ForeignKey(
                        name: "FK_TitlePerson_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "public",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TitlePerson_Titles_TitleId",
                        column: x => x.TitleId,
                        principalSchema: "public",
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Person_Name_TenantId",
                schema: "public",
                table: "Person",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TitlePerson_TitleId",
                schema: "public",
                table: "TitlePerson",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TitlePerson",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Person",
                schema: "public");
        }
    }
}
