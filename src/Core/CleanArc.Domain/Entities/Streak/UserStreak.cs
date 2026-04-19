using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Streak;

public class UserStreak : BaseEntity<int>
{
  public int UserId { get; set; }
  public int CurrentStreak { get; set; }
  public int BestStreak { get; set; }
  public DateOnly? LastCheckInDate { get; set; }
  public DateOnly? LastMysteryRewardClaimedDate { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }

  #endregion
}
