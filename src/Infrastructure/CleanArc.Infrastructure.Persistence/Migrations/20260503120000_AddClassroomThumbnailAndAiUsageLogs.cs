using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    public partial class AddClassroomThumbnailAndAiUsageLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "thumbnail_type",
                table: "Classrooms",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "Classrooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_asset_id",
                table: "Classrooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_prompt",
                table: "Classrooms",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "thumbnail_generated_at",
                table: "Classrooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_usage_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    feature_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    endpoint_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    model_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    request_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    related_entity_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_usage_logs_Users_user_id",
                        column: x => x.user_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_feature_type",
                table: "ai_usage_logs",
                column: "feature_type");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_related_entity_id",
                table: "ai_usage_logs",
                column: "related_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_related_entity_type",
                table: "ai_usage_logs",
                column: "related_entity_type");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_user_id_feature_type_created_at",
                table: "ai_usage_logs",
                columns: new[] { "user_id", "feature_type", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_logs");

            migrationBuilder.DropColumn(
                name: "thumbnail_type",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "thumbnail_asset_id",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "thumbnail_prompt",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "thumbnail_generated_at",
                table: "Classrooms");
        }
    }
}
