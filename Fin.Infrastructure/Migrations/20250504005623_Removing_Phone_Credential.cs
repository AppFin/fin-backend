using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Removing_Phone_Credential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Credentials_EncryptedPhone",
                schema: "public",
                table: "Credentials");

            migrationBuilder.DropIndex(
                name: "IX_Credentials_TelegramChatId",
                schema: "public",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "EncryptedPhone",
                schema: "public",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "PhoneCountryCode",
                schema: "public",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                schema: "public",
                table: "Credentials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPhone",
                schema: "public",
                table: "Credentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneCountryCode",
                schema: "public",
                table: "Credentials",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramChatId",
                schema: "public",
                table: "Credentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_EncryptedPhone",
                schema: "public",
                table: "Credentials",
                column: "EncryptedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_TelegramChatId",
                schema: "public",
                table: "Credentials",
                column: "TelegramChatId",
                unique: true);
        }
    }
}
