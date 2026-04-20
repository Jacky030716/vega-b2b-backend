using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamTokensAndBadgeDreamTokenRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DreamTokensCount",
                schema: "usr",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStickerGeneratedAtUtc",
                schema: "usr",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardDreamTokens",
                table: "Badges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DreamTokensCount",
                schema: "usr",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastStickerGeneratedAtUtc",
                schema: "usr",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RewardDreamTokens",
                table: "Badges");
        }
    }
}
