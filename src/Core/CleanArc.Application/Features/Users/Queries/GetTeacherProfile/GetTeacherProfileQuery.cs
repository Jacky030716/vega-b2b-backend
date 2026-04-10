using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetTeacherProfile;

public record GetTeacherProfileQuery(int TeacherId)
    : IRequest<OperationResult<TeacherProfileDto>>;

public record TeacherProfileDto(
    string FullName,
    string Email,
    string RoleLabel,
    string? AvatarUrl,
    string? AvatarVariant,
    TeacherProfileStatsDto Stats,
    TeacherPreferencesDto Preferences);

public record TeacherProfileStatsDto(
    int TotalStudentsManaged,
    int ChallengesLaunched,
    int DiamondsAwarded);

public record TeacherPreferencesDto(
    bool WeeklyAiInsightsEmail,
    bool InactiveStudentAlerts);
