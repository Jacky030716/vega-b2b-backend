using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Streak;

public class DailyCheckIn : BaseEntity<int>
{
  public int UserId { get; set; }
  public DateOnly CheckInDate { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }

  #endregion
}
