using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyllabusIngestionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "syllabus_modules",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "module_code",
                table: "syllabus_modules",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unit_number",
                table: "syllabus_modules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit_title",
                table: "syllabus_modules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE syllabus_modules
                SET module_code = 'LEGACY-' || id
                WHERE module_code IS NULL OR btrim(module_code) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "module_code",
                table: "syllabus_modules",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "vocabulary_items",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "bm_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zh_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

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
                name: "item_type",
                table: "vocabulary_items",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "WORD");

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "vocabulary_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE vocabulary_items
                SET bm_text = word
                WHERE bm_text IS NULL OR btrim(bm_text) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "bm_text",
                table: "vocabulary_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_vocabulary_items_module_id_normalized_word",
                table: "vocabulary_items");

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_modules_module_code",
                table: "syllabus_modules",
                column: "module_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_syllabus_modules_public_id",
                table: "syllabus_modules",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_items_module_id_display_order",
                table: "vocabulary_items",
                columns: new[] { "module_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_items_module_id_normalized_word",
                table: "vocabulary_items",
                columns: new[] { "module_id", "normalized_word" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_items_public_id",
                table: "vocabulary_items",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_syllabus_modules_module_code",
                table: "syllabus_modules");

            migrationBuilder.DropIndex(
                name: "IX_syllabus_modules_public_id",
                table: "syllabus_modules");

            migrationBuilder.DropIndex(
                name: "IX_vocabulary_items_module_id_display_order",
                table: "vocabulary_items");

            migrationBuilder.DropIndex(
                name: "IX_vocabulary_items_module_id_normalized_word",
                table: "vocabulary_items");

            migrationBuilder.DropIndex(
                name: "IX_vocabulary_items_public_id",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "module_code",
                table: "syllabus_modules");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "syllabus_modules");

            migrationBuilder.DropColumn(
                name: "unit_number",
                table: "syllabus_modules");

            migrationBuilder.DropColumn(
                name: "unit_title",
                table: "syllabus_modules");

            migrationBuilder.DropColumn(
                name: "bm_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "display_order",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "en_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "item_type",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "syllable_text",
                table: "vocabulary_items");

            migrationBuilder.DropColumn(
                name: "zh_text",
                table: "vocabulary_items");

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_items_module_id_normalized_word",
                table: "vocabulary_items",
                columns: new[] { "module_id", "normalized_word" });
        }
    }
}
