using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalUuidAndShortJoinCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalUuid",
                schema: "usr",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
            migrationBuilder.Sql("UPDATE usr.\"Users\" SET \"ExternalUuid\" = gen_random_uuid() WHERE \"ExternalUuid\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "ExternalUuid",
                schema: "usr",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.Sql("UPDATE \"Classrooms\" SET \"JoinCode\" = UPPER(LEFT(TRIM(\"JoinCode\"), 4));");

            migrationBuilder.AlterColumn<string>(
                name: "JoinCode",
                table: "Classrooms",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalUuid",
                schema: "usr",
                table: "Users",
                column: "ExternalUuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalUuid",
                schema: "usr",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalUuid",
                schema: "usr",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "JoinCode",
                table: "Classrooms",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4);
        }
    }
}
