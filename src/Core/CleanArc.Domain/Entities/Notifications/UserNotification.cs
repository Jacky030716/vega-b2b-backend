using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Notifications;

public class UserNotification : BaseEntity<int>
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AlertType { get; set; } = "SYSTEM_B2B";
    public string PayloadJson { get; set; } = "{}";
    public string? DeduplicationKey { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public User.User User { get; set; } = null!;
}
