using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class AssignQuizCommandHandler : IRequestHandler<AssignQuizCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public AssignQuizCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(AssignQuizCommand request, CancellationToken cancellationToken)
  {
    var classroom = await _unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom == null)
      return OperationResult<bool>.NotFoundResult("Classroom not found");

    if (classroom.TeacherId != request.TeacherId)
      return OperationResult<bool>.UnauthorizedResult("Only the classroom teacher can assign quizzes");

    await _unitOfWork.ClassroomRepository.AssignQuizAsync(new ClassroomQuiz
    {
      ClassroomId = request.ClassroomId,
      QuizId = request.QuizId,
      AssignedDate = DateTime.UtcNow,
      DueDate = request.DueDate
    });

    return OperationResult<bool>.SuccessResult(true);
  }
}
