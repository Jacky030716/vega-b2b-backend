using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Change_ContentData_To_Jsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL cannot auto-cast text → jsonb; USING clause required.
            migrationBuilder.Sql(
                @"ALTER TABLE ""Challenges"" ALTER COLUMN ""ContentData"" TYPE jsonb USING ""ContentData""::jsonb;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ContentData",
                table: "Challenges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
