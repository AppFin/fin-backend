using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adjusting_deleteRequest_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserDeleteRequests_UserId",
                schema: "public",
                table: "UserDeleteRequests");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeleteRequests_UserId",
                schema: "public",
                table: "UserDeleteRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserDeleteRequests_UserId",
                schema: "public",
                table: "UserDeleteRequests");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeleteRequests_UserId",
                schema: "public",
                table: "UserDeleteRequests",
                column: "UserId",
                unique: true);
        }
    }
}
