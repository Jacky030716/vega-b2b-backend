using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Analytics;

public sealed record GetStudentMasteryQuery(
    int StudentId)
    : IRequest<IReadOnlyList<StudentWordMasteryDto>>;

public sealed record GetStudentWeaknessSummaryQuery(
    int StudentId)
    : IRequest<WeaknessSummaryDto>;

public sealed record GetStudentRecommendedNextChallengesQuery(
    int StudentId)
    : IRequest<IReadOnlyList<AdaptiveRecommendationDto>>;

public sealed record GetClassWeaknessOverviewQuery(
    int ClassId)
    : IRequest<ClassWeaknessOverviewDto>;

public sealed record GetClassModuleProgressQuery(
    int ClassId)
    : IRequest<IReadOnlyList<ModuleProgressDto>>;

public sealed record GetStudentPerformanceQuery(
    int StudentId)
    : IRequest<StudentPerformanceDto>;
