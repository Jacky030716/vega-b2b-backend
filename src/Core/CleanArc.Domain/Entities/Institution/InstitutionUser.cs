using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Institution;

public class InstitutionUser : IEntity
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public Institution Institution { get; set; }

    public int UserId { get; set; }
    public User.User User { get; set; }

    public string AccessScope { get; set; } = "Member access";
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}
