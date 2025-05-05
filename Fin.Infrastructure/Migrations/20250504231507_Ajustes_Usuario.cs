using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Ajustes_Usuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageIdentifier",
                schema: "public",
                table: "Users",
                newName: "ImagePublicUrl");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "BirthDate",
                schema: "public",
                table: "Users",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePublicUrl",
                schema: "public",
                table: "Users",
                newName: "ImageIdentifier");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "BirthDate",
                schema: "public",
                table: "Users",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
