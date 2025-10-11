using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adding_creditcard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCards",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Icon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Limit = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    Inactivated = table.Column<bool>(type: "boolean", nullable: false),
                    DebitWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardBrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialInstitutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCards_CardBrands_CardBrandId",
                        column: x => x.CardBrandId,
                        principalSchema: "public",
                        principalTable: "CardBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCards_FinancialInstitution_FinancialInstitutionId",
                        column: x => x.FinancialInstitutionId,
                        principalSchema: "public",
                        principalTable: "FinancialInstitution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditCards_Wallets_DebitWalletId",
                        column: x => x.DebitWalletId,
                        principalSchema: "public",
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_CardBrandId",
                schema: "public",
                table: "CreditCards",
                column: "CardBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_DebitWalletId",
                schema: "public",
                table: "CreditCards",
                column: "DebitWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_FinancialInstitutionId",
                schema: "public",
                table: "CreditCards",
                column: "FinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_Name_TenantId",
                schema: "public",
                table: "CreditCards",
                columns: new[] { "Name", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCards",
                schema: "public");
        }
    }
}
