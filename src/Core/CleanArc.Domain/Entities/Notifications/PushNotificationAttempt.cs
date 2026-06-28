using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Notifications;

public class PushNotificationAttempt : BaseEntity<int>
{
    public int UserId { get; set; }
    public string PushToken { get; set; } = string.Empty;
    public int AlertId { get; set; }
    public string Status { get; set; } = string.Empty; // DeviceTokenSelected, DeliveryFailed, Sent
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public User.User User { get; set; } = null!;
}
