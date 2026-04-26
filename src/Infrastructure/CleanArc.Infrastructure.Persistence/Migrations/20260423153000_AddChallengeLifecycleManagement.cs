using System;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260423153000_AddChallengeLifecycleManagement")]
    public partial class AddChallengeLifecycleManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_pinned",
                table: "Challenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_activity_at",
                table: "Challenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_state",
                table: "Challenges",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<double>(
                name: "recommended_score",
                table: "Challenges",
                type: "double precision",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.Sql(
                """
                UPDATE "Challenges"
                SET lifecycle_state = CASE
                    WHEN lower(coalesce(status, '')) = 'archived' THEN 'Archived'
                    WHEN lower(coalesce(status, '')) = 'completed' THEN 'Completed'
                    WHEN assigned_at IS NULL THEN 'Draft'
                    WHEN due_at IS NOT NULL AND due_at > now() THEN 'Scheduled'
                    ELSE 'Active'
                END,
                last_activity_at = COALESCE(last_activity_at, assigned_at)
                WHERE lifecycle_state = 'Draft';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_classroom_id_lifecycle_state",
                table: "Challenges",
                columns: new[] { "ClassroomId", "lifecycle_state" });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_is_pinned",
                table: "Challenges",
                column: "is_pinned");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_lifecycle_state",
                table: "Challenges",
                column: "lifecycle_state");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_recommended_score",
                table: "Challenges",
                column: "recommended_score");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Challenges_classroom_id_lifecycle_state",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_is_pinned",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_lifecycle_state",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_recommended_score",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "is_pinned",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "last_activity_at",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "lifecycle_state",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "recommended_score",
                table: "Challenges");
        }
    }
}
