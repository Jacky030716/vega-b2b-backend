using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries;

internal class GetChallengeLeaderboardQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetChallengeLeaderboardQuery, OperationResult<List<ChallengeLeaderboardEntryDto>>>
{
    public async ValueTask<OperationResult<List<ChallengeLeaderboardEntryDto>>> Handle(
        GetChallengeLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var rows = await unitOfWork.ChallengeRepository
            .GetChallengeLeaderboardAsync(request.ChallengeId, request.ClassroomId);

        var ranked = rows
            .Select((row, index) =>
            {
                var avatarSource = string.IsNullOrWhiteSpace(row.User?.AvatarUrl)
                    ? row.User?.AvatarId
                    : row.User?.AvatarUrl;

                return new ChallengeLeaderboardEntryDto(
                    Rank: index + 1,
                    UserId: row.UserId,
                    StudentName: string.IsNullOrWhiteSpace(row.User?.Name)
                        ? row.User?.UserName ?? "Student"
                        : row.User?.Name ?? "Student",
                    AvatarId: avatarSource,
                    AttemptCount: row.AttemptCount,
                    HasCompleted: row.HasCompleted,
                    BestScore: row.BestScore,
                    BestStars: row.BestStars,
                    BestAccuracy: row.BestAccuracy,
                    BestDurationSeconds: row.BestDurationSeconds,
                    FirstCompletedAt: row.FirstCompletedAt,
                    LastAttemptAt: row.LastAttemptAt
                );
            })
            .ToList();

        return OperationResult<List<ChallengeLeaderboardEntryDto>>.SuccessResult(ranked);
    }
}
