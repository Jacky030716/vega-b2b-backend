using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                schema: "usr",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    MaxSeats = table.Column<int>(type: "integer", nullable: false),
                    SeatsUsed = table.Column<int>(type: "integer", nullable: false),
                    RenewalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionTier = table.Column<string>(type: "text", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_InstitutionId",
                schema: "usr",
                table: "Users",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Institutions_InstitutionId",
                schema: "usr",
                table: "Users",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Institutions_InstitutionId",
                schema: "usr",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Users_InstitutionId",
                schema: "usr",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                schema: "usr",
                table: "Users");
        }
    }
}
