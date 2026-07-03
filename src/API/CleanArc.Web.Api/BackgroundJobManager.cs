using Hangfire;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Achievements;

namespace CleanArc.Web.Api;

public class BackgroundJobManager : IBackgroundJobManager
{
    public void EnqueuePushNotification(int attemptId)
    {
        BackgroundJob.Enqueue<ISrsNotificationService>(service =>
            service.ProcessPushNotificationAttemptAsync(attemptId, CancellationToken.None));
    }

    public void EnqueueAchievementEvent(int userId, string eventType, string eventId, string propertiesJson)
    {
        BackgroundJob.Enqueue<IAchievementTrackingService>(service =>
            service.ExecuteTrackingJobAsync(userId, eventType, eventId, propertiesJson, CancellationToken.None));
    }

    public void EnqueueSyncStudentAchievements(int userId)
    {
        BackgroundJob.Enqueue<IAchievementTrackingService>(service =>
            service.SyncStudentAchievementsAsync(userId, CancellationToken.None));
    }
}
