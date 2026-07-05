using System.Linq;
using System.Security.Cryptography;
using CleanArc.Application.Common;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
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

        // ponytail: derive institution strictly from logged-in admin user to prevent multi-institution bypass
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.AdminUserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<UpdateAdminUserResult>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institutionId = membership.InstitutionId;

        var isAllowedUser = await userReportRepository.IsInstitutionTeacherOrStudentUserAsync(
            institutionId,
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
                var classrooms = await unitOfWork.ClassroomRepository.GetStudentClassroomsAsync(user.Id);
                var normalizedVisualSequence = NormalizeVisualSequenceToIds(request.PicturePassword);

                foreach (var classroom in classrooms)
                {
                    var existingCred = credentials.FirstOrDefault(c => c.ClassroomId == classroom.Id);
                    if (existingCred != null)
                    {
                        existingCred.VisualPasswordHash = VisualPasswordHelper.HashPassword(
                            normalizedVisualSequence,
                            existingCred.StudentLoginCode
                        );
                        await unitOfWork.StudentCredentialRepository.UpdateAsync(existingCred);
                    }
                    else
                    {
                        var loginCode = await GenerateUniqueLoginCodeAsync(unitOfWork.StudentCredentialRepository);
                        await unitOfWork.StudentCredentialRepository.CreateAsync(new StudentCredential
                        {
                            UserId = user.Id,
                            ClassroomId = classroom.Id,
                            StudentLoginCode = loginCode,
                            VisualPasswordHash = VisualPasswordHelper.HashPassword(normalizedVisualSequence, loginCode),
                            IsActive = true,
                            FailedAttempts = 0
                        });
                    }
                }
                await unitOfWork.CommitAsync();
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

    private static async Task<string> GenerateUniqueLoginCodeAsync(IStudentCredentialRepository repository)
    {
        while (true)
        {
            var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            var existing = await repository.GetByLoginCodeAsync(candidate);
            if (existing == null)
            {
                return candidate;
            }
        }
    }

    private static string NormalizeVisualSequenceToIds(string picturePassword)
    {
        if (string.IsNullOrWhiteSpace(picturePassword))
            return picturePassword;

        var parts = picturePassword.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var translatedParts = parts.Select(part =>
        {
            var trimmed = part.Trim().ToLowerInvariant();
            if (trimmed.StartsWith("icon_") && int.TryParse(trimmed.Substring(5), out var num))
            {
                return num.ToString();
            }
            return trimmed;
        });

        return string.Join("-", translatedParts);
    }
}
