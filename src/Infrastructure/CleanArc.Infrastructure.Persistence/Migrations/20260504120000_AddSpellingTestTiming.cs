using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260504120000_AddSpellingTestTiming")]
    public partial class AddSpellingTestTiming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE student_spelling_test_attempts
                  ADD COLUMN IF NOT EXISTS confirmed_at timestamp with time zone NULL,
                  ADD COLUMN IF NOT EXISTS last_resumed_at timestamp with time zone NULL,
                  ADD COLUMN IF NOT EXISTS remaining_seconds integer NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
