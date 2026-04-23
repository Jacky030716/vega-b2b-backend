using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetAdminUserDetails;

internal sealed class GetAdminUserDetailsQueryHandler(
    IInstitutionUserReportRepository userReportRepository)
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

        var detail = await userReportRepository.GetUserDetailsAsync(
            request.InstitutionId <= 0 ? 1 : request.InstitutionId,
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
