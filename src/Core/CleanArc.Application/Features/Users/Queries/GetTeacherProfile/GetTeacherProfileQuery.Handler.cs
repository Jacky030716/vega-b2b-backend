using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetTeacherProfile;

internal sealed class GetTeacherProfileQueryHandler(
    IAppUserManager userManager,
    IUnitOfWork unitOfWork,
    IAiUsageService aiUsageService)
    : IRequestHandler<GetTeacherProfileQuery, OperationResult<TeacherProfileDto>>
{
  private const decimal SupportAccuracyThreshold = 40m;

  public async ValueTask<OperationResult<TeacherProfileDto>> Handle(
      GetTeacherProfileQuery request,
      CancellationToken cancellationToken)
  {
    var teacher = await userManager.GetUserByIdAsync(request.TeacherId);
    if (teacher is null)
      return OperationResult<TeacherProfileDto>.NotFoundResult("Teacher not found");

    var classrooms = await unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(request.TeacherId);
    var studentSnapshots = new Dictionary<int, TeacherStudentSnapshot>();

    foreach (var classroom in classrooms)
    {
      var members = await unitOfWork.ClassroomRepository.GetClassroomMembersAsync(classroom.Id);
      foreach (var member in members)
      {
        var snapshot = GetOrCreateSnapshot(studentSnapshots, member);
        snapshot.Experience = Math.Max(snapshot.Experience, member.User.Experience);
      }

      var challenges = await unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(classroom.Id);
      foreach (var challenge in challenges)
      {
        var leaderboard = await unitOfWork.ChallengeRepository.GetChallengeLeaderboardAsync(challenge.Id, classroom.Id);
        foreach (var progress in leaderboard)
        {
          if (!studentSnapshots.TryGetValue(progress.UserId, out var snapshot))
          {
            continue;
          }

          snapshot.AttemptCount += progress.AttemptCount;
          if (progress.HasCompleted)
          {
            snapshot.CompletedChallengeCount += 1;
            var estimatedTotal = challenge.MaxStars * 100;
            var fallbackAccuracy = estimatedTotal > 0
                ? (decimal)progress.BestScore / estimatedTotal * 100m
                : 0m;
            snapshot.AccuracyTotal += progress.BestAccuracy ?? Math.Min(100m, fallbackAccuracy);
            snapshot.AccuracyCount += 1;
          }
        }
      }
    }

    var activeStudents = studentSnapshots.Values.Count(student =>
        student.Experience > 0 || student.AttemptCount > 0 || student.CompletedChallengeCount > 0);
    var studentsNeedingSupport = studentSnapshots.Values.Count(student =>
        student.CompletedChallengeCount == 0 ||
        (student.AccuracyCount > 0 && student.AverageAccuracy < SupportAccuracyThreshold));
    var aiQuota = await aiUsageService.GetRemainingQuotaAsync(
        request.TeacherId,
        AiFeatureTypes.CustomChallengeGeneration,
        cancellationToken);

    TeacherSubscriptionSnapshotDto? subscription = null;
    var institutionName = "Institution not assigned";
    var accessScope = "Teacher access";
    var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
        request.TeacherId,
        cancellationToken);
    var institution = membership?.Institution;
    if (institution is not null)
    {
      institutionName = string.IsNullOrWhiteSpace(institution.Name)
          ? "Vega Institution"
          : institution.Name;
      accessScope = string.IsNullOrWhiteSpace(membership?.AccessScope)
          ? "Teacher access"
          : membership.AccessScope;
      subscription = new TeacherSubscriptionSnapshotDto(
          string.IsNullOrWhiteSpace(institution.SubscriptionTier) ? "Standard" : institution.SubscriptionTier,
          institution.SeatsUsed,
          institution.MaxSeats,
          "School Admin");
    }

    var avatarUrl = teacher.AvatarUrl;
    if (string.IsNullOrWhiteSpace(avatarUrl) &&
        int.TryParse(teacher.AvatarId, out var avatarItemId) &&
        avatarItemId > 0)
    {
      var avatarItem = await unitOfWork.ShopRepository.GetShopItemByIdAsync(avatarItemId);
      if (avatarItem is not null &&
          string.Equals(avatarItem.Category, "avatar", StringComparison.OrdinalIgnoreCase))
      {
        avatarUrl = avatarItem.ImageUrl;
      }
    }

    var fullName = string.IsNullOrWhiteSpace(teacher.Name)
        ? teacher.UserName
        : teacher.Name;
    var roles = await userManager.GetUserRolesAsync(teacher);

    var result = new TeacherProfileDto(
        fullName,
        teacher.Email ?? string.Empty,
        GetRoleLabel(roles),
        institutionName,
        accessScope,
        avatarUrl,
        "professor",
        new TeacherProfileStatsDto(
            classrooms.Count,
            activeStudents,
            studentsNeedingSupport,
            aiQuota.Remaining),
        subscription,
        new TeacherPreferencesDto(
            teacher.WeeklyAiInsightsEmail,
            teacher.InactiveStudentAlerts));

    return OperationResult<TeacherProfileDto>.SuccessResult(result);
  }

  private static string GetRoleLabel(IList<string> roles)
  {
    if (roles.Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)))
      return "Administrator";

    if (roles.Any(role => string.Equals(role, "InstitutionAdmin", StringComparison.OrdinalIgnoreCase)))
      return "Institution Admin";

    if (roles.Any(role => string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase)))
      return "Teacher";

    return "Educator";
  }

  private static TeacherStudentSnapshot GetOrCreateSnapshot(
      Dictionary<int, TeacherStudentSnapshot> snapshots,
      ClassroomStudent member)
  {
    if (!snapshots.TryGetValue(member.UserId, out var snapshot))
    {
      snapshot = new TeacherStudentSnapshot(member.UserId);
      snapshots[member.UserId] = snapshot;
    }

    return snapshot;
  }

  private sealed class TeacherStudentSnapshot(int userId)
  {
    public int UserId { get; } = userId;
    public int Experience { get; set; }
    public int AttemptCount { get; set; }
    public int CompletedChallengeCount { get; set; }
    public decimal AccuracyTotal { get; set; }
    public int AccuracyCount { get; set; }
    public decimal AverageAccuracy => AccuracyCount == 0 ? 0 : AccuracyTotal / AccuracyCount;
  }
}
