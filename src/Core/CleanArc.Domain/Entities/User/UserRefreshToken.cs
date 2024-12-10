using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.User;

public class UserRefreshToken:BaseEntity<Guid>
{
    public UserRefreshToken()
    {
        CreatedAt=DateTime.UtcNow;
    }

    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsValid { get; set; }
}