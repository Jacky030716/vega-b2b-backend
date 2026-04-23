using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Orchestration;

internal sealed class GetAdaptiveRecommendationsQueryHandler(IChallengeOrchestrator orchestrator)
    : IRequestHandler<GetAdaptiveRecommendationsQuery, IReadOnlyList<AdaptiveRecommendationDto>>
{
    public async ValueTask<IReadOnlyList<AdaptiveRecommendationDto>> Handle(
        GetAdaptiveRecommendationsQuery request,
        CancellationToken cancellationToken)
        => await orchestrator.RecommendForStudentAsync(request.StudentId, request.Request, cancellationToken);
}

internal sealed class GenerateAdaptiveChallengeCommandHandler(IChallengeOrchestrator orchestrator)
    : IRequestHandler<GenerateAdaptiveChallengeCommand, GeneratedAdaptiveChallengePreviewDto>
{
    public async ValueTask<GeneratedAdaptiveChallengePreviewDto> Handle(
        GenerateAdaptiveChallengeCommand request,
        CancellationToken cancellationToken)
        => await orchestrator.GenerateAsync(request.Request, cancellationToken);
}

internal sealed class AssignAdaptiveChallengeCommandHandler(IChallengeOrchestrator orchestrator)
    : IRequestHandler<AssignAdaptiveChallengeCommand, AssignedAdaptiveChallengeDto>
{
    public async ValueTask<AssignedAdaptiveChallengeDto> Handle(
        AssignAdaptiveChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = request.Request with { CreatedByTeacherId = request.TeacherId };
        return await orchestrator.AssignAsync(normalizedRequest, cancellationToken);
    }
}

internal sealed class GetAdaptiveChallengeByIdQueryHandler(IChallengeOrchestrator orchestrator)
    : IRequestHandler<GetAdaptiveChallengeByIdQuery, GeneratedAdaptiveChallengePreviewDto?>
{
    public async ValueTask<GeneratedAdaptiveChallengePreviewDto?> Handle(
        GetAdaptiveChallengeByIdQuery request,
        CancellationToken cancellationToken)
        => await orchestrator.GetChallengeAsync(request.ChallengeId, cancellationToken);
}
