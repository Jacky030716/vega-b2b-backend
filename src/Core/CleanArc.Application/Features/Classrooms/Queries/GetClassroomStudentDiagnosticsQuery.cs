using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetClassroomStudentDiagnosticsQuery(
    int ClassroomId,
    int StudentId,
    int RequestingTeacherId)
    : IRequest<OperationResult<ClassroomStudentDiagnosticsDto>>;

public record ClassroomStudentDiagnosticsDto(
    int UserId,
    string StudentName,
    string LoginId,
    string ClassroomName,
    string? AvatarId,
    int Diamonds,
    int Experience,
    int Level,
    double LatestScore,
    int LatestPoints,
    int LatestTotalPoints,
    double AverageScore,
    int TotalQuizzesTaken,
    string OverallStatus,
    string StatusTone,
    List<RadarAxisDto> RadarAxes,
    List<StudentActivityItemDto> RecentActivities,
    List<StudentAchievementItemDto> RecentAchievements,
    List<ClassroomPerformanceItemDto> RecentClassroomPerformances);

public record RadarAxisDto(string Label, double Value);

public record StudentActivityItemDto(
    int Id,
    string Type,
    string Title,
    string Description,
    DateTime CreatedAt);

public record StudentAchievementItemDto(
    int BadgeId,
    string Name,
    string Category,
    DateTime EarnedAt);

public record ClassroomPerformanceItemDto(
    string QuizId,
    DateTime CompletedAt,
    double ScorePercentage,
    int Score,
    int TotalPoints,
    int TimeSpent);
