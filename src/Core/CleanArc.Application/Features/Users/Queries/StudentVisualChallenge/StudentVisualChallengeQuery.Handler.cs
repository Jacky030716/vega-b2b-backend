using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
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

    // Self-healing: if any enrolled students are missing classroom-specific credentials, generate them dynamically
    var missingCredMembers = members.Where(m => !credentialByUserId.ContainsKey(m.UserId)).ToList();
    if (missingCredMembers.Count > 0)
    {
      var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var member in missingCredMembers)
      {
        string loginCode;
        while (true)
        {
          var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
          if (existingCodes.Contains(candidate))
            continue;

          var existing = await _unitOfWork.StudentCredentialRepository.GetByLoginCodeAsync(candidate);
          if (existing != null)
            continue;

          loginCode = candidate;
          existingCodes.Add(loginCode);
          break;
        }

        var newCred = new StudentCredential
        {
          UserId = member.UserId,
          ClassroomId = classroom.Id,
          StudentLoginCode = loginCode,
          VisualPasswordHash = "DEFAULT",
          IsActive = true,
          FailedAttempts = 0
        };

        await _unitOfWork.StudentCredentialRepository.CreateAsync(newCred);
        credentialByUserId[member.UserId] = newCred;
      }
      await _unitOfWork.CommitAsync();
    }

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
