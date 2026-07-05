using System.Security.Cryptography;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

internal sealed class AddClassroomStudentCommandHandler(
    IUnitOfWork unitOfWork,
    IAppUserManager userManager)
    : IRequestHandler<AddClassroomStudentCommand, OperationResult<int>>
{
    public async ValueTask<OperationResult<int>> Handle(
        AddClassroomStudentCommand request,
        CancellationToken cancellationToken)
    {
        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
        if (classroom is null)
        {
            return OperationResult<int>.NotFoundResult("Classroom not found");
        }

        if (!request.IsAdmin && classroom.TeacherId != request.RequestingUserId)
        {
            return OperationResult<int>.UnauthorizedResult("You do not manage this classroom");
        }

        var student = await userManager.GetUserByIdAsync(request.StudentId);
        if (student is null)
        {
            return OperationResult<int>.NotFoundResult("Student not found");
        }

        var roles = await userManager.GetUserRolesAsync(student);
        if (!roles.Any(role => string.Equals(role, "student", StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<int>.ForbiddenResult("Only students can be added to classrooms.");
        }

        var existing = await unitOfWork.ClassroomRepository.GetClassroomStudentAsync(request.ClassroomId, request.StudentId);
        if (existing is not null)
        {
            return OperationResult<int>.FailureResult("Student is already in this classroom");
        }

        await unitOfWork.ClassroomRepository.JoinClassroomAsync(new ClassroomStudent
        {
            ClassroomId = request.ClassroomId,
            UserId = request.StudentId,
            JoinedDate = DateTime.UtcNow
        });

        var loginCode = await GenerateUniqueLoginCodeAsync(unitOfWork.StudentCredentialRepository, cancellationToken);
        await unitOfWork.StudentCredentialRepository.CreateAsync(new StudentCredential
        {
            UserId = request.StudentId,
            ClassroomId = request.ClassroomId,
            StudentLoginCode = loginCode,
            VisualPasswordHash = "DEFAULT",
            IsActive = true,
            FailedAttempts = 0
        });

        await unitOfWork.CommitAsync();

        return OperationResult<int>.SuccessResult(request.ClassroomId);
    }

    private static async Task<string> GenerateUniqueLoginCodeAsync(
        IStudentCredentialRepository credentialRepository,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            var existingCred = await credentialRepository.GetByLoginCodeAsync(candidate);
            if (existingCred is null)
            {
                return candidate;
            }
        }
    }
}
