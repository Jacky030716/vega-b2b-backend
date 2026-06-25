using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetAdminUserDetails;

internal sealed class GetAdminUserDetailsQueryHandler(
    IInstitutionUserReportRepository userReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminUserDetailsQuery, OperationResult<GetAdminUserDetailsResult>>
{
    public async ValueTask<OperationResult<GetAdminUserDetailsResult>> Handle(
        GetAdminUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return OperationResult<GetAdminUserDetailsResult>.FailureResult("Invalid user id.");
        }

        // ponytail: derive institution strictly from logged-in admin user to prevent multi-institution bypass
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.AdminUserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<GetAdminUserDetailsResult>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institutionId = membership.InstitutionId;

        var detail = await userReportRepository.GetUserDetailsAsync(
            institutionId,
            request.UserId,
            cancellationToken);

        if (detail is null)
        {
            return OperationResult<GetAdminUserDetailsResult>.NotFoundResult("User not found.");
        }

        return OperationResult<GetAdminUserDetailsResult>.SuccessResult(new GetAdminUserDetailsResult
        {
            Id = detail.Id,
            FirstName = detail.FirstName,
            LastName = detail.LastName,
            UserName = detail.UserName,
            Email = detail.Email,
            Role = detail.Role,
            IsActive = detail.IsActive,
            LastLoginAt = detail.LastLoginAt,
            TotalXp = detail.TotalXp,
            TotalStars = detail.TotalStars,
            Classrooms = detail.Classrooms
                .Select(x => new AdminUserClassroomDto { Id = x.Id, Name = x.Name })
                .ToList()
        });
    }
}
