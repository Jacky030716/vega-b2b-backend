using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Admin.Commands.CreateAdminStudent;

public sealed record CreateAdminStudentCommand(
    int InstitutionId,
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string TemporaryPassword) : IRequest<OperationResult<CreateAdminStudentResult>>;

public sealed record CreateAdminStudentResult(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string Role,
    bool IsActive);

internal sealed class CreateAdminStudentCommandHandler(
    IAppUserManager userManager,
    IUnitOfWork unitOfWork,
    ILogger<CreateAdminStudentCommandHandler> logger)
    : IRequestHandler<CreateAdminStudentCommand, OperationResult<CreateAdminStudentResult>>
{
    public async ValueTask<OperationResult<CreateAdminStudentResult>> Handle(
        CreateAdminStudentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.InstitutionId <= 0 ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.TemporaryPassword))
        {
            return OperationResult<CreateAdminStudentResult>.FailureResult(
                "Institution, student details, username, and temporary password are required.");
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await userManager.IsExistUserName(userName))
        {
            return OperationResult<CreateAdminStudentResult>.FailureResult("Username already exists.");
        }

        if (await userManager.FindUserByEmail(email) is not null)
        {
            return OperationResult<CreateAdminStudentResult>.FailureResult("Email already exists.");
        }

        var institution = await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(request.InstitutionId);
        if (institution is null)
        {
            return OperationResult<CreateAdminStudentResult>.NotFoundResult("Institution not found.");
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            InstitutionId = request.InstitutionId,
            Name = request.FirstName.Trim(),
            FamilyName = request.LastName.Trim(),
        };

        var createResult = await userManager.CreateUserWithPasswordAsync(user, request.TemporaryPassword);
        if (!createResult.Succeeded)
        {
            var message = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create student.";
            return OperationResult<CreateAdminStudentResult>.FailureResult(message);
        }

        var roleResult = await userManager.AddUserToRoleAsync(user, "student");
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign the student role to new user {UserId}.", user.Id);
            return OperationResult<CreateAdminStudentResult>.FailureResult("Failed to assign student access.");
        }

        await unitOfWork.InstitutionRepository.AssignUserToInstitutionAsync(
            request.InstitutionId,
            user.Id,
            "Student access",
            isPrimary: true,
            cancellationToken);

        logger.LogInformation(
            "Institution {InstitutionId} created student user {UserId}.",
            request.InstitutionId,
            user.Id);

        return OperationResult<CreateAdminStudentResult>.SuccessResult(new CreateAdminStudentResult(
            user.Id,
            user.Name,
            user.FamilyName,
            user.Email,
            user.UserName,
            "student",
            true));
    }
}
