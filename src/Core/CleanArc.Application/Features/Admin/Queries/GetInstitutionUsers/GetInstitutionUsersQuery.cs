using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;

public class GetInstitutionUsersQuery : IRequest<OperationResult<GetInstitutionUsersResult>>
{
    public int InstitutionId { get; set; } = 1;
    public string Role { get; set; } = "all";
    public string Tab { get; set; } = "all";
    public string? Search { get; set; }
}

public class GetInstitutionUsersResult
{
    public IReadOnlyList<InstitutionUserSummaryDto> Users { get; set; } = [];
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int UnassignedCount { get; set; }
}

public class InstitutionUserSummaryDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ClassName { get; set; }
    public bool HasLoggedIn { get; set; }
    public string CredentialHint { get; set; } = string.Empty;
}
