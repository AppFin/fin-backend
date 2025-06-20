using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adding_user_delete_request : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDeleteRequests",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAbortedId = table.Column<Guid>(type: "uuid", nullable: true),
                    AbortedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Aborted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeleteEffectivatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeleteRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeleteRequests_Users_UserAbortedId",
                        column: x => x.UserAbortedId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserDeleteRequests_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDeleteRequests_UserAbortedId",
                schema: "public",
                table: "UserDeleteRequests",
                column: "UserAbortedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeleteRequests_UserId",
                schema: "public",
                table: "UserDeleteRequests",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDeleteRequests",
                schema: "public");
        }
    }
}
