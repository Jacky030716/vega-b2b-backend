using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetStudentClassroomsQueryHandler : IRequestHandler<GetStudentClassroomsQuery, OperationResult<List<ClassroomDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetStudentClassroomsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ClassroomDto>>> Handle(GetStudentClassroomsQuery request, CancellationToken cancellationToken)
  {
    var classrooms = await _unitOfWork.ClassroomRepository.GetStudentClassroomsAsync(request.UserId);

    var result = new List<ClassroomDto>();
    foreach (var c in classrooms)
    {
      var studentCount = await _unitOfWork.ClassroomRepository.GetStudentCountAsync(c.Id);
      var challenges = await _unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(c.Id);
      result.Add(new ClassroomDto(c.Id, c.Name, c.Description, c.Subject, c.Thumbnail,
          c.JoinCode, c.TeacherId, c.Teacher?.UserName ?? "", studentCount, challenges.Count));
    }

    return OperationResult<List<ClassroomDto>>.SuccessResult(result);
  }
}
