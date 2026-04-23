using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Commands.UpdateAdminUser;

internal sealed class UpdateAdminUserCommandHandler(
    IInstitutionUserReportRepository userReportRepository,
    IAppUserManager userManager)
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
