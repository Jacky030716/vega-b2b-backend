using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceShopItemStockWithTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "ShopItems");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "ShopItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "General");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "ShopItems");

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "ShopItems",
                type: "integer",
                nullable: true);
        }
    }
}
