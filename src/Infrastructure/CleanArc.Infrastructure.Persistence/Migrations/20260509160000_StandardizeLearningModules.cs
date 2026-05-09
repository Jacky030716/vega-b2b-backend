using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260509160000_StandardizeLearningModules")]
    public partial class StandardizeLearningModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public.learning_modules') IS NULL
                       AND to_regclass('public.syllabus_modules') IS NOT NULL THEN
                        ALTER TABLE syllabus_modules RENAME TO learning_modules;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE learning_modules
                    ADD COLUMN IF NOT EXISTS module_type character varying(24) NOT NULL DEFAULT 'PREDEFINED',
                    ADD COLUMN IF NOT EXISTS created_by_teacher_id integer NULL;

                UPDATE learning_modules
                SET module_type = 'PREDEFINED'
                WHERE module_type IS NULL OR module_type = '';
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public.custom_modules') IS NOT NULL THEN
                        INSERT INTO learning_modules (
                            public_id,
                            module_code,
                            subject,
                            language,
                            year_level,
                            term,
                            week,
                            unit_number,
                            unit_title,
                            title,
                            description,
                            module_type,
                            source_type,
                            created_by_teacher_id,
                            is_active,
                            created_at,
                            updated_at
                        )
                        SELECT
                            gen_random_uuid(),
                            'CUSTOM-' || cm.classroom_id || '-' || cm.id,
                            COALESCE(NULLIF(c."Subject", ''), 'Custom'),
                            'ms',
                            cm.year_level,
                            '',
                            NULL,
                            NULL,
                            cm.name,
                            cm.name,
                            'Teacher-created learning module.',
                            'CUSTOM',
                            'teacher_created',
                            cm.created_by_teacher_id,
                            true,
                            cm.created_at,
                            cm.updated_at
                        FROM custom_modules cm
                        INNER JOIN "Classrooms" c ON c."Id" = cm.classroom_id
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM learning_modules lm
                            WHERE lm.module_code = 'CUSTOM-' || cm.classroom_id || '-' || cm.id
                        );

                        INSERT INTO classroom_modules (classroom_id, module_id, created_at, updated_at)
                        SELECT cm.classroom_id, lm.id, cm.created_at, cm.updated_at
                        FROM custom_modules cm
                        INNER JOIN learning_modules lm ON lm.module_code = 'CUSTOM-' || cm.classroom_id || '-' || cm.id
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM classroom_modules link
                            WHERE link.classroom_id = cm.classroom_id
                              AND link.module_id = lm.id
                        );

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_name = 'Challenges'
                              AND column_name = 'custom_module_id'
                        ) THEN
                            UPDATE "Challenges" ch
                            SET module_id = lm.id
                            FROM custom_modules cm
                            INNER JOIN learning_modules lm ON lm.module_code = 'CUSTOM-' || cm.classroom_id || '-' || cm.id
                            WHERE ch.custom_module_id = cm.id
                              AND ch.module_id IS NULL;
                        END IF;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Challenges" DROP CONSTRAINT IF EXISTS "FK_Challenges_custom_modules_custom_module_id";
                DROP INDEX IF EXISTS "IX_Challenges_custom_module_id_lifecycle_state";
                ALTER TABLE "Challenges" DROP COLUMN IF EXISTS custom_module_id;
                DROP TABLE IF EXISTS custom_modules;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_learning_modules_Users_created_by_teacher_id'
                    ) THEN
                        ALTER TABLE learning_modules
                        ADD CONSTRAINT "FK_learning_modules_Users_created_by_teacher_id"
                        FOREIGN KEY (created_by_teacher_id)
                        REFERENCES usr."Users" ("UserId")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_learning_modules_module_type_created_by_teacher_id"
                    ON learning_modules (module_type, created_by_teacher_id);

                CREATE INDEX IF NOT EXISTS "IX_Challenges_module_id_lifecycle_state"
                    ON "Challenges" (module_id, lifecycle_state);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Challenges_module_id_lifecycle_state";
                DROP INDEX IF EXISTS "IX_learning_modules_module_type_created_by_teacher_id";
                ALTER TABLE learning_modules DROP CONSTRAINT IF EXISTS "FK_learning_modules_Users_created_by_teacher_id";
                ALTER TABLE learning_modules DROP COLUMN IF EXISTS created_by_teacher_id;
                ALTER TABLE learning_modules DROP COLUMN IF EXISTS module_type;

                DO $$
                BEGIN
                    IF to_regclass('public.syllabus_modules') IS NULL
                       AND to_regclass('public.learning_modules') IS NOT NULL THEN
                        ALTER TABLE learning_modules RENAME TO syllabus_modules;
                    END IF;
                END $$;
                """);
        }
    }
}
