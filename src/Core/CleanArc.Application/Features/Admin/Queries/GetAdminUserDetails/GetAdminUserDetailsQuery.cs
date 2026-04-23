using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetAdminUserDetails;

public class GetAdminUserDetailsQuery : IRequest<OperationResult<GetAdminUserDetailsResult>>
{
    public int InstitutionId { get; set; } = 1;
    public int UserId { get; set; }
}

public class GetAdminUserDetailsResult
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalXp { get; set; }
    public int TotalStars { get; set; }
    public IReadOnlyList<AdminUserClassroomDto> Classrooms { get; set; } = [];
}

public class AdminUserClassroomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
