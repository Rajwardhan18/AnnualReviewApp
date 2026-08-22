using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class CycleEnded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ended",
                table: "ReviewCycles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ended",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ReviewCycles");
        }
    }
}
