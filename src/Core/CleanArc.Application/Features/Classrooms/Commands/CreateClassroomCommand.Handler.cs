using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class CreateClassroomCommandHandler : IRequestHandler<CreateClassroomCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateClassroomCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
  {
    var joinCode = GenerateJoinCode();

    var classroom = new Classroom
    {
      Name = request.Name,
      Description = request.Description,
      Subject = request.Subject,
      YearLevel = Math.Clamp(request.YearLevel, 1, 6),
      Thumbnail = request.Thumbnail,
      JoinCode = joinCode,
      TeacherId = request.TeacherId,
      IsActive = true
    };

    var created = await _unitOfWork.ClassroomRepository.CreateClassroomAsync(classroom);
    return OperationResult<int>.SuccessResult(created.Id);
  }

  private static string GenerateJoinCode()
  {
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
  }
}
