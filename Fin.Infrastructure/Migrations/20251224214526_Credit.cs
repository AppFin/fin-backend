using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Credit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TitlePerson_Person_PersonId",
                schema: "public",
                table: "TitlePerson");

            migrationBuilder.DropForeignKey(
                name: "FK_TitlePerson_Titles_TitleId",
                schema: "public",
                table: "TitlePerson");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TitlePerson",
                schema: "public",
                table: "TitlePerson");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Person",
                schema: "public",
                table: "Person");

            migrationBuilder.RenameTable(
                name: "TitlePerson",
                schema: "public",
                newName: "TitlePersons",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Person",
                schema: "public",
                newName: "Persons",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "IX_TitlePerson_TitleId",
                schema: "public",
                table: "TitlePersons",
                newName: "IX_TitlePersons_TitleId");

            migrationBuilder.RenameIndex(
                name: "IX_Person_Name_TenantId",
                schema: "public",
                table: "Persons",
                newName: "IX_Persons_Name_TenantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TitlePersons",
                schema: "public",
                table: "TitlePersons",
                columns: new[] { "PersonId", "TitleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Persons",
                schema: "public",
                table: "Persons",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CardBillings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTitleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardBillings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardBillings_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalSchema: "public",
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardBillings_Titles_PaymentTitleId",
                        column: x => x.PaymentTitleId,
                        principalSchema: "public",
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCharges",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCharges_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalSchema: "public",
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditChargeCategories",
                schema: "public",
                columns: table => new
                {
                    CreditChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditChargeCategories", x => new { x.CreditChargeId, x.TitleCategoryId });
                    table.ForeignKey(
                        name: "FK_CreditChargeCategories_CreditCharges_CreditChargeId",
                        column: x => x.CreditChargeId,
                        principalSchema: "public",
                        principalTable: "CreditCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditChargeCategories_TitleCategories_TitleCategoryId",
                        column: x => x.TitleCategoryId,
                        principalSchema: "public",
                        principalTable: "TitleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditChargePerson",
                schema: "public",
                columns: table => new
                {
                    CreditChargesId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeopleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditChargePerson", x => new { x.CreditChargesId, x.PeopleId });
                    table.ForeignKey(
                        name: "FK_CreditChargePerson_CreditCharges_CreditChargesId",
                        column: x => x.CreditChargesId,
                        principalSchema: "public",
                        principalTable: "CreditCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditChargePerson_Persons_PeopleId",
                        column: x => x.PeopleId,
                        principalSchema: "public",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditChargePersons",
                schema: "public",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditChargePersons", x => new { x.PersonId, x.CreditChargeId });
                    table.ForeignKey(
                        name: "FK_CreditChargePersons_CreditCharges_CreditChargeId",
                        column: x => x.CreditChargeId,
                        principalSchema: "public",
                        principalTable: "CreditCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditChargePersons_Persons_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "public",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Order = table.Column<byte>(type: "smallint", nullable: false),
                    CreditChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardBillingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installments_CardBillings_CardBillingId",
                        column: x => x.CardBillingId,
                        principalSchema: "public",
                        principalTable: "CardBillings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Installments_CreditCharges_CreditChargeId",
                        column: x => x.CreditChargeId,
                        principalSchema: "public",
                        principalTable: "CreditCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardBillings_CreditCardId",
                schema: "public",
                table: "CardBillings",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBillings_PaymentTitleId",
                schema: "public",
                table: "CardBillings",
                column: "PaymentTitleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditChargeCategories_TitleCategoryId",
                schema: "public",
                table: "CreditChargeCategories",
                column: "TitleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditChargePerson_PeopleId",
                schema: "public",
                table: "CreditChargePerson",
                column: "PeopleId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditChargePersons_CreditChargeId",
                schema: "public",
                table: "CreditChargePersons",
                column: "CreditChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCharges_CreditCardId",
                schema: "public",
                table: "CreditCharges",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_CardBillingId",
                schema: "public",
                table: "Installments",
                column: "CardBillingId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_CreditChargeId",
                schema: "public",
                table: "Installments",
                column: "CreditChargeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TitlePersons_Persons_PersonId",
                schema: "public",
                table: "TitlePersons",
                column: "PersonId",
                principalSchema: "public",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TitlePersons_Titles_TitleId",
                schema: "public",
                table: "TitlePersons",
                column: "TitleId",
                principalSchema: "public",
                principalTable: "Titles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TitlePersons_Persons_PersonId",
                schema: "public",
                table: "TitlePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_TitlePersons_Titles_TitleId",
                schema: "public",
                table: "TitlePersons");

            migrationBuilder.DropTable(
                name: "CreditChargeCategories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CreditChargePerson",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CreditChargePersons",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Installments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CardBillings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CreditCharges",
                schema: "public");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TitlePersons",
                schema: "public",
                table: "TitlePersons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Persons",
                schema: "public",
                table: "Persons");

            migrationBuilder.RenameTable(
                name: "TitlePersons",
                schema: "public",
                newName: "TitlePerson",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Persons",
                schema: "public",
                newName: "Person",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "IX_TitlePersons_TitleId",
                schema: "public",
                table: "TitlePerson",
                newName: "IX_TitlePerson_TitleId");

            migrationBuilder.RenameIndex(
                name: "IX_Persons_Name_TenantId",
                schema: "public",
                table: "Person",
                newName: "IX_Person_Name_TenantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TitlePerson",
                schema: "public",
                table: "TitlePerson",
                columns: new[] { "PersonId", "TitleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Person",
                schema: "public",
                table: "Person",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TitlePerson_Person_PersonId",
                schema: "public",
                table: "TitlePerson",
                column: "PersonId",
                principalSchema: "public",
                principalTable: "Person",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TitlePerson_Titles_TitleId",
                schema: "public",
                table: "TitlePerson",
                column: "TitleId",
                principalSchema: "public",
                principalTable: "Titles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
