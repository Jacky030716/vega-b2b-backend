namespace CleanArc.Application.Contracts.Notifications;

public interface IBackgroundJobManager
{
    void EnqueuePushNotification(int attemptId);
}
