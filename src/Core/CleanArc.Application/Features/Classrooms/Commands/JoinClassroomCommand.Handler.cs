using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class JoinClassroomCommandHandler : IRequestHandler<JoinClassroomCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public JoinClassroomCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(JoinClassroomCommand request, CancellationToken cancellationToken)
  {
    var normalizedJoinCode = request.JoinCode?.Trim().ToUpperInvariant() ?? string.Empty;
    var classroom = await _unitOfWork.ClassroomRepository.GetClassroomByJoinCodeAsync(normalizedJoinCode);
    if (classroom == null)
      return OperationResult<int>.NotFoundResult("Invalid join code or classroom not found");

    var existing = await _unitOfWork.ClassroomRepository.GetClassroomStudentAsync(classroom.Id, request.UserId);
    if (existing != null)
      return OperationResult<int>.FailureResult("You are already in this classroom");

    await _unitOfWork.ClassroomRepository.JoinClassroomAsync(new ClassroomStudent
    {
      ClassroomId = classroom.Id,
      UserId = request.UserId,
      JoinedDate = DateTime.UtcNow
    });

    return OperationResult<int>.SuccessResult(classroom.Id);
  }
}
