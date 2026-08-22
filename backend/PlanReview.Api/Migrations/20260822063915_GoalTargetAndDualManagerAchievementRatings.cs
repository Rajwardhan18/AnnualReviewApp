using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanReview.Api.Migrations
{
    /// <inheritdoc />
    public partial class GoalTargetAndDualManagerAchievementRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ManagerRating",
                table: "Achievements",
                newName: "Manager2Rating");

            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "Goals",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Manager1Rating",
                table: "Achievements",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Manager1Rating",
                table: "Achievements");

            migrationBuilder.RenameColumn(
                name: "Manager2Rating",
                table: "Achievements",
                newName: "ManagerRating");
        }
    }
}
