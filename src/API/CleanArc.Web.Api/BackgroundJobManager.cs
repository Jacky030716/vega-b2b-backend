using Hangfire;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Web.Api;

public class BackgroundJobManager : IBackgroundJobManager
{
    public void EnqueuePushNotification(int attemptId)
    {
        BackgroundJob.Enqueue<ISrsNotificationService>(service =>
            service.ProcessPushNotificationAttemptAsync(attemptId, CancellationToken.None));
    }
}
