using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;

internal sealed class GetInstitutionUsersQueryHandler(
    IInstitutionUserReportRepository userReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetInstitutionUsersQuery, OperationResult<GetInstitutionUsersResult>>
{
    public async ValueTask<OperationResult<GetInstitutionUsersResult>> Handle(
        GetInstitutionUsersQuery request,
        CancellationToken cancellationToken)
    {
        // ponytail: derive institution strictly from logged-in admin user to prevent multi-institution bypass
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<GetInstitutionUsersResult>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institutionId = membership.InstitutionId;

        var normalizedRole = NormalizeFilter(request.Role, "all", ["all", "student", "teacher"]);
        var normalizedTab = NormalizeFilter(request.Tab, "all", ["all", "unassigned", "inactive"]);

        var rows = await userReportRepository.GetUsersAsync(
            new InstitutionUserReportFilter(
                InstitutionId: institutionId,
                Role: normalizedRole,
                Tab: normalizedTab,
                Search: request.Search),
            cancellationToken);

        var dtoRows = rows
            .Select(row => new InstitutionUserSummaryDto
            {
                Id = row.Id,
                FirstName = row.FirstName,
                LastName = row.LastName,
                UserName = row.UserName,
                Email = row.Email,
                Role = row.Role,
                IsActive = row.IsActive,
                LastLoginAt = row.LastLoginAt,
                ClassName = row.ClassName,
                HasLoggedIn = row.HasLoggedIn,
                CredentialHint = row.CredentialHint
            })
            .ToList();

        return OperationResult<GetInstitutionUsersResult>.SuccessResult(new GetInstitutionUsersResult
        {
            Users = dtoRows,
            TotalCount = dtoRows.Count,
            ActiveCount = dtoRows.Count(x => x.IsActive),
            InactiveCount = dtoRows.Count(x => !x.IsActive),
            UnassignedCount = rows.Count(x => x.IsUnassigned)
        });
    }

    private static string NormalizeFilter(string? value, string fallback, IReadOnlyCollection<string> allowed)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();

        return allowed.Contains(normalized) ? normalized : fallback;
    }
}
