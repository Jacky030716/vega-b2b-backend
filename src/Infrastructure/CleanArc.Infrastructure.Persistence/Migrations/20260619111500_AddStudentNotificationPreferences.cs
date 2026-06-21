using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations;

public partial class AddStudentNotificationPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AchievementAlertsEnabled",
            schema: "usr",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "InAppNotificationsEnabled",
            schema: "usr",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "NotificationTimezone",
            schema: "usr",
            table: "Users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "UTC");

        migrationBuilder.AddColumn<bool>(
            name: "PracticeRemindersEnabled",
            schema: "usr",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "QuietHoursEndLocal",
            schema: "usr",
            table: "Users",
            type: "character varying(5)",
            maxLength: 5,
            nullable: false,
            defaultValue: "08:00");

        migrationBuilder.AddColumn<string>(
            name: "QuietHoursStartLocal",
            schema: "usr",
            table: "Users",
            type: "character varying(5)",
            maxLength: 5,
            nullable: false,
            defaultValue: "22:00");

        migrationBuilder.AddColumn<string>(
            name: "ReminderTimeLocal",
            schema: "usr",
            table: "Users",
            type: "character varying(5)",
            maxLength: 5,
            nullable: false,
            defaultValue: "18:00");

        migrationBuilder.AddColumn<bool>(
            name: "StreakRemindersEnabled",
            schema: "usr",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "WeeklyReportsEnabled",
            schema: "usr",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AchievementAlertsEnabled", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "InAppNotificationsEnabled", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "NotificationTimezone", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "PracticeRemindersEnabled", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "QuietHoursEndLocal", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "QuietHoursStartLocal", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "ReminderTimeLocal", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "StreakRemindersEnabled", schema: "usr", table: "Users");
        migrationBuilder.DropColumn(name: "WeeklyReportsEnabled", schema: "usr", table: "Users");
    }
}
