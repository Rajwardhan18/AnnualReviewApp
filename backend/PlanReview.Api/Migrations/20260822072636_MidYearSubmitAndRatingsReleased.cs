using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class MidYearSubmitAndRatingsReleased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MidYearSubmittedAt",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RatingsReleased",
                table: "ReviewCycles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingsReleasedAt",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MidYearSubmittedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RatingsReleased",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "RatingsReleasedAt",
                table: "ReviewCycles");
        }
    }
}
