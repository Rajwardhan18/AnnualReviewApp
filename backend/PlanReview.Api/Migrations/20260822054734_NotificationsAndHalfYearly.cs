using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsAndHalfYearly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MidYearReflection",
                table: "Reviews",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MidYearUpdatedAt",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HalfYearlyDueDate",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HalfYearlyReleased",
                table: "ReviewCycles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HalfYearlyReleasedAt",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ReviewCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "MidYearReflection",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "MidYearUpdatedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "HalfYearlyDueDate",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "HalfYearlyReleased",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "HalfYearlyReleasedAt",
                table: "ReviewCycles");
        }
    }
}
