using System;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260502120000_AddRecoveryMissions")]
    public partial class AddRecoveryMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recovery_missions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    classroom_id = table.Column<int>(type: "integer", nullable: false),
                    module_id = table.Column<int>(type: "integer", nullable: true),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    recommended_game_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    target_words_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    config_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    reward_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{\"xp\":50,\"diamonds\":2}"),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "PENDING"),
                    generated_by = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "SYSTEM"),
                    approved_by_teacher_id = table.Column<int>(type: "integer", nullable: true),
                    ai_audit_log_id = table.Column<int>(type: "integer", nullable: true),
                    available_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archive_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    linked_challenge_id = table.Column<int>(type: "integer", nullable: true),
                    weak_skill = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "MIXED"),
                    trigger_snapshot_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    reward_claimed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_missions", x => x.id);
                    table.ForeignKey(
                        name: "FK_recovery_missions_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recovery_missions_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recovery_missions_syllabus_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "syllabus_modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_recovery_missions_Users_approved_by_teacher_id",
                        column: x => x.approved_by_teacher_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_recovery_missions_ai_audit_logs_ai_audit_log_id",
                        column: x => x.ai_audit_log_id,
                        principalTable: "ai_audit_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_recovery_missions_Challenges_linked_challenge_id",
                        column: x => x.linked_challenge_id,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_recovery_missions_student_class_module_skill_status", table: "recovery_missions", columns: new[] { "student_id", "classroom_id", "module_id", "weak_skill", "status" });
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_classroom_id", table: "recovery_missions", column: "classroom_id");
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_module_id", table: "recovery_missions", column: "module_id");
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_approved_by_teacher_id", table: "recovery_missions", column: "approved_by_teacher_id");
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_ai_audit_log_id", table: "recovery_missions", column: "ai_audit_log_id");
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_linked_challenge_id", table: "recovery_missions", column: "linked_challenge_id");
            migrationBuilder.CreateIndex(name: "IX_recovery_missions_archive_at", table: "recovery_missions", column: "archive_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "recovery_missions");
        }
    }
}
