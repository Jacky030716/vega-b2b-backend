using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiamondTransactions_UserId",
                table: "DiamondTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_CreatedTime",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_DiamondTransactions_UserId_CreatedTime",
                table: "DiamondTransactions",
                columns: new[] { "UserId", "CreatedTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_ChallengeId",
                table: "Attempts",
                columns: new[] { "UserId", "ChallengeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_ChallengeId_IsCompleted",
                table: "Attempts",
                columns: new[] { "UserId", "ChallengeId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId_CreatedTime",
                table: "ActivityLogs",
                columns: new[] { "UserId", "CreatedTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiamondTransactions_UserId_CreatedTime",
                table: "DiamondTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId_ChallengeId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId_ChallengeId_IsCompleted",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_UserId_CreatedTime",
                table: "ActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_DiamondTransactions_UserId",
                table: "DiamondTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId",
                table: "Attempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedTime",
                table: "ActivityLogs",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");
        }
    }
}
