using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomStudentDiagnosticsQueryHandler(
    IUnitOfWork unitOfWork,
    IAppUserManager userManager)
    : IRequestHandler<GetClassroomStudentDiagnosticsQuery, OperationResult<ClassroomStudentDiagnosticsDto>>
{
  public async ValueTask<OperationResult<ClassroomStudentDiagnosticsDto>> Handle(
      GetClassroomStudentDiagnosticsQuery request,
      CancellationToken cancellationToken)
  {
    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom is null)
      return OperationResult<ClassroomStudentDiagnosticsDto>.NotFoundResult("Classroom not found");

    if (classroom.TeacherId != request.RequestingTeacherId)
      return OperationResult<ClassroomStudentDiagnosticsDto>.UnauthorizedResult("You do not manage this classroom");

    var members = await unitOfWork.ClassroomRepository.GetClassroomMembersAsync(request.ClassroomId);
    var studentMembership = members.FirstOrDefault(member => member.UserId == request.StudentId);
    if (studentMembership is null)
      return OperationResult<ClassroomStudentDiagnosticsDto>.NotFoundResult("Student not found in this classroom");

    var student = await userManager.GetUserByIdAsync(request.StudentId);
    if (student is null)
      return OperationResult<ClassroomStudentDiagnosticsDto>.NotFoundResult("Student not found");

    var userProgress = await unitOfWork.ProgressionRepository.GetUserProgressAsync(request.StudentId);
    var userBadges = await unitOfWork.BadgeRepository.GetUserBadgesAsync(request.StudentId);
    var recentActivities = await unitOfWork.ActivityLogRepository.GetRecentActivityAsync(request.StudentId, 5);
    var classroomQuizzes = await unitOfWork.ClassroomRepository.GetClassroomQuizzesAsync(request.ClassroomId);

    var recentClassroomPerformances = new List<ClassroomPerformanceItemDto>();
    foreach (var classroomQuiz in classroomQuizzes.Take(5))
    {
      var leaderboard = await unitOfWork.ClassroomRepository.GetLeaderboardAsync(
          classroomQuiz.QuizId,
          request.ClassroomId);
      var studentRow = leaderboard.FirstOrDefault(entry => entry.UserId == request.StudentId);
      if (studentRow is null)
        continue;

      recentClassroomPerformances.Add(new ClassroomPerformanceItemDto(
          classroomQuiz.QuizId,
          studentRow.CompletedAt,
          studentRow.Percentage,
          studentRow.Score,
          studentRow.TotalPoints,
          studentRow.TimeSpent));
    }

    var latestPerformance = recentClassroomPerformances
        .OrderByDescending(item => item.CompletedAt)
        .FirstOrDefault();

    var averageScore = recentClassroomPerformances.Count > 0
        ? recentClassroomPerformances.Average(item => item.ScorePercentage)
        : 0;
    var averageTimeSpent = recentClassroomPerformances.Count > 0
        ? recentClassroomPerformances.Average(item => item.TimeSpent)
        : 0;
    var quizzesTaken = Math.Max(
        userProgress?.TotalQuizzesTaken ?? 0,
        recentClassroomPerformances.Count);
    var level = Math.Max(userProgress?.CurrentLevel ?? student.Level, student.Level);

    var accuracyValue = quizzesTaken > 0 && userProgress is not null
        ? Math.Min(100, Math.Round((double)userProgress.TotalCorrectAnswers / quizzesTaken))
        : Math.Round(averageScore);
    var consistencyValue = recentClassroomPerformances.Count > 1
        ? Math.Min(
            100,
            Math.Max(
                0,
                100 - recentClassroomPerformances.Max(item => item.ScorePercentage) +
                recentClassroomPerformances.Min(item => item.ScorePercentage)))
        : Math.Round(averageScore);
    var speedValue = averageTimeSpent > 0
        ? Math.Max(20, Math.Min(100, Math.Round(100 - (averageTimeSpent / 6.0))))
        : 50;
    var masteryValue = latestPerformance is not null
        ? Math.Round(latestPerformance.ScorePercentage)
        : Math.Round(averageScore);

    var statusTone = masteryValue >= 80
        ? "emerald"
        : masteryValue >= 60
            ? "amber"
            : "rose";
    var overallStatus = masteryValue >= 80
        ? "Thriving"
        : masteryValue >= 60
            ? "Steady"
            : "Needs Support";

    var avatarSource = string.IsNullOrWhiteSpace(student.AvatarUrl)
        ? student.AvatarId
        : student.AvatarUrl;

    var result = new ClassroomStudentDiagnosticsDto(
        student.Id,
        string.IsNullOrWhiteSpace(student.Name) ? student.UserName ?? "Student" : student.Name,
        student.UserName ?? string.Empty,
        classroom.Name,
        avatarSource,
        student.Diamonds,
        student.Experience,
        level,
        latestPerformance?.ScorePercentage ?? 0,
        latestPerformance?.Score ?? 0,
        latestPerformance?.TotalPoints ?? 0,
        averageScore,
        quizzesTaken,
        overallStatus,
        statusTone,
        new List<RadarAxisDto>
        {
          new("Accuracy", accuracyValue),
          new("Mastery", masteryValue),
          new("Consistency", consistencyValue),
          new("Speed", speedValue),
        },
        recentActivities.Select(activity => new StudentActivityItemDto(
            activity.Id,
            activity.Type,
            activity.Title,
            activity.Description,
            activity.CreatedTime)).ToList(),
        userBadges
            .OrderByDescending(badge => badge.EarnedAt)
            .Take(4)
            .Select(badge => new StudentAchievementItemDto(
                badge.BadgeId,
                badge.Badge.Name,
                badge.Badge.Category,
                badge.EarnedAt))
            .ToList(),
        recentClassroomPerformances
            .OrderByDescending(item => item.CompletedAt)
            .ToList());

    return OperationResult<ClassroomStudentDiagnosticsDto>.SuccessResult(result);
  }
}
