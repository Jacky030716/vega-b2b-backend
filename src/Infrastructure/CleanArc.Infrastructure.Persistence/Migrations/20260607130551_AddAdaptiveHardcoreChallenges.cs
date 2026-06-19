using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveHardcoreChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hardcore_challenge_drafts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    game_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false),
                    target_words_json = table.Column<string>(type: "text", nullable: false),
                    content_data = table.Column<string>(type: "text", nullable: false),
                    reward_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_diamonds = table.Column<int>(type: "integer", nullable: false),
                    mascot_eligibility = table.Column<bool>(type: "boolean", nullable: false),
                    mascot_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    badge_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    expiry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    triggering_metrics_json = table.Column<string>(type: "text", nullable: false),
                    decision_reason = table.Column<string>(type: "text", nullable: false),
                    confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    linked_challenge_id = table.Column<int>(type: "integer", nullable: true),
                    linked_spelling_test_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hardcore_challenge_drafts", x => x.id);
                    table.ForeignKey(
                        name: "FK_hardcore_challenge_drafts_Challenges_linked_challenge_id",
                        column: x => x.linked_challenge_id,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_hardcore_challenge_drafts_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardcore_challenge_drafts_spelling_tests_linked_spelling_te~",
                        column: x => x.linked_spelling_test_id,
                        principalTable: "spelling_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "adaptive_agent_decisions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    triggering_metrics_json = table.Column<string>(type: "text", nullable: false),
                    decision_reason = table.Column<string>(type: "text", nullable: false),
                    confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    generated_draft_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_agent_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_adaptive_agent_decisions_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_adaptive_agent_decisions_hardcore_challenge_drafts_generate~",
                        column: x => x.generated_draft_id,
                        principalTable: "hardcore_challenge_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_agent_decisions_generated_draft_id",
                table: "adaptive_agent_decisions",
                column: "generated_draft_id");

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_agent_decisions_student_id_agent_name",
                table: "adaptive_agent_decisions",
                columns: new[] { "student_id", "agent_name" });

            migrationBuilder.CreateIndex(
                name: "IX_hardcore_challenge_drafts_linked_challenge_id",
                table: "hardcore_challenge_drafts",
                column: "linked_challenge_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardcore_challenge_drafts_linked_spelling_test_id",
                table: "hardcore_challenge_drafts",
                column: "linked_spelling_test_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardcore_challenge_drafts_student_id_status",
                table: "hardcore_challenge_drafts",
                columns: new[] { "student_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adaptive_agent_decisions");

            migrationBuilder.DropTable(
                name: "hardcore_challenge_drafts");
        }
    }
}
