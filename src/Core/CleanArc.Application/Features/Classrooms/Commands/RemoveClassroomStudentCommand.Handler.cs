using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal sealed class RemoveClassroomStudentCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveClassroomStudentCommand, OperationResult<int>>
{
    public async ValueTask<OperationResult<int>> Handle(
        RemoveClassroomStudentCommand request,
        CancellationToken cancellationToken)
    {
        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
        if (classroom is null)
        {
            return OperationResult<int>.NotFoundResult("Classroom not found");
        }

        if (!request.IsAdmin && classroom.TeacherId != request.RequestingUserId)
        {
            return OperationResult<int>.UnauthorizedResult("You do not manage this classroom");
        }

        var existing = await unitOfWork.ClassroomRepository.GetClassroomStudentAsync(request.ClassroomId, request.StudentId);
        if (existing is null)
        {
            return OperationResult<int>.NotFoundResult("Student not found in this classroom");
        }

        var removed = await unitOfWork.ClassroomRepository.RemoveClassroomStudentAsync(request.ClassroomId, request.StudentId);
        if (!removed)
        {
            return OperationResult<int>.FailureResult("Failed to remove the student from the classroom");
        }

        var credentials = await unitOfWork.StudentCredentialRepository.GetByUserIdAsync(request.StudentId);
        var classroomCredential = credentials.FirstOrDefault(credential => credential.ClassroomId == request.ClassroomId);
        if (classroomCredential is not null)
        {
            classroomCredential.IsActive = false;
            classroomCredential.FailedAttempts = 0;
            classroomCredential.LastFailedAt = null;
            await unitOfWork.StudentCredentialRepository.UpdateAsync(classroomCredential);
        }

        await unitOfWork.CommitAsync();

        return OperationResult<int>.SuccessResult(request.ClassroomId);
    }
}
