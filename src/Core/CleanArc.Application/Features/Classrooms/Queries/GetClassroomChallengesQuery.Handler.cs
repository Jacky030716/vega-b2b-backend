using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomChallengesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClassroomChallengesQuery, OperationResult<List<ClassroomChallengeDto>>>
{
    public async ValueTask<OperationResult<List<ClassroomChallengeDto>>> Handle(
        GetClassroomChallengesQuery request, CancellationToken cancellationToken)
    {
        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
        if (classroom is null)
            return OperationResult<List<ClassroomChallengeDto>>.NotFoundResult("Classroom not found");

        if (classroom.TeacherId != request.RequestingTeacherId)
            return OperationResult<List<ClassroomChallengeDto>>.UnauthorizedResult("You do not manage this classroom");

        var challenges = await unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(request.ClassroomId);
        var totalStudents = await unitOfWork.ClassroomRepository.GetStudentCountAsync(request.ClassroomId);
        var leaderboardSnapshots = await unitOfWork.ChallengeRepository.GetChallengeLeaderboardSnapshotsAsync(
            request.ClassroomId,
            challenges.Select(challenge => challenge.Id).ToArray());
        var utcNow = DateTime.UtcNow;

        var dtos = new List<ClassroomChallengeDto>(challenges.Count);
        foreach (var challenge in challenges)
        {
            leaderboardSnapshots.TryGetValue(challenge.Id, out var leaderboardSnapshot);

            var completedCount = leaderboardSnapshot?.CompletedCount ?? 0;
            var lastLeaderboardActivity = leaderboardSnapshot?.LastAttemptAt;

            var lastActivityAt = challenge.LastActivityAt
                ?? lastLeaderboardActivity
                ?? challenge.AssignedAt
                ?? challenge.CreatedTime;

            var effectiveLifecycle = ResolveLifecycleState(
                challenge,
                completedCount,
                totalStudents,
                lastActivityAt,
                utcNow);

            var recommendation = ScoreChallenge(
                challenge,
                completedCount,
                totalStudents,
                lastActivityAt,
                utcNow,
                challenges);

            dtos.Add(new ClassroomChallengeDto(
                ChallengeId: challenge.Id,
                GameKey: challenge.Game?.Key ?? string.Empty,
                Title: challenge.Title,
                Description: challenge.Description,
                DifficultyLevel: challenge.DifficultyLevel,
                OrderIndex: challenge.OrderIndex,
                IsAIGenerated: challenge.IsAIGenerated,
                CreatedAt: challenge.CreatedTime,
                LifecycleState: effectiveLifecycle.ToString().ToUpperInvariant(),
                IsPinned: challenge.IsPinned,
                RecommendedScore: recommendation.Score,
                LastActivityAt: lastActivityAt,
                AssignedAt: challenge.AssignedAt,
                DueAt: challenge.DueAt,
                ModuleId: challenge.ModuleId,
                SourceType: challenge.SourceType,
                ChallengeMode: challenge.ChallengeMode,
                GameTemplateCode: null,
                RecommendationReason: recommendation.Reason,
                CompletedStudentCount: completedCount,
                TotalStudentCount: totalStudents
            ));
        }

        return OperationResult<List<ClassroomChallengeDto>>.SuccessResult(dtos);
    }

    private static ChallengeLifecycleState ResolveLifecycleState(
        Challenge challenge,
        int completedCount,
        int totalStudents,
        DateTime? lastActivityAt,
        DateTime utcNow)
    {
        if (challenge.LifecycleState is ChallengeLifecycleState.Archived or ChallengeLifecycleState.Completed)
            return challenge.LifecycleState;

        if (challenge.Status.Equals("archived", StringComparison.OrdinalIgnoreCase))
            return ChallengeLifecycleState.Archived;

        if (challenge.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            return ChallengeLifecycleState.Completed;

        if (!challenge.AssignedAt.HasValue)
        {
            if (challenge.ClassroomId.HasValue)
                return ChallengeLifecycleState.Active;

            return ChallengeLifecycleState.Draft;
        }

        if (totalStudents > 0 && completedCount >= totalStudents)
            return ChallengeLifecycleState.Completed;

        if (challenge.DueAt.HasValue && challenge.DueAt.Value > utcNow && completedCount == 0 && !lastActivityAt.HasValue)
            return ChallengeLifecycleState.Scheduled;

        if (completedCount > 0 || lastActivityAt.HasValue)
            return ChallengeLifecycleState.Active;

        return ChallengeLifecycleState.Scheduled;
    }

    private static (double Score, string Reason) ScoreChallenge(
        Challenge challenge,
        int completedCount,
        int totalStudents,
        DateTime? lastActivityAt,
        DateTime utcNow,
        IReadOnlyCollection<Challenge> allChallenges)
    {
        var score = 0d;
        var reasons = new List<string>();

        if (LooksLikeWeakWordsChallenge(challenge))
        {
            score += 30;
            reasons.Add("Weak words detected");
        }

        if (challenge.ModuleId.HasValue)
        {
            score += 10;
            reasons.Add("Module practice");
        }

        if (challenge.AssignedAt.HasValue && (utcNow - challenge.AssignedAt.Value).TotalDays >= 7)
        {
            score += 20;
            reasons.Add("Needs review");
        }

        var masteryRatio = totalStudents > 0 ? (double)completedCount / totalStudents : 0;
        if (masteryRatio < 0.6)
        {
            score += 15;
            reasons.Add("Students struggling");
        }

        if (completedCount > 0 && totalStudents > 0 && completedCount >= totalStudents)
        {
            score -= 20;
            reasons.Add("Already completed recently");
        }

        if (HasDuplicateSimilarChallenge(challenge, allChallenges))
        {
            score -= 10;
            reasons.Add("Duplicate similar challenge exists");
        }

        if (!lastActivityAt.HasValue || (utcNow - lastActivityAt.Value).TotalDays > 10)
        {
            score += 20;
            reasons.Add("Not attempted recently");
        }

        score = Math.Clamp(score, 0, 100);

        if (reasons.Count == 0)
            reasons.Add("Balanced class activity");

        return (score, string.Join(". ", reasons));
    }

    private static bool LooksLikeWeakWordsChallenge(Challenge challenge)
    {
        var haystack = string.Join(' ', new[]
        {
            challenge.Title,
            challenge.Description,
            challenge.ChallengeMode ?? string.Empty,
            challenge.SourceType ?? string.Empty,
            challenge.ConfigJson ?? string.Empty
        }).ToLowerInvariant();

        return haystack.Contains("weak", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("remedial", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("review", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("improve", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDuplicateSimilarChallenge(Challenge challenge, IReadOnlyCollection<Challenge> allChallenges)
    {
        return allChallenges.Any(other =>
            other.Id != challenge.Id &&
            other.GameId == challenge.GameId &&
            other.ModuleId == challenge.ModuleId &&
            string.Equals(other.ChallengeMode, challenge.ChallengeMode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(other.SourceType, challenge.SourceType, StringComparison.OrdinalIgnoreCase));
    }
}
