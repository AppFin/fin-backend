using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLogoUrlToIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                schema: "public",
                table: "FinancialInstitution",
                newName: "Icon");

            // Change the max length from 500 to 50
            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                schema: "public",
                table: "FinancialInstitution",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Clear existing data since we're changing from URLs to icon names
            migrationBuilder.Sql("DELETE FROM \"public\".\"FinancialInstitution\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Change back to max length 500
            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                schema: "public",
                table: "FinancialInstitution",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Rename the column Icon back to LogoUrl
            migrationBuilder.RenameColumn(
                name: "Icon",
                schema: "public",
                table: "FinancialInstitution",
                newName: "LogoUrl");
        }
    }
}
