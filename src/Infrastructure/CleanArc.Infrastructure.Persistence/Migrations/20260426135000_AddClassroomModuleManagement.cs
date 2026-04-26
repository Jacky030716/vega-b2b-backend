using System;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260426135000_AddClassroomModuleManagement")]
    public partial class AddClassroomModuleManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "year_level",
                table: "Classrooms",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "custom_module_id",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "Challenges",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "custom_modules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    classroom_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, defaultValue: "Custom Module"),
                    year_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by_teacher_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_modules", x => x.id);
                    table.ForeignKey(
                        name: "FK_custom_modules_Classrooms_classroom_id",
                        column: x => x.classroom_id,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_custom_modules_Users_created_by_teacher_id",
                        column: x => x.created_by_teacher_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_ClassroomId_module_id",
                table: "Challenges",
                columns: new[] { "ClassroomId", "module_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_custom_module_id_lifecycle_state",
                table: "Challenges",
                columns: new[] { "custom_module_id", "lifecycle_state" });

            migrationBuilder.CreateIndex(
                name: "IX_custom_modules_classroom_id",
                table: "custom_modules",
                column: "classroom_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_modules_created_by_teacher_id",
                table: "custom_modules",
                column: "created_by_teacher_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_custom_modules_custom_module_id",
                table: "Challenges",
                column: "custom_module_id",
                principalTable: "custom_modules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_custom_modules_custom_module_id",
                table: "Challenges");

            migrationBuilder.DropTable(
                name: "custom_modules");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_ClassroomId_module_id",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_custom_module_id_lifecycle_state",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "year_level",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "custom_module_id",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "Challenges");
        }
    }
}
