using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Adding_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ways = table.Column<string>(type: "text", nullable: true),
                    TextBody = table.Column<string>(type: "text", nullable: true),
                    HtmlBody = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    StartToDelivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StopToDelivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedWays = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRememberUseSettings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ways = table.Column<string>(type: "text", nullable: true),
                    NotifyOn = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WeekDays = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRememberUseSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRememberUseSettings_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationUserDeliveries",
                schema: "public",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Delivery = table.Column<bool>(type: "boolean", nullable: false),
                    Visualized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationUserDeliveries", x => new { x.NotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_NotificationUserDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "public",
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationUserDeliveries_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationUserDeliveries_UserId",
                schema: "public",
                table: "NotificationUserDeliveries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_UserId",
                schema: "public",
                table: "UserNotificationSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRememberUseSettings_UserId",
                schema: "public",
                table: "UserRememberUseSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationUserDeliveries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserRememberUseSettings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "public");
        }
    }
}
