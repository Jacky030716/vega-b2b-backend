namespace CleanArc.Domain.Entities.User;

public static class StudentNotificationPreferenceDefaults
{
    public const bool InAppNotificationsEnabled = true;
    public const bool PracticeRemindersEnabled = true;
    public const bool StreakRemindersEnabled = true;
    public const bool AchievementAlertsEnabled = true;
    public const bool WeeklyReportsEnabled = true;
    public const string ReminderTimeLocal = "18:00";
    public const string QuietHoursStartLocal = "22:00";
    public const string QuietHoursEndLocal = "08:00";
    public const string NotificationTimezone = "UTC";
}
