using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Admin.Commands.CreateAdminTeacher;

public sealed record CreateAdminTeacherCommand(
    int InstitutionId,
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string TemporaryPassword,
    int UserId = 0) : IRequest<OperationResult<CreateAdminTeacherResult>>;

public sealed record CreateAdminTeacherResult(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string Role,
    bool IsActive);

internal sealed class CreateAdminTeacherCommandHandler(
    IAppUserManager userManager,
    IUnitOfWork unitOfWork,
    ILogger<CreateAdminTeacherCommandHandler> logger)
    : IRequestHandler<CreateAdminTeacherCommand, OperationResult<CreateAdminTeacherResult>>
{
    public async ValueTask<OperationResult<CreateAdminTeacherResult>> Handle(
        CreateAdminTeacherCommand request,
        CancellationToken cancellationToken)
    {
        // ponytail: derive institution strictly from logged-in admin user to prevent multi-institution bypass
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<CreateAdminTeacherResult>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institutionId = membership.InstitutionId;

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.TemporaryPassword))
        {
            return OperationResult<CreateAdminTeacherResult>.FailureResult(
                "Teacher details, username, and temporary password are required.");
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await userManager.IsExistUserName(userName))
        {
            return OperationResult<CreateAdminTeacherResult>.FailureResult("Username already exists.");
        }

        if (await userManager.FindUserByEmail(email) is not null)
        {
            return OperationResult<CreateAdminTeacherResult>.FailureResult("Email already exists.");
        }

        var institution = await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(institutionId);
        if (institution is null)
        {
            return OperationResult<CreateAdminTeacherResult>.NotFoundResult("Institution not found.");
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            InstitutionId = institutionId,
            Name = request.FirstName.Trim(),
            FamilyName = request.LastName.Trim(),
        };

        var createResult = await userManager.CreateUserWithPasswordAsync(user, request.TemporaryPassword);
        if (!createResult.Succeeded)
        {
            var message = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create teacher.";
            return OperationResult<CreateAdminTeacherResult>.FailureResult(message);
        }

        var roleResult = await userManager.AddUserToRoleAsync(user, "teacher");
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign the teacher role to new user {UserId}.", user.Id);
            return OperationResult<CreateAdminTeacherResult>.FailureResult("Failed to assign teacher access.");
        }

        await unitOfWork.InstitutionRepository.AssignUserToInstitutionAsync(
            institutionId,
            user.Id,
            "Teacher access",
            isPrimary: true,
            cancellationToken);

        logger.LogInformation(
            "Institution {InstitutionId} created teacher user {UserId}.",
            institutionId,
            user.Id);

        return OperationResult<CreateAdminTeacherResult>.SuccessResult(new CreateAdminTeacherResult(
            user.Id,
            user.Name,
            user.FamilyName,
            user.Email,
            user.UserName,
            "teacher",
            true));
    }
}
