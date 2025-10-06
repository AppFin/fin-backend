using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class testAjust : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialInstitution_Code_TenantId",
                schema: "public",
                table: "FinancialInstitution");

            migrationBuilder.DropIndex(
                name: "IX_FinancialInstitution_Name_TenantId",
                schema: "public",
                table: "FinancialInstitution");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "public",
                table: "FinancialInstitution");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                schema: "public",
                table: "FinancialInstitution",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "public",
                table: "FinancialInstitution",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitution_Code",
                schema: "public",
                table: "FinancialInstitution",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitution_Name",
                schema: "public",
                table: "FinancialInstitution",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialInstitution_Code",
                schema: "public",
                table: "FinancialInstitution");

            migrationBuilder.DropIndex(
                name: "IX_FinancialInstitution_Name",
                schema: "public",
                table: "FinancialInstitution");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "public",
                table: "FinancialInstitution",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "public",
                table: "FinancialInstitution",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "public",
                table: "FinancialInstitution",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitution_Code_TenantId",
                schema: "public",
                table: "FinancialInstitution",
                columns: new[] { "Code", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitution_Name_TenantId",
                schema: "public",
                table: "FinancialInstitution",
                columns: new[] { "Name", "TenantId" },
                unique: true);
        }
    }
}
