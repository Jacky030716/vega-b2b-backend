using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "word_progress",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    word_id = table.Column<int>(type: "integer", nullable: false),
                    total_attempts = table.Column<int>(type: "integer", nullable: false),
                    total_correct = table.Column<int>(type: "integer", nullable: false),
                    mastery_score = table.Column<int>(type: "integer", nullable: false),
                    last_practiced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_review_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_word_progress", x => x.id);
                    table.ForeignKey(
                        name: "FK_word_progress_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_word_progress_vocabulary_items_word_id",
                        column: x => x.word_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_word_progress_student_id_word_id",
                table: "word_progress",
                columns: new[] { "student_id", "word_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_word_progress_word_id",
                table: "word_progress",
                column: "word_id");

            migrationBuilder.Sql(@"
INSERT INTO word_progress (student_id, word_id, total_attempts, total_correct, mastery_score, last_practiced_at, next_review_date, created_at, updated_at)
SELECT student_id, vocabulary_item_id, total_attempts, correct_attempts, mastery_score, last_practiced_at, next_review_at, created_at, updated_at
FROM student_word_mastery
ON CONFLICT (student_id, word_id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "word_progress");
        }
    }
}
