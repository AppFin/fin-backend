using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Adding_Admin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                schema: "public",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                schema: "public",
                table: "Users");
        }
    }
}
