using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeChallengeItemAttemptFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_challenge_items_challenge_i~",
                table: "student_challenge_item_attempts");

            migrationBuilder.AlterColumn<int>(
                name: "challenge_item_id",
                table: "student_challenge_item_attempts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_challenge_items_challenge_i~",
                table: "student_challenge_item_attempts",
                column: "challenge_item_id",
                principalTable: "challenge_items",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_challenge_items_challenge_i~",
                table: "student_challenge_item_attempts");

            migrationBuilder.AlterColumn<int>(
                name: "challenge_item_id",
                table: "student_challenge_item_attempts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_challenge_items_challenge_i~",
                table: "student_challenge_item_attempts",
                column: "challenge_item_id",
                principalTable: "challenge_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
