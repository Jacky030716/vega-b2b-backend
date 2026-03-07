using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Mission;

public class UserMission : BaseEntity<int>
{
  public int UserId { get; set; }
  public int MissionId { get; set; }
  public int Progress { get; set; }
  public bool IsCompleted { get; set; }
  public DateTime? CompletedAt { get; set; }
  public DateTime? ClaimedAt { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }
  public SpecialMission Mission { get; set; }

  #endregion
}
