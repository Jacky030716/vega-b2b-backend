using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class ArchiveClassroomCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ArchiveClassroomCommand, OperationResult<ArchiveClassroomResult>>
{
  public async ValueTask<OperationResult<ArchiveClassroomResult>> Handle(ArchiveClassroomCommand request, CancellationToken cancellationToken)
  {
    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId, tracking: true);
    if (classroom is null)
      return OperationResult<ArchiveClassroomResult>.NotFoundResult("Classroom not found");

    if (!request.IsAdmin && classroom.TeacherId != request.RequestingUserId)
      return OperationResult<ArchiveClassroomResult>.ForbiddenResult("You do not manage this classroom");

    await unitOfWork.ClassroomRepository.ArchiveClassroomAsync(classroom, request.RequestingUserId);

    return OperationResult<ArchiveClassroomResult>.SuccessResult(
        new ArchiveClassroomResult(true, "Classroom archived successfully."));
  }
}
