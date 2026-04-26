using CleanArc.Application.Features.Classrooms.Queries;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Challenges.Queries;

public record ChallengeBoardDto(
    int ClassId,
    IReadOnlyList<ClassroomChallengeDto> Recommended,
    IReadOnlyList<ClassroomChallengeDto> Active,
    IReadOnlyList<ClassroomChallengeDto> Scheduled,
    IReadOnlyList<ClassroomChallengeDto> Library);

public sealed record GetChallengeBoardQuery(
    int ClassId,
    int RequestingTeacherId)
    : IRequest<OperationResult<ChallengeBoardDto>>;

public sealed record GetRecommendedChallengesQuery(
    int ClassId,
    int RequestingTeacherId)
    : IRequest<OperationResult<IReadOnlyList<ClassroomChallengeDto>>>;

internal sealed class GetChallengeBoardQueryHandler(ISender sender)
    : IRequestHandler<GetChallengeBoardQuery, OperationResult<ChallengeBoardDto>>
{
    public async ValueTask<OperationResult<ChallengeBoardDto>> Handle(
        GetChallengeBoardQuery request,
        CancellationToken cancellationToken)
    {
        var challengesResult = await sender.Send(
            new GetClassroomChallengesQuery(request.ClassId, request.RequestingTeacherId),
            cancellationToken);

        if (!challengesResult.IsSuccess)
        {
            var errorMessage = challengesResult.ErrorMessage ?? "Unable to load classroom challenges";
            return challengesResult.IsNotFound
                ? OperationResult<ChallengeBoardDto>.NotFoundResult(errorMessage)
                : challengesResult.IsUnauthorized
                    ? OperationResult<ChallengeBoardDto>.UnauthorizedResult(errorMessage)
                    : OperationResult<ChallengeBoardDto>.FailureResult(errorMessage);
        }

        var challenges = challengesResult.Result ?? new List<ClassroomChallengeDto>();

        var recommended = challenges
            .Where(item => item.RecommendedScore > 0 && item.LifecycleState is "ACTIVE" or "SCHEDULED")
            .OrderByDescending(item => item.RecommendedScore)
            .ThenByDescending(item => item.IsPinned)
            .Take(3)
            .ToList();

        var active = challenges
            .Where(item => item.LifecycleState == "ACTIVE")
            .OrderByDescending(item => item.IsPinned)
            .ThenByDescending(item => item.RecommendedScore)
            .ThenByDescending(item => item.LastActivityAt)
            .ToList();

        var scheduled = challenges
            .Where(item => item.LifecycleState == "SCHEDULED")
            .OrderBy(item => item.AssignedAt ?? item.CreatedAt)
            .ThenBy(item => item.DueAt ?? DateTime.MaxValue)
            .ToList();

        var library = challenges
            .Where(item => item.LifecycleState is "DRAFT" or "COMPLETED" or "ARCHIVED")
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.RecommendedScore)
            .ToList();

        var board = new ChallengeBoardDto(request.ClassId, recommended, active, scheduled, library);
        return OperationResult<ChallengeBoardDto>.SuccessResult(board);
    }
}

internal sealed class GetRecommendedChallengesQueryHandler(ISender sender)
    : IRequestHandler<GetRecommendedChallengesQuery, OperationResult<IReadOnlyList<ClassroomChallengeDto>>>
{
    public async ValueTask<OperationResult<IReadOnlyList<ClassroomChallengeDto>>> Handle(
        GetRecommendedChallengesQuery request,
        CancellationToken cancellationToken)
    {
        var challengesResult = await sender.Send(
            new GetClassroomChallengesQuery(request.ClassId, request.RequestingTeacherId),
            cancellationToken);

        if (!challengesResult.IsSuccess)
        {
            var errorMessage = challengesResult.ErrorMessage ?? "Unable to load classroom challenges";
            return challengesResult.IsNotFound
                ? OperationResult<IReadOnlyList<ClassroomChallengeDto>>.NotFoundResult(errorMessage)
                : challengesResult.IsUnauthorized
                    ? OperationResult<IReadOnlyList<ClassroomChallengeDto>>.UnauthorizedResult(errorMessage)
                    : OperationResult<IReadOnlyList<ClassroomChallengeDto>>.FailureResult(errorMessage);
        }

        var recommended = challengesResult.Result?
            .Where(item => item.RecommendedScore > 0 && item.LifecycleState is "ACTIVE" or "SCHEDULED")
            .OrderByDescending(item => item.RecommendedScore)
            .ThenByDescending(item => item.IsPinned)
            .Take(3)
            .ToList() ?? new List<ClassroomChallengeDto>();

        return OperationResult<IReadOnlyList<ClassroomChallengeDto>>.SuccessResult(recommended);
    }
}
