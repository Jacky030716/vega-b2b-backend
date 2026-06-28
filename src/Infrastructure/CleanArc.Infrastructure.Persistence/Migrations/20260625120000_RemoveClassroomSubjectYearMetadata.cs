using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    public partial class RemoveClassroomSubjectYearMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Classrooms" DROP COLUMN IF EXISTS "Subject";
                ALTER TABLE "Classrooms" DROP COLUMN IF EXISTS year_level;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Classrooms"
                  ADD COLUMN IF NOT EXISTS "Subject" character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE "Classrooms"
                  ADD COLUMN IF NOT EXISTS year_level integer NOT NULL DEFAULT 1;
                """);
        }
    }
}
