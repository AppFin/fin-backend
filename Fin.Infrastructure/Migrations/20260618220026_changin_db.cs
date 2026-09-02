using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changin_db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfInstallments",
                schema: "public",
                table: "CreditCharges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfInstallments",
                schema: "public",
                table: "CreditCharges");
        }
    }
}
