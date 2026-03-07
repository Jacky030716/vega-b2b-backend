using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Mission;

public class SpecialMission : BaseEntity<int>
{
  public string Title { get; set; }
  public string Description { get; set; }
  public string MissionType { get; set; } // daily, weekly, event
  public string RewardType { get; set; } // diamonds, xp, badge
  public int RewardAmount { get; set; }
  public int? RewardBadgeId { get; set; }
  public string RequiredAction { get; set; } // JSON — e.g. {"type":"complete_quizzes","count":3}
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public bool IsActive { get; set; } = true;

  #region Navigation Properties

  public Achievement.Badge RewardBadge { get; set; }
  public ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();

  #endregion
}
