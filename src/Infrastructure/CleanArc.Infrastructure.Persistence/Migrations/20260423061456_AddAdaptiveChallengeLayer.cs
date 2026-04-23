using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveChallengeLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "assigned_at",
                table: "Challenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "challenge_mode",
                table: "Challenges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                table: "Challenges",
                type: "jsonb",
                nullable: true,
                defaultValue: "{}");

            migrationBuilder.AddColumn<DateTime>(
                name: "due_at",
                table: "Challenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "game_template_id",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "module_id",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "Challenges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Challenges",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                defaultValue: "assigned");

            migrationBuilder.AddColumn<int>(
                name: "student_id",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "game_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    supports_adaptive_difficulty = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_challenge_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    challenge_id = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    completion_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    average_response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    total_hints_used = table.Column<int>(type: "integer", nullable: false),
                    total_retries = table.Column<int>(type: "integer", nullable: false),
                    device_info = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_challenge_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_challenge_attempts_Challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_challenge_attempts_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_skill_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    spelling_recall_score = table.Column<int>(type: "integer", nullable: false),
                    pronunciation_recall_score = table.Column<int>(type: "integer", nullable: false),
                    syllable_assembly_score = table.Column<int>(type: "integer", nullable: false),
                    visual_memory_score = table.Column<int>(type: "integer", nullable: false),
                    auditory_memory_score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_skill_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_skill_profiles_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "syllabus_modules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    year_level = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    week = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_syllabus_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vocabulary_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_id = table.Column<int>(type: "integer", nullable: false),
                    word = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_word = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    year_level = table.Column<int>(type: "integer", nullable: false),
                    syllables_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "[]"),
                    phonetic_hint = table.Column<string>(type: "text", nullable: true),
                    pronunciation_text = table.Column<string>(type: "text", nullable: true),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false),
                    meaning_text = table.Column<string>(type: "text", nullable: true),
                    example_sentence = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vocabulary_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_vocabulary_items_syllabus_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "syllabus_modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "challenge_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    challenge_id = table.Column<int>(type: "integer", nullable: false),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: true),
                    sequence_no = table.Column<int>(type: "integer", nullable: false),
                    settings_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenge_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_challenge_items_Challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_challenge_items_vocabulary_items_vocabulary_item_id",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "student_word_mastery",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: false),
                    module_id = table.Column<int>(type: "integer", nullable: true),
                    mastery_score = table.Column<int>(type: "integer", nullable: false),
                    mastery_level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    total_attempts = table.Column<int>(type: "integer", nullable: false),
                    correct_attempts = table.Column<int>(type: "integer", nullable: false),
                    first_try_correct_count = table.Column<int>(type: "integer", nullable: false),
                    average_response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    total_hints_used = table.Column<int>(type: "integer", nullable: false),
                    total_retries = table.Column<int>(type: "integer", nullable: false),
                    last_practiced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_review_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    weakness_tags_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "[]"),
                    last_game_template_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_word_mastery", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_word_mastery_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_word_mastery_game_templates_last_game_template_id",
                        column: x => x.last_game_template_id,
                        principalTable: "game_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_student_word_mastery_syllabus_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "syllabus_modules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_student_word_mastery_vocabulary_items_vocabulary_item_id",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_challenge_item_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_challenge_attempt_id = table.Column<int>(type: "integer", nullable: false),
                    challenge_item_id = table.Column<int>(type: "integer", nullable: false),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: true),
                    game_template_id = table.Column<int>(type: "integer", nullable: true),
                    presented_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    was_correct = table.Column<bool>(type: "boolean", nullable: false),
                    first_attempt_correct = table.Column<bool>(type: "boolean", nullable: false),
                    retries_count = table.Column<int>(type: "integer", nullable: false),
                    hints_used = table.Column<int>(type: "integer", nullable: false),
                    answer_text = table.Column<string>(type: "text", nullable: true),
                    expected_answer_text = table.Column<string>(type: "text", nullable: true),
                    speech_confidence = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    error_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    raw_telemetry_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_challenge_item_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_challenge_item_attempts_challenge_items_challenge_i~",
                        column: x => x.challenge_item_id,
                        principalTable: "challenge_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_challenge_item_attempts_game_templates_game_templat~",
                        column: x => x.game_template_id,
                        principalTable: "game_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_student_challenge_item_attempts_student_challenge_attempts_~",
                        column: x => x.student_challenge_attempt_id,
                        principalTable: "student_challenge_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_challenge_item_attempts_vocabulary_items_vocabulary~",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "error_pattern_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: true),
                    challenge_item_attempt_id = table.Column<int>(type: "integer", nullable: true),
                    pattern_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    observed_value = table.Column<string>(type: "text", nullable: true),
                    expected_value = table.Column<string>(type: "text", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_pattern_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_error_pattern_logs_Users_student_id",
                        column: x => x.student_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_error_pattern_logs_student_challenge_item_attempts_challeng~",
                        column: x => x.challenge_item_attempt_id,
                        principalTable: "student_challenge_item_attempts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_error_pattern_logs_vocabulary_items_vocabulary_item_id",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_game_template_id",
                table: "Challenges",
                column: "game_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_module_id",
                table: "Challenges",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_student_id",
                table: "Challenges",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_challenge_items_challenge_id_sequence_no",
                table: "challenge_items",
                columns: new[] { "challenge_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_challenge_items_vocabulary_item_id",
                table: "challenge_items",
                column: "vocabulary_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_error_pattern_logs_challenge_item_attempt_id",
                table: "error_pattern_logs",
                column: "challenge_item_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_error_pattern_logs_student_id",
                table: "error_pattern_logs",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_error_pattern_logs_vocabulary_item_id",
                table: "error_pattern_logs",
                column: "vocabulary_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_templates_code",
                table: "game_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_attempts_challenge_id_student_id_attempt_~",
                table: "student_challenge_attempts",
                columns: new[] { "challenge_id", "student_id", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_attempts_student_id",
                table: "student_challenge_attempts",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_item_attempts_challenge_item_id",
                table: "student_challenge_item_attempts",
                column: "challenge_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_item_attempts_game_template_id",
                table: "student_challenge_item_attempts",
                column: "game_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_item_attempts_student_challenge_attempt_id",
                table: "student_challenge_item_attempts",
                column: "student_challenge_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_challenge_item_attempts_vocabulary_item_id",
                table: "student_challenge_item_attempts",
                column: "vocabulary_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_skill_profiles_student_id_subject_language",
                table: "student_skill_profiles",
                columns: new[] { "student_id", "subject", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_word_mastery_last_game_template_id",
                table: "student_word_mastery",
                column: "last_game_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_word_mastery_module_id",
                table: "student_word_mastery",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_word_mastery_student_id_vocabulary_item_id",
                table: "student_word_mastery",
                columns: new[] { "student_id", "vocabulary_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_word_mastery_vocabulary_item_id",
                table: "student_word_mastery",
                column: "vocabulary_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_modules_subject_language_year_level_term_week",
                table: "syllabus_modules",
                columns: new[] { "subject", "language", "year_level", "term", "week" });

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_items_module_id_normalized_word",
                table: "vocabulary_items",
                columns: new[] { "module_id", "normalized_word" });

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_game_templates_game_template_id",
                table: "Challenges",
                column: "game_template_id",
                principalTable: "game_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_syllabus_modules_module_id",
                table: "Challenges",
                column: "module_id",
                principalTable: "syllabus_modules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_game_templates_game_template_id",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_syllabus_modules_module_id",
                table: "Challenges");

            migrationBuilder.DropTable(
                name: "error_pattern_logs");

            migrationBuilder.DropTable(
                name: "student_skill_profiles");

            migrationBuilder.DropTable(
                name: "student_word_mastery");

            migrationBuilder.DropTable(
                name: "student_challenge_item_attempts");

            migrationBuilder.DropTable(
                name: "challenge_items");

            migrationBuilder.DropTable(
                name: "game_templates");

            migrationBuilder.DropTable(
                name: "student_challenge_attempts");

            migrationBuilder.DropTable(
                name: "vocabulary_items");

            migrationBuilder.DropTable(
                name: "syllabus_modules");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_game_template_id",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_module_id",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_student_id",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "challenge_mode",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "config_json",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "due_at",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "game_template_id",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "module_id",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "student_id",
                table: "Challenges");
        }
    }
}
