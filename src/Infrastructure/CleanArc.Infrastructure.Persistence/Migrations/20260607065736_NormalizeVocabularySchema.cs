using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeVocabularySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vocabulary_syllable_infos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: false),
                    syllables_json = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "[]"),
                    syllable_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vocabulary_syllable_infos", x => x.id);
                    table.ForeignKey(
                        name: "FK_vocabulary_syllable_infos_vocabulary_items_vocabulary_item_~",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vocabulary_translations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vocabulary_item_id = table.Column<int>(type: "integer", nullable: false),
                    language_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    translation_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vocabulary_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_vocabulary_translations_vocabulary_items_vocabulary_item_id",
                        column: x => x.vocabulary_item_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_syllable_infos_vocabulary_item_id",
                table: "vocabulary_syllable_infos",
                column: "vocabulary_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_translations_vocabulary_item_id_language_code",
                table: "vocabulary_translations",
                columns: new[] { "vocabulary_item_id", "language_code" },
                unique: true);

            // Copy translations data (SQLite and PG compatible)
            migrationBuilder.Sql("INSERT INTO vocabulary_translations (vocabulary_item_id, language_code, translation_text, created_at) SELECT id, 'ms', bm_text, CURRENT_TIMESTAMP FROM vocabulary_items WHERE bm_text IS NOT NULL AND bm_text <> ''");
            migrationBuilder.Sql("INSERT INTO vocabulary_translations (vocabulary_item_id, language_code, translation_text, created_at) SELECT id, 'zh', zh_text, CURRENT_TIMESTAMP FROM vocabulary_items WHERE zh_text IS NOT NULL AND zh_text <> ''");
            migrationBuilder.Sql("INSERT INTO vocabulary_translations (vocabulary_item_id, language_code, translation_text, created_at) SELECT id, 'en', en_text, CURRENT_TIMESTAMP FROM vocabulary_items WHERE en_text IS NOT NULL AND en_text <> ''");

            // Copy syllable info data
            migrationBuilder.Sql("INSERT INTO vocabulary_syllable_infos (vocabulary_item_id, syllables_json, syllable_text, created_at) SELECT id, COALESCE(syllables_json, '[]'), syllable_text, CURRENT_TIMESTAMP FROM vocabulary_items WHERE syllable_text IS NOT NULL OR (syllables_json IS NOT NULL AND syllables_json <> '[]')");

            migrationBuilder.DropColumn(
                name: "bm_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "en_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "syllable_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "syllables_json",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "zh_text",
                table: "vocabulary_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vocabulary_syllable_infos");

            migrationBuilder.DropTable(
                name: "vocabulary_translations");

            migrationBuilder.AddColumn<string>(
                name: "bm_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "en_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syllable_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syllables_json",
                table: "vocabulary_items",
                type: "jsonb",
                nullable: true,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "zh_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);
        }
    }
}
