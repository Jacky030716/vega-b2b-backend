using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Application.Features.Users.StudentNotificationPreferences;

internal static class StudentNotificationPreferencesMapper
{
    public static StudentNotificationPreferencesDto FromUser(User user) => new(
        user.InAppNotificationsEnabled,
        user.PracticeRemindersEnabled,
        user.StreakRemindersEnabled,
        user.AchievementAlertsEnabled,
        user.WeeklyReportsEnabled,
        string.IsNullOrWhiteSpace(user.ReminderTimeLocal)
            ? StudentNotificationPreferenceDefaults.ReminderTimeLocal
            : user.ReminderTimeLocal,
        string.IsNullOrWhiteSpace(user.QuietHoursStartLocal)
            ? StudentNotificationPreferenceDefaults.QuietHoursStartLocal
            : user.QuietHoursStartLocal,
        string.IsNullOrWhiteSpace(user.QuietHoursEndLocal)
            ? StudentNotificationPreferenceDefaults.QuietHoursEndLocal
            : user.QuietHoursEndLocal,
        string.IsNullOrWhiteSpace(user.NotificationTimezone)
            ? StudentNotificationPreferenceDefaults.NotificationTimezone
            : user.NotificationTimezone);
}
