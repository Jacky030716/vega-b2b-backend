using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetTeacherClassroomsQueryHandler : IRequestHandler<GetTeacherClassroomsQuery, OperationResult<List<ClassroomDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetTeacherClassroomsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ClassroomDto>>> Handle(GetTeacherClassroomsQuery request, CancellationToken cancellationToken)
  {
    var classrooms = await _unitOfWork.ClassroomRepository.GetTeacherClassroomsAsync(request.TeacherId);

    var result = new List<ClassroomDto>();
    foreach (var c in classrooms)
    {
      var studentCount = await _unitOfWork.ClassroomRepository.GetStudentCountAsync(c.Id);
      var quizCount = await _unitOfWork.ClassroomRepository.GetQuizCountAsync(c.Id);
      result.Add(new ClassroomDto(c.Id, c.Name, c.Description, c.Subject, c.Thumbnail,
          c.JoinCode, c.TeacherId, "", studentCount, quizCount));
    }

    return OperationResult<List<ClassroomDto>>.SuccessResult(result);
  }
}
