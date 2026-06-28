using CleanArc.Domain.Entities.User;

namespace CleanArc.Application.Contracts.Notifications;

public static class NotificationCategories
{
    public const string PracticeReminder = "practice_reminder";
    public const string StreakNudge = "streak_nudge";
    public const string AchievementAlert = "achievement_alert";
    public const string WeeklyReport = "weekly_report";
}

public interface INotificationValidationService
{
    bool ShouldSendNotification(User user, string alertCategory, DateTime utcNow);
}
