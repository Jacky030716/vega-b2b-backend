using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;
using Microsoft.Extensions.Caching.Memory;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomStudentDiagnosticsQueryHandler(
    IUnitOfWork unitOfWork,
    IAppUserManager userManager,
    IMemoryCache cache)
    : IRequestHandler<GetClassroomStudentDiagnosticsQuery, OperationResult<ClassroomStudentDiagnosticsDto>>
{
  // Cache key — invalidated on every new challenge attempt completion for this student+classroom.
  internal static string CacheKey(int studentId, int classroomId)
      => $"diagnostics:{studentId}:{classroomId}";

  private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

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

    var userBadges = await unitOfWork.BadgeRepository.GetUserBadgesAsync(request.StudentId);
    var recentActivities = await unitOfWork.ActivityLogRepository.GetRecentActivityAsync(request.StudentId, 5);

    // ── Radar metrics: served from cache, recalculated only on new attempts ────
    var cacheKey = CacheKey(request.StudentId, request.ClassroomId);
    if (!cache.TryGetValue(cacheKey, out RadarMetrics? metrics))
    {
      metrics = await BuildMetricsAsync(request.StudentId, request.ClassroomId);
      cache.Set(cacheKey, metrics, CacheTtl);
    }

    var level = Math.Max(metrics!.UserProgress?.CurrentLevel ?? student.Level, student.Level);
    var quizzesTaken = Math.Max(metrics.UserProgress?.TotalQuizzesTaken ?? 0, metrics.CompletedCount);

    var avatarSource = string.IsNullOrWhiteSpace(student.AvatarUrl)
        ? student.AvatarId
        : student.AvatarUrl;

    var statusTone = metrics.MasteryValue >= 80 ? "emerald"
        : metrics.MasteryValue >= 60 ? "amber"
        : "rose";
    var overallStatus = metrics.MasteryValue >= 80 ? "Thriving"
        : metrics.MasteryValue >= 60 ? "Steady"
        : "Needs Support";

    // Last active = most recent challenge attempt, or most recent activity log, whichever is later.
    var lastAttemptAt = metrics.RecentPerformances.Count > 0
        ? (DateTime?)metrics.RecentPerformances.Max(p => p.CompletedAt)
        : null;
    var lastActivityAt = recentActivities.Count > 0
        ? (DateTime?)recentActivities.Max(a => a.CreatedTime)
        : null;
    DateTime? lastActiveAt = (lastAttemptAt, lastActivityAt) switch
    {
      (not null, not null) => lastAttemptAt > lastActivityAt ? lastAttemptAt : lastActivityAt,
      (not null, null)     => lastAttemptAt,
      (null, not null)     => lastActivityAt,
      _                    => null,
    };

    var result = new ClassroomStudentDiagnosticsDto(
        student.Id,
        string.IsNullOrWhiteSpace(student.Name) ? student.UserName ?? "Student" : student.Name,
        student.UserName ?? string.Empty,
        classroom.Name,
        avatarSource,
        student.Diamonds,
        student.Experience,
        level,
        metrics.LatestPerformance?.ScorePercentage ?? 0,
        metrics.LatestPerformance?.Score ?? 0,
        metrics.LatestPerformance?.TotalPoints ?? 0,
        metrics.AverageScore,
        quizzesTaken,
        overallStatus,
        statusTone,
        new List<RadarAxisDto>
        {
          new("Accuracy",    metrics.AccuracyValue),
          new("Mastery",     metrics.MasteryValue),
          new("Consistency", metrics.ConsistencyValue),
          new("Speed",       metrics.SpeedValue),
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
        metrics.RecentPerformances
            .OrderByDescending(item => item.CompletedAt)
            .ToList(),
        lastActiveAt);

    return OperationResult<ClassroomStudentDiagnosticsDto>.SuccessResult(result);
  }

  private async Task<RadarMetrics> BuildMetricsAsync(int studentId, int classroomId)
  {
    // Pre-load ALL student progress rows for this classroom in one query (eliminates N+1).
    var progressByChallenge = (await unitOfWork.ChallengeRepository
        .GetStudentProgressForClassroomAsync(studentId, classroomId))
        .ToDictionary(p => p.ChallengeId);

    var userProgress = await unitOfWork.ProgressionRepository.GetUserProgressAsync(studentId);

    // Iterate over classroom challenges (preserving original logic) but look up progress from
    // the pre-loaded dictionary instead of querying per-challenge.
    var classroomChallenges = await unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(classroomId);
    var recentClassroomPerformances = new List<ClassroomPerformanceItemDto>();

    foreach (var challenge in classroomChallenges)
    {
      if (!progressByChallenge.TryGetValue(challenge.Id, out var progressRow) || !progressRow.HasCompleted)
        continue;

      // Convert BestScore to a true percentage via MaxStars, same as the original handler.
      var estimatedTotal = challenge.MaxStars * 100;
      var scorePercent = progressRow.BestAccuracy.HasValue
          ? (double)progressRow.BestAccuracy.Value
          : (estimatedTotal > 0
              ? Math.Round((double)progressRow.BestScore / estimatedTotal * 100.0, 1)
              : 0);

      recentClassroomPerformances.Add(new ClassroomPerformanceItemDto(
          challenge.Id.ToString(),
          progressRow.FirstCompletedAt ?? progressRow.LastAttemptAt,
          scorePercent,
          progressRow.BestScore,
          estimatedTotal,
          progressRow.BestDurationSeconds.HasValue ? (int)progressRow.BestDurationSeconds.Value : 0));
    }

    var latestPerformance = recentClassroomPerformances
        .OrderByDescending(p => p.CompletedAt)
        .FirstOrDefault();

    var averageScore = recentClassroomPerformances.Count > 0
        ? recentClassroomPerformances.Average(p => p.ScorePercentage)
        : 0;
    var averageTimeSpent = recentClassroomPerformances.Count > 0
        ? recentClassroomPerformances.Average(p => p.TimeSpent)
        : 0;

    // ── Accuracy: average of BestAccuracy (or derived %) across ALL completed challenges ─────
    var accuracyValue = recentClassroomPerformances.Count > 0
        ? Math.Min(100, Math.Round(recentClassroomPerformances.Average(p => p.ScorePercentage)))
        : 0;

    // ── Consistency: inverse of score spread among recent performances ────────────────────────
    // Take the last 5 by completion date for the spread calculation.
    var recentFive = recentClassroomPerformances
        .OrderByDescending(p => p.CompletedAt)
        .Take(5)
        .ToList();

    var consistencyValue = recentFive.Count > 1
        ? Math.Min(100, Math.Max(0,
            100 - recentFive.Max(p => p.ScorePercentage) +
            recentFive.Min(p => p.ScorePercentage)))
        : Math.Round(averageScore);

    // ── Speed: based on average duration of recent performances ──────────────────────────────
    var speedValue = averageTimeSpent > 0
        ? Math.Max(20, Math.Min(100, Math.Round(100 - (averageTimeSpent / 6.0))))
        : 50;

    // ── Mastery: average accuracy across all completed challenges ─────────────────────────────
    var masteryValue = recentClassroomPerformances.Count > 0
        ? Math.Min(100, Math.Round(recentClassroomPerformances.Average(p => p.ScorePercentage)))
        : 0;

    return new RadarMetrics(
        AccuracyValue:    accuracyValue,
        ConsistencyValue: consistencyValue,
        SpeedValue:       speedValue,
        MasteryValue:     masteryValue,
        AverageScore:     averageScore,
        CompletedCount:   recentClassroomPerformances.Count,
        UserProgress:     userProgress,
        LatestPerformance: latestPerformance,
        RecentPerformances: recentClassroomPerformances);
  }

  // Local value-object — keeps the cached blob strongly-typed and minimal.
  private sealed record RadarMetrics(
      double AccuracyValue,
      double ConsistencyValue,
      double SpeedValue,
      double MasteryValue,
      double AverageScore,
      int CompletedCount,
      CleanArc.Domain.Entities.Progression.UserProgress? UserProgress,
      ClassroomPerformanceItemDto? LatestPerformance,
      List<ClassroomPerformanceItemDto> RecentPerformances);
}
