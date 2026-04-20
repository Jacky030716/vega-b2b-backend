using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStickerInventoryAndGiftTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StickerInventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatorUserId = table.Column<int>(type: "integer", nullable: false),
                    SourceStickerId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OwnershipSource = table.Column<int>(type: "integer", nullable: false),
                    PromptChoicesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    GenerationModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickerInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StickerInventoryItems_StickerInventoryItems_SourceStickerId",
                        column: x => x.SourceStickerId,
                        principalTable: "StickerInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StickerInventoryItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StickerInventoryItems_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StickerGiftTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    RecipientUserId = table.Column<int>(type: "integer", nullable: false),
                    SourceStickerId = table.Column<int>(type: "integer", nullable: false),
                    RecipientStickerId = table.Column<int>(type: "integer", nullable: false),
                    DiamondCost = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickerGiftTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StickerGiftTransactions_StickerInventoryItems_RecipientStic~",
                        column: x => x.RecipientStickerId,
                        principalTable: "StickerInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StickerGiftTransactions_StickerInventoryItems_SourceSticker~",
                        column: x => x.SourceStickerId,
                        principalTable: "StickerInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StickerGiftTransactions_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StickerGiftTransactions_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalSchema: "usr",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StickerGiftTransactions_RecipientStickerId",
                table: "StickerGiftTransactions",
                column: "RecipientStickerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StickerGiftTransactions_RecipientUserId",
                table: "StickerGiftTransactions",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StickerGiftTransactions_RecipientUserId_Status",
                table: "StickerGiftTransactions",
                columns: new[] { "RecipientUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StickerGiftTransactions_SenderUserId",
                table: "StickerGiftTransactions",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StickerGiftTransactions_SourceStickerId",
                table: "StickerGiftTransactions",
                column: "SourceStickerId");

            migrationBuilder.CreateIndex(
                name: "IX_StickerInventoryItems_CreatorUserId",
                table: "StickerInventoryItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StickerInventoryItems_OwnerUserId",
                table: "StickerInventoryItems",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StickerInventoryItems_SourceStickerId",
                table: "StickerInventoryItems",
                column: "SourceStickerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StickerGiftTransactions");

            migrationBuilder.DropTable(
                name: "StickerInventoryItems");
        }
    }
}
