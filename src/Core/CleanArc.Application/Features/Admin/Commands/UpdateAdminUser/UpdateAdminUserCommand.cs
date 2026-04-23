using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Commands.UpdateAdminUser;

public class UpdateAdminUserCommand : IRequest<OperationResult<UpdateAdminUserResult>>
{
    public int InstitutionId { get; set; } = 1;
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UpdateAdminUserResult
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
