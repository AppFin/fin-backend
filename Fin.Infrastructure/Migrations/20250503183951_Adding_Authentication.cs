using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Adding_Authentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Locale = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Sex = table.Column<int>(type: "integer", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImageIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActivity = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EncryptedPhone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneCountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    GoogleId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TelegramChatId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResetToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credentials_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                schema: "public",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "public",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUsers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_EncryptedEmail",
                schema: "public",
                table: "Credentials",
                column: "EncryptedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_EncryptedPhone",
                schema: "public",
                table: "Credentials",
                column: "EncryptedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_GoogleId",
                schema: "public",
                table: "Credentials",
                column: "GoogleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_ResetToken",
                schema: "public",
                table: "Credentials",
                column: "ResetToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_TelegramChatId",
                schema: "public",
                table: "Credentials",
                column: "TelegramChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_UserId",
                schema: "public",
                table: "Credentials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_UserId",
                schema: "public",
                table: "TenantUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Credentials",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TenantUsers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");
        }
    }
}
