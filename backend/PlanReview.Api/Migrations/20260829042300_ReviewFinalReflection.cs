using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReviewFinalReflection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinalReflection",
                table: "Reviews",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalReflectionSubmittedAt",
                table: "Reviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalReflectionUpdatedAt",
                table: "Reviews",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalReflection",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FinalReflectionSubmittedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "FinalReflectionUpdatedAt",
                table: "Reviews");
        }
    }
}
