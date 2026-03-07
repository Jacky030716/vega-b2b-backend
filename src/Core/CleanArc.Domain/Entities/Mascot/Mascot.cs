using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Mascot;

public class Mascot : BaseEntity<int>
{
  public string Name { get; set; }
  public string ImageUrl { get; set; }
  public string Description { get; set; }
  public bool IsDefault { get; set; }
  public string UnlockCondition { get; set; } // JSON — nullable

  #region Navigation Properties

  public ICollection<UserMascot> UserMascots { get; set; } = new List<UserMascot>();

  #endregion
}
