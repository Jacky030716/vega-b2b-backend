using System;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260508052000_AddInstitutionUserMemberships")]
    public partial class AddInstitutionUserMemberships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "institution_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    institution_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    access_scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Member access"),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institution_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_institution_users_Institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_institution_users_Users_user_id",
                        column: x => x.user_id,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO institution_users (institution_id, user_id, access_scope, is_primary, is_active, joined_at)
                SELECT
                    u."InstitutionId",
                    u."UserId",
                    CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'teacher'
                        ) THEN 'Teacher access'
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'student'
                        ) THEN 'Student access'
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'admin'
                        ) THEN 'Admin access'
                        ELSE 'Member access'
                    END,
                    true,
                    true,
                    CURRENT_TIMESTAMP
                FROM usr."Users" u
                WHERE u."InstitutionId" IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "Institutions" i
                      WHERE i."Id" = u."InstitutionId"
                  )
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO institution_users (institution_id, user_id, access_scope, is_primary, is_active, joined_at)
                SELECT
                    1,
                    u."UserId",
                    CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'teacher'
                        ) THEN 'Teacher access'
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'student'
                        ) THEN 'Student access'
                        WHEN EXISTS (
                            SELECT 1
                            FROM usr."UserRoles" ur
                            INNER JOIN usr."Roles" r ON r."Id" = ur."RoleId"
                            WHERE ur."UserId" = u."UserId" AND lower(r."Name") = 'admin'
                        ) THEN 'Admin access'
                        ELSE 'Member access'
                    END,
                    true,
                    true,
                    CURRENT_TIMESTAMP
                FROM usr."Users" u
                WHERE u."InstitutionId" IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "Institutions" i
                      WHERE i."Id" = 1
                  )
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                UPDATE usr."Users"
                SET "InstitutionId" = 1
                WHERE "InstitutionId" IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "Institutions" i
                      WHERE i."Id" = 1
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_institution_users_active_pair",
                table: "institution_users",
                columns: new[] { "institution_id", "user_id" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_institution_users_active_primary_user",
                table: "institution_users",
                column: "user_id",
                unique: true,
                filter: "is_primary = true AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_institution_users_institution_active",
                table: "institution_users",
                columns: new[] { "institution_id", "is_active" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "institution_users");
        }
    }
}
