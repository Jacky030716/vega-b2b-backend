using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619093000_RenameLegacyHardcoreDraftCompletedAt")]
public class RenameLegacyHardcoreDraftCompletedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'hardcore_challenge_drafts'
                      AND column_name = 'CompletedAt'
                ) AND NOT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'hardcore_challenge_drafts'
                      AND column_name = 'completed_at'
                ) THEN
                    EXECUTE format(
                        'ALTER TABLE %I.%I RENAME COLUMN %I TO %I',
                        current_schema(),
                        'hardcore_challenge_drafts',
                        'CompletedAt',
                        'completed_at');
                END IF;
            END
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'hardcore_challenge_drafts'
                      AND column_name = 'completed_at'
                ) AND NOT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'hardcore_challenge_drafts'
                      AND column_name = 'CompletedAt'
                ) THEN
                    EXECUTE format(
                        'ALTER TABLE %I.%I RENAME COLUMN %I TO %I',
                        current_schema(),
                        'hardcore_challenge_drafts',
                        'completed_at',
                        'CompletedAt');
                END IF;
            END
            $$;
            """);
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
    }
}
