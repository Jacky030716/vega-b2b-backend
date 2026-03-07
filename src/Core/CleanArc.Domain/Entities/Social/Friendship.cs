using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Social;

public class Friendship : BaseEntity<int>
{
  public int RequesterId { get; set; }
  public int AddresseeId { get; set; }
  public string Status { get; set; } = "pending"; // pending, accepted, declined

  #region Navigation Properties

  public User.User Requester { get; set; }
  public User.User Addressee { get; set; }

  #endregion
}
