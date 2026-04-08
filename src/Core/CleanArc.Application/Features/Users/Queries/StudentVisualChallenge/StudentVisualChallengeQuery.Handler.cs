using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualChallenge;

internal class StudentVisualChallengeQueryHandler : IRequestHandler<StudentVisualChallengeQuery, OperationResult<StudentVisualChallengeResponse>>
{
  private readonly IUnitOfWork _unitOfWork;

  public StudentVisualChallengeQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<StudentVisualChallengeResponse>> Handle(
      StudentVisualChallengeQuery request,
      CancellationToken cancellationToken)
  {
    var normalizedCode = request.ClassCode.Trim().ToUpperInvariant();
    var classroom = await _unitOfWork.ClassroomRepository.GetClassroomByJoinCodeAsync(normalizedCode);

    if (classroom == null)
      return OperationResult<StudentVisualChallengeResponse>.NotFoundResult("Classroom not found");

    var members = await _unitOfWork.ClassroomRepository.GetClassroomMembersAsync(classroom.Id);
    var credentials = await _unitOfWork.StudentCredentialRepository.GetByClassroomIdAsync(classroom.Id);
    var credentialByUserId = credentials
        .Where(sc => sc.IsActive)
        .ToDictionary(sc => sc.UserId, sc => sc);

    var studentDtos = members
        .Where(member => credentialByUserId.ContainsKey(member.UserId))
        .Select(member =>
        {
          var credential = credentialByUserId[member.UserId];
          var displayName = string.IsNullOrWhiteSpace(member.User.Name) ? member.User.UserName : member.User.Name;
          var avatarId = string.IsNullOrWhiteSpace(member.User.AvatarId) ? "0" : member.User.AvatarId;

          return new StudentVisualChallengeStudentDto(
                  member.UserId,
                  displayName,
                  avatarId,
                  member.User.AvatarUrl,
                  credential.StudentLoginCode
              );
        })
        .OrderBy(student => student.DisplayName)
        .ToList();

    var response = new StudentVisualChallengeResponse(
        classroom.Id,
        classroom.Name,
        studentDtos
    );

    return OperationResult<StudentVisualChallengeResponse>.SuccessResult(response);
  }
}
