using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Admin.Commands.DeleteAdminUser;

public sealed record DeleteAdminUserCommand(
    int UserId,
    int TargetUserId) : IRequest<OperationResult<bool>>;

internal sealed class DeleteAdminUserCommandHandler(
    IAppUserManager userManager,
    IInstitutionUserReportRepository userReportRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteAdminUserCommandHandler> logger)
    : IRequestHandler<DeleteAdminUserCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(
        DeleteAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TargetUserId <= 0)
        {
            return OperationResult<bool>.FailureResult("Invalid user id.");
        }

        // ponytail: derive institution strictly from logged-in admin user to prevent multi-institution bypass
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<bool>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        // Ensure the target user actually belongs to this institution and is a teacher or student
        var isAllowedUser = await userReportRepository.IsInstitutionTeacherOrStudentUserAsync(
            membership.InstitutionId,
            request.TargetUserId,
            cancellationToken);

        if (!isAllowedUser)
        {
            return OperationResult<bool>.NotFoundResult("User not found under this institution.");
        }

        var targetUser = await userManager.GetUserById(request.TargetUserId);
        if (targetUser is null)
        {
            return OperationResult<bool>.NotFoundResult("User not found.");
        }

        var deleteResult = await userManager.DeleteUserAsync(targetUser);
        if (!deleteResult.Succeeded)
        {
            var message = deleteResult.Errors.FirstOrDefault()?.Description ?? "Failed to delete user.";
            return OperationResult<bool>.FailureResult(message);
        }

        logger.LogInformation("Admin {AdminUserId} hard-deleted user {DeletedUserId}.", request.UserId, request.TargetUserId);

        return OperationResult<bool>.SuccessResult(true);
    }
}
