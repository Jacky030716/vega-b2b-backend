using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Challenges.Commands;

public sealed record UpdateChallengeLifecycleCommand(
    int ChallengeId,
    int RequestingTeacherId,
    ChallengeLifecycleState LifecycleState,
    bool IsPinned)
    : IRequest<OperationResult<bool>>;

internal sealed class UpdateChallengeLifecycleCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateChallengeLifecycleCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(
        UpdateChallengeLifecycleCommand request,
        CancellationToken cancellationToken)
    {
        var challenge = await unitOfWork.ChallengeRepository.GetChallengeByIdAsync(request.ChallengeId);
        if (challenge is null)
            return OperationResult<bool>.NotFoundResult("Challenge not found");

        if (challenge.ClassroomId.HasValue)
        {
            var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(challenge.ClassroomId.Value);
            if (classroom is null)
                return OperationResult<bool>.NotFoundResult("Classroom not found");

            if (classroom.TeacherId != request.RequestingTeacherId)
                return OperationResult<bool>.UnauthorizedResult("You do not manage this classroom");
        }
        else if (challenge.CreatedById != request.RequestingTeacherId)
        {
            return OperationResult<bool>.UnauthorizedResult("You do not manage this challenge");
        }

        challenge.LifecycleState = request.LifecycleState;
        challenge.IsPinned = request.IsPinned && request.LifecycleState is ChallengeLifecycleState.Active or ChallengeLifecycleState.Scheduled;
        challenge.LastActivityAt = request.LifecycleState is ChallengeLifecycleState.Active or ChallengeLifecycleState.Completed
            ? DateTime.UtcNow
            : challenge.LastActivityAt;

        if (request.LifecycleState == ChallengeLifecycleState.Archived)
            challenge.IsPinned = false;

        await unitOfWork.ChallengeRepository.UpdateChallengeAsync(challenge);
        return OperationResult<bool>.SuccessResult(true);
    }
}
