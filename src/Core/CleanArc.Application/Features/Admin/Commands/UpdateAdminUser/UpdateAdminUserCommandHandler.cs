using System.Linq;
using CleanArc.Application.Common;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Commands.UpdateAdminUser;

internal sealed class UpdateAdminUserCommandHandler(
    IInstitutionUserReportRepository userReportRepository,
    IAppUserManager userManager,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAdminUserCommand, OperationResult<UpdateAdminUserResult>>
{
    public async ValueTask<OperationResult<UpdateAdminUserResult>> Handle(
        UpdateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return OperationResult<UpdateAdminUserResult>.FailureResult("Invalid user id.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return OperationResult<UpdateAdminUserResult>.FailureResult("First name, last name, and email are required.");
        }

        var isAllowedUser = await userReportRepository.IsInstitutionTeacherOrStudentUserAsync(
            request.InstitutionId <= 0 ? 1 : request.InstitutionId,
            request.UserId,
            cancellationToken);

        if (!isAllowedUser)
        {
            return OperationResult<UpdateAdminUserResult>.NotFoundResult("User not found.");
        }

        var user = await userManager.GetUserById(request.UserId);
        if (user is null)
        {
            return OperationResult<UpdateAdminUserResult>.NotFoundResult("User not found.");
        }

        user.Name = request.FirstName.Trim();
        user.FamilyName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();

        if (request.IsActive)
        {
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
        }
        else
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        }

        var result = await userManager.UpdateUser(user);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Description ?? "Failed to update user.";
            return OperationResult<UpdateAdminUserResult>.FailureResult(message);
        }

        // Handle password / visual password updates based on role
        var roles = await userManager.GetUserRolesAsync(user);
        var isStudent = roles.Any(r => r.Equals("student", StringComparison.OrdinalIgnoreCase));

        if (isStudent)
        {
            if (!string.IsNullOrWhiteSpace(request.PicturePassword))
            {
                if (!VisualPasswordHelper.IsValidVisualPassword(request.PicturePassword))
                {
                    return OperationResult<UpdateAdminUserResult>.FailureResult(
                        "Invalid picture password format. Must be three valid icons in format 'icon_xx-icon_xx-icon_xx'."
                    );
                }

                var credentials = await unitOfWork.StudentCredentialRepository.GetByUserIdAsync(user.Id);
                foreach (var credential in credentials)
                {
                    credential.VisualPasswordHash = VisualPasswordHelper.HashPassword(
                        request.PicturePassword,
                        credential.StudentLoginCode
                    );
                    await unitOfWork.StudentCredentialRepository.UpdateAsync(credential);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var token = await userManager.GeneratePasswordResetToken(user);
                var resetResult = await userManager.ResetPassword(user, token, request.Password);
                if (!resetResult.Succeeded)
                {
                    var message = resetResult.Errors.FirstOrDefault()?.Description ?? "Failed to update standard password.";
                    return OperationResult<UpdateAdminUserResult>.FailureResult(message);
                }
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.PicturePassword))
            {
                return OperationResult<UpdateAdminUserResult>.FailureResult("Teachers and admins do not support picture passwords.");
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var token = await userManager.GeneratePasswordResetToken(user);
                var resetResult = await userManager.ResetPassword(user, token, request.Password);
                if (!resetResult.Succeeded)
                {
                    var message = resetResult.Errors.FirstOrDefault()?.Description ?? "Failed to update password.";
                    return OperationResult<UpdateAdminUserResult>.FailureResult(message);
                }
            }
        }

        return OperationResult<UpdateAdminUserResult>.SuccessResult(new UpdateAdminUserResult
        {
            Id = user.Id,
            FirstName = user.Name ?? string.Empty,
            LastName = user.FamilyName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsActive = request.IsActive
        });
    }
}
