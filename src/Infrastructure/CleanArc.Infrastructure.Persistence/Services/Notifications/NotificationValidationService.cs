using System.Globalization;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Infrastructure.Persistence.Services.Notifications;

public class NotificationValidationService : INotificationValidationService
{
    public bool ShouldSendNotification(User user, string alertCategory, DateTime utcNow)
    {
        // General toggle
        if (!user.InAppNotificationsEnabled)
            return false;

        // Specific category toggles
        bool categoryEnabled = alertCategory switch
        {
            NotificationCategories.PracticeReminder => user.PracticeRemindersEnabled,
            NotificationCategories.StreakNudge => user.StreakRemindersEnabled,
            NotificationCategories.AchievementAlert => user.AchievementAlertsEnabled,
            NotificationCategories.WeeklyReport => user.WeeklyReportsEnabled,
            _ => true
        };

        if (!categoryEnabled)
            return false;

        // Resolve timezone (fallback to defaults if null/empty)
        var timezoneId = string.IsNullOrWhiteSpace(user.NotificationTimezone)
            ? StudentNotificationPreferenceDefaults.NotificationTimezone
            : user.NotificationTimezone;

        DateTime localNow;
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timezone);
        }
        catch
        {
            localNow = utcNow;
        }

        // Daily Reminder check (only applies to spaced-repetition daily practice reminders)
        if (alertCategory == NotificationCategories.PracticeReminder)
        {
            var reminderTime = ParseTimeOrDefault(
                user.ReminderTimeLocal,
                StudentNotificationPreferenceDefaults.ReminderTimeLocal);
            if (localNow.TimeOfDay < reminderTime.ToTimeSpan())
                return false;
        }

        // Quiet hours check
        var quietHoursStart = ParseTimeOrDefault(
            user.QuietHoursStartLocal,
            StudentNotificationPreferenceDefaults.QuietHoursStartLocal);
        var quietHoursEnd = ParseTimeOrDefault(
            user.QuietHoursEndLocal,
            StudentNotificationPreferenceDefaults.QuietHoursEndLocal);

        return !IsWithinQuietHours(localNow.TimeOfDay, quietHoursStart, quietHoursEnd);
    }

    private static TimeOnly ParseTimeOrDefault(string? rawValue, string fallback)
    {
        if (TimeOnly.TryParseExact(rawValue, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return TimeOnly.ParseExact(fallback, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    private static bool IsWithinQuietHours(TimeSpan currentTime, TimeOnly start, TimeOnly end)
    {
        if (start == end)
            return false;

        var startTime = start.ToTimeSpan();
        var endTime = end.ToTimeSpan();

        return startTime < endTime
            ? currentTime >= startTime && currentTime < endTime
            : currentTime >= startTime || currentTime < endTime;
    }
}
