using System;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260515170000_AddBillingPaymentDemoTables")]
    public partial class AddBillingPaymentDemoTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    institution_id = table.Column<int>(type: "integer", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    plan_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, defaultValue: "standard-demo"),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, defaultValue: "NONE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_billing_accounts_Institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    institution_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    is_demo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_Institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_billing_accounts_institution_id",
                table: "billing_accounts",
                column: "institution_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_billing_accounts_stripe_customer_id",
                table: "billing_accounts",
                column: "stripe_customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_institution_id_created_at",
                table: "payment_transactions",
                columns: new[] { "institution_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_stripe_checkout_session_id",
                table: "payment_transactions",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_stripe_payment_intent_id",
                table: "payment_transactions",
                column: "stripe_payment_intent_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_accounts");

            migrationBuilder.DropTable(
                name: "payment_transactions");
        }
    }
}
