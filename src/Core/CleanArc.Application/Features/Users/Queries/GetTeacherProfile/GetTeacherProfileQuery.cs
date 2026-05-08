using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetTeacherProfile;

public record GetTeacherProfileQuery(int TeacherId)
    : IRequest<OperationResult<TeacherProfileDto>>;

public record TeacherProfileDto(
    string FullName,
    string Email,
    string RoleLabel,
    string InstitutionName,
    string AccessScope,
    string? AvatarUrl,
    string? AvatarVariant,
    TeacherProfileStatsDto Stats,
    TeacherSubscriptionSnapshotDto? Subscription,
    TeacherPreferencesDto Preferences);

public record TeacherProfileStatsDto(
    int ActiveClassrooms,
    int ActiveStudents,
    int StudentsNeedingSupport,
    int? AiGenerationsRemaining);

public record TeacherSubscriptionSnapshotDto(
    string PlanTier,
    int SeatsUsed,
    int SeatLimit,
    string BillingManagedBy);

public record TeacherPreferencesDto(
    bool WeeklyAiInsightsEmail,
    bool InactiveStudentAlerts);
