using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                schema: "public",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "light");

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                schema: "public",
                table: "Tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "America/Sao_Paulo",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                schema: "public",
                table: "Tenants",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "pt-BR",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                schema: "public",
                table: "Tenants",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                schema: "public",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                schema: "public",
                table: "Tenants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "America/Sao_Paulo");

            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                schema: "public",
                table: "Tenants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "pt-BR");
        }
    }
}
