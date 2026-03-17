using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillShopThemeFromDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""ShopItems""
                SET ""Theme"" = CASE
                    WHEN COALESCE(NULLIF(btrim(""Description""), ''), '') IN ('Demon Slayer', 'Jujutsu Kaisen', 'Classic', 'Self')
                        THEN btrim(""Description"")
                    ELSE 'General'
                END
                WHERE ""Theme"" IS NULL OR btrim(""Theme"") = '' OR ""Theme"" = 'General';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""ShopItems""
                SET ""Theme"" = 'General';
            ");
        }
    }
}
