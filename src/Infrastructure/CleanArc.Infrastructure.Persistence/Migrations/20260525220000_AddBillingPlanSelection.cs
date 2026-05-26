using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260525220000_AddBillingPlanSelection")]
public partial class AddBillingPlanSelection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "active_plan_id",
            table: "billing_accounts",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "standard-monthly");

        migrationBuilder.AddColumn<string>(
            name: "plan_id",
            table: "payment_transactions",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "standard-monthly");

        migrationBuilder.Sql(
            """
            UPDATE billing_accounts AS account
            SET active_plan_id = CASE
                    WHEN LOWER(institution."SubscriptionTier") = 'premium' THEN 'premium-monthly'
                    ELSE 'standard-monthly'
                END,
                plan_id = CASE
                    WHEN account.plan_id IS NULL OR account.plan_id = 'standard-demo' THEN
                        CASE WHEN LOWER(institution."SubscriptionTier") = 'premium' THEN 'premium-monthly'
                             ELSE 'standard-monthly' END
                    ELSE account.plan_id
                END
            FROM "Institutions" AS institution
            WHERE account.institution_id = institution."Id";
            """);

        migrationBuilder.AlterColumn<string>(
            name: "plan_id",
            table: "billing_accounts",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "standard-monthly",
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80,
            oldNullable: true,
            oldDefaultValue: "standard-demo");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "active_plan_id",
            table: "billing_accounts");

        migrationBuilder.DropColumn(
            name: "plan_id",
            table: "payment_transactions");

        migrationBuilder.AlterColumn<string>(
            name: "plan_id",
            table: "billing_accounts",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true,
            defaultValue: "standard-demo",
            oldClrType: typeof(string),
            oldType: "character varying(80)",
            oldMaxLength: 80,
            oldDefaultValue: "standard-monthly");
    }
}
