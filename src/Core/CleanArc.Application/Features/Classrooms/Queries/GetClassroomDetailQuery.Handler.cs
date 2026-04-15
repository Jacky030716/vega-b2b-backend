using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Globalization;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomDetailQueryHandler : IRequestHandler<GetClassroomDetailQuery, OperationResult<ClassroomDetailDto>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetClassroomDetailQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<ClassroomDetailDto>> Handle(GetClassroomDetailQuery request, CancellationToken cancellationToken)
  {
    var classroom = await _unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom == null)
      return OperationResult<ClassroomDetailDto>.NotFoundResult("Classroom not found");

    var studentCount = await _unitOfWork.ClassroomRepository.GetStudentCountAsync(request.ClassroomId);
    var quizzes = await _unitOfWork.ClassroomRepository.GetClassroomQuizzesAsync(request.ClassroomId);

    var quizDtos = new List<ClassroomQuizDto>(quizzes.Count);
    foreach (var q in quizzes)
    {
      string? title = null;
      string? gameKey = null;

      if (int.TryParse(q.QuizId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var challengeId))
      {
        var challenge = await _unitOfWork.ChallengeRepository.GetChallengeByIdAsync(challengeId);
        title = challenge?.Title;
        gameKey = challenge?.Game?.Key;
      }

      quizDtos.Add(new ClassroomQuizDto(q.Id, q.QuizId, q.AssignedDate, q.DueDate, title, gameKey));
    }

    var result = new ClassroomDetailDto(classroom.Id, classroom.Name, classroom.Description, classroom.Subject,
        classroom.Thumbnail, classroom.JoinCode, classroom.TeacherId, classroom.Teacher?.UserName ?? "",
        studentCount, quizDtos);

    return OperationResult<ClassroomDetailDto>.SuccessResult(result);
  }
}
