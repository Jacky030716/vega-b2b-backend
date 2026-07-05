using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal class JoinClassroomCommandHandler : IRequestHandler<JoinClassroomCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAppUserManager _userManager;

  public JoinClassroomCommandHandler(IUnitOfWork unitOfWork, IAppUserManager userManager)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
  }

  public async ValueTask<OperationResult<int>> Handle(JoinClassroomCommand request, CancellationToken cancellationToken)
  {
    var normalizedJoinCode = request.JoinCode?.Trim().ToUpperInvariant() ?? string.Empty;

    var user = await _userManager.GetUserByIdAsync(request.UserId);
    if (user is null)
      return OperationResult<int>.UnauthorizedResult("Authenticated user was not found");

    var roles = await _userManager.GetUserRolesAsync(user);
    if (!roles.Any(role => string.Equals(role, "student", StringComparison.OrdinalIgnoreCase)))
      return OperationResult<int>.ForbiddenResult("Only students can join classrooms using a join code.");

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

    string loginCode;
    while (true)
    {
      var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
      var existingCred = await _unitOfWork.StudentCredentialRepository.GetByLoginCodeAsync(candidate);
      if (existingCred == null)
      {
        loginCode = candidate;
        break;
      }
    }

    await _unitOfWork.StudentCredentialRepository.CreateAsync(new StudentCredential
    {
      UserId = request.UserId,
      ClassroomId = classroom.Id,
      StudentLoginCode = loginCode,
      VisualPasswordHash = "DEFAULT",
      IsActive = true,
      FailedAttempts = 0
    });

    await _unitOfWork.CommitAsync();

    // Automatically link student to teacher's institution
    var teacher = await _userManager.GetUserByIdAsync(classroom.TeacherId);
    if (teacher is not null && teacher.InstitutionId.HasValue)
    {
        var studentUser = await _userManager.GetUserByIdAsync(request.UserId);
        if (studentUser is not null)
        {
            studentUser.InstitutionId = teacher.InstitutionId;
            await _userManager.UpdateUserAsync(studentUser);
        }

        await _unitOfWork.InstitutionRepository.AssignUserToInstitutionAsync(
            teacher.InstitutionId.Value,
            request.UserId,
            "Student access",
            isPrimary: true,
            cancellationToken);
    }

    return OperationResult<int>.SuccessResult(classroom.Id);
  }
}
