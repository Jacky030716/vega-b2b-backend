using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLastSrsNotificationSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSrsNotificationSentAt",
                schema: "usr",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSrsNotificationSentAt",
                schema: "usr",
                table: "Users");
        }
    }
}
