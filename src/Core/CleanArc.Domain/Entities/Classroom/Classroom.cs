using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Classroom;

public class Classroom : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string Subject { get; set; }
  public int YearLevel { get; set; } = 1;
  public string Thumbnail { get; set; } = string.Empty;
  public string ThumbnailType { get; set; } = "DEFAULT";
  public string? ThumbnailUrl { get; set; }
  public string? ThumbnailAssetId { get; set; }
  public string? ThumbnailPrompt { get; set; }
  public DateTime? ThumbnailGeneratedAt { get; set; }
  public string JoinCode { get; set; }
  public int TeacherId { get; set; }
  public bool IsActive { get; set; } = true;
  public bool IsDeleted { get; set; } = false;
  public DateTime? DeletedAt { get; set; }
  public int? DeletedBy { get; set; }

  #region Navigation Properties

  public User.User Teacher { get; set; }
  public ICollection<ClassroomStudent> Students { get; set; } = new List<ClassroomStudent>();
  public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
  public ICollection<ClassroomSubject> Subjects { get; set; } = new List<ClassroomSubject>();
  public ICollection<ClassroomModule> Modules { get; set; } = new List<ClassroomModule>();

  #endregion
}
