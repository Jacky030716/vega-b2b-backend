using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;

internal sealed class GetInstitutionUsersQueryHandler(
    IInstitutionUserReportRepository userReportRepository)
    : IRequestHandler<GetInstitutionUsersQuery, OperationResult<GetInstitutionUsersResult>>
{
    public async ValueTask<OperationResult<GetInstitutionUsersResult>> Handle(
        GetInstitutionUsersQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedRole = NormalizeFilter(request.Role, "all", ["all", "student", "teacher"]);
        var normalizedTab = NormalizeFilter(request.Tab, "all", ["all", "unassigned", "inactive"]);

        var rows = await userReportRepository.GetUsersAsync(
            new InstitutionUserReportFilter(
                InstitutionId: request.InstitutionId <= 0 ? 1 : request.InstitutionId,
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
