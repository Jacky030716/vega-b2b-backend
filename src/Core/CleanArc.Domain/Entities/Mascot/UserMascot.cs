using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Mascot;

public class UserMascot : BaseEntity<int>
{
  public int UserId { get; set; }
  public int MascotId { get; set; }
  public bool IsEquipped { get; set; }
  public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

  #region Navigation Properties

  public User.User User { get; set; }
  public Mascot Mascot { get; set; }

  #endregion
}
