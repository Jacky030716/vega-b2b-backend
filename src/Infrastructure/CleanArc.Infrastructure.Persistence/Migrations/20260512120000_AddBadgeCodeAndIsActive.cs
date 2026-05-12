using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeCodeAndIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Badges",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Badges",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE "Badges"
                SET "Code" = trim(both '_' from upper(regexp_replace("Name", '[^A-Za-z0-9]+', '_', 'g')))
                WHERE "Code" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Code",
                table: "Badges",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Badges_Code",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Badges");
        }
    }
}
