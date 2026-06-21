namespace CleanArc.Application.Contracts.DTOs.User;

public record StudentNotificationPreferencesDto(
    bool InAppNotificationsEnabled,
    bool PracticeRemindersEnabled,
    bool StreakRemindersEnabled,
    bool AchievementAlertsEnabled,
    bool WeeklyReportsEnabled,
    string ReminderTimeLocal,
    string QuietHoursStartLocal,
    string QuietHoursEndLocal,
    string NotificationTimezone);
