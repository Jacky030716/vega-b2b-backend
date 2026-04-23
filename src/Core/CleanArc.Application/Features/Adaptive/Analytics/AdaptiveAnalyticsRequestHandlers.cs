using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Analytics;

internal sealed class GetStudentMasteryQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetStudentMasteryQuery, IReadOnlyList<StudentWordMasteryDto>>
{
    public async ValueTask<IReadOnlyList<StudentWordMasteryDto>> Handle(
        GetStudentMasteryQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetMasteryAsync(request.StudentId, cancellationToken);
}

internal sealed class GetStudentWeaknessSummaryQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetStudentWeaknessSummaryQuery, WeaknessSummaryDto>
{
    public async ValueTask<WeaknessSummaryDto> Handle(
        GetStudentWeaknessSummaryQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetWeaknessSummaryAsync(request.StudentId, cancellationToken);
}

internal sealed class GetStudentRecommendedNextChallengesQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetStudentRecommendedNextChallengesQuery, IReadOnlyList<AdaptiveRecommendationDto>>
{
    public async ValueTask<IReadOnlyList<AdaptiveRecommendationDto>> Handle(
        GetStudentRecommendedNextChallengesQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetRecommendedNextChallengesAsync(request.StudentId, cancellationToken);
}

internal sealed class GetClassWeaknessOverviewQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetClassWeaknessOverviewQuery, ClassWeaknessOverviewDto>
{
    public async ValueTask<ClassWeaknessOverviewDto> Handle(
        GetClassWeaknessOverviewQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetClassWeaknessOverviewAsync(request.ClassId, cancellationToken);
}

internal sealed class GetClassModuleProgressQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetClassModuleProgressQuery, IReadOnlyList<ModuleProgressDto>>
{
    public async ValueTask<IReadOnlyList<ModuleProgressDto>> Handle(
        GetClassModuleProgressQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetModuleProgressAsync(request.ClassId, cancellationToken);
}

internal sealed class GetStudentPerformanceQueryHandler(IAdaptiveAnalyticsService adaptiveAnalyticsService)
    : IRequestHandler<GetStudentPerformanceQuery, StudentPerformanceDto>
{
    public async ValueTask<StudentPerformanceDto> Handle(
        GetStudentPerformanceQuery request,
        CancellationToken cancellationToken)
        => await adaptiveAnalyticsService.GetStudentPerformanceAsync(request.StudentId, cancellationToken);
}
