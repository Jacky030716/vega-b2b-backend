using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Orchestration;

public sealed record GetAdaptiveRecommendationsQuery(
    int StudentId,
    GenerateAdaptiveChallengeRequest? Request)
    : IRequest<IReadOnlyList<AdaptiveRecommendationDto>>;

public sealed record GenerateAdaptiveChallengeCommand(
    GenerateAdaptiveChallengeRequest Request)
    : IRequest<GeneratedAdaptiveChallengePreviewDto>;

public sealed record AssignAdaptiveChallengeCommand(
    AssignAdaptiveChallengeRequest Request,
    int TeacherId)
    : IRequest<AssignedAdaptiveChallengeDto>;

public sealed record GetAdaptiveChallengeByIdQuery(
    int ChallengeId)
    : IRequest<GeneratedAdaptiveChallengePreviewDto?>;
