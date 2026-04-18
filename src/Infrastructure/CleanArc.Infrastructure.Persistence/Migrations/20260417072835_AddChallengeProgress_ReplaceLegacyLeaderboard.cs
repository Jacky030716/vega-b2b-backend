using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeProgress_ReplaceLegacyLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassroomQuizzes");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.AddColumn<int>(
                name: "ClassroomId",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChallengeProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    ClassroomId = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    HasCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    BestScore = table.Column<int>(type: "integer", nullable: false),
                    BestStars = table.Column<int>(type: "integer", nullable: false),
                    BestAccuracy = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    BestDurationSeconds = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    TotalXpEarned = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeProgresses_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeProgresses_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_ClassroomId",
                table: "Challenges",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgresses_ChallengeId",
                table: "ChallengeProgresses",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgresses_ClassroomId",
                table: "ChallengeProgresses",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProgresses_UserId_ChallengeId_ClassroomId",
                table: "ChallengeProgresses",
                columns: new[] { "UserId", "ChallengeId", "ClassroomId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges");

            migrationBuilder.DropTable(
                name: "ChallengeProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_ClassroomId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "Challenges");

            migrationBuilder.CreateTable(
                name: "ClassroomQuizzes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassroomId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuizId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomQuizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomQuizzes_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassroomId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Percentage = table.Column<double>(type: "double precision", nullable: false),
                    QuizId = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    TimeSpent = table.Column<int>(type: "integer", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomQuizzes_ClassroomId_QuizId",
                table: "ClassroomQuizzes",
                columns: new[] { "ClassroomId", "QuizId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ClassroomId",
                table: "LeaderboardEntries",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_QuizId_UserId_ClassroomId",
                table: "LeaderboardEntries",
                columns: new[] { "QuizId", "UserId", "ClassroomId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_UserId",
                table: "LeaderboardEntries",
                column: "UserId");
        }
    }
}
