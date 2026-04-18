using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
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

        var dtos = new List<ClassroomChallengeDto>(challenges.Count);
        foreach (var challenge in challenges)
        {
            var leaderboard = await unitOfWork.ChallengeRepository
                .GetChallengeLeaderboardAsync(challenge.Id, request.ClassroomId);

            var completedCount = leaderboard.Count(lp => lp.HasCompleted);

            dtos.Add(new ClassroomChallengeDto(
                ChallengeId: challenge.Id,
                GameKey: challenge.Game?.Key ?? string.Empty,
                Title: challenge.Title,
                Description: challenge.Description,
                DifficultyLevel: challenge.DifficultyLevel,
                OrderIndex: challenge.OrderIndex,
                IsAIGenerated: challenge.IsAIGenerated,
                CreatedAt: challenge.CreatedTime,
                CompletedStudentCount: completedCount,
                TotalStudentCount: totalStudents
            ));
        }

        return OperationResult<List<ClassroomChallengeDto>>.SuccessResult(dtos);
    }
}
