using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

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
    var challenges = await _unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(request.ClassroomId);

    var result = new ClassroomDetailDto(
        classroom.Id,
        classroom.Name,
        classroom.Description,
        classroom.Subject,
        classroom.Thumbnail,
        classroom.JoinCode,
        classroom.YearLevel,
        classroom.TeacherId,
        classroom.Teacher?.UserName ?? "",
        studentCount,
        challenges.Count);

    return OperationResult<ClassroomDetailDto>.SuccessResult(result);
  }
}
