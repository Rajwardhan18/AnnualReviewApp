using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class CycleFinalReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalReviewDueDate",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FinalReviewReleased",
                table: "ReviewCycles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalReviewReleasedAt",
                table: "ReviewCycles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalReviewDueDate",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "FinalReviewReleased",
                table: "ReviewCycles");

            migrationBuilder.DropColumn(
                name: "FinalReviewReleasedAt",
                table: "ReviewCycles");
        }
    }
}
