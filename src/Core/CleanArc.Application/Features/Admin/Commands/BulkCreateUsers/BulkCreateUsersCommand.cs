using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Commands.BulkCreateUsers;

public class BulkCreateUsersCommand : IRequest<OperationResult<BulkCreateUsersResult>>
{
    public int Count { get; set; }
    public string Role { get; set; } // "student" or "teacher"
    public int InstitutionId { get; set; }
}

public class BulkCreateUsersResult
{
    public string CsvContent { get; set; }
    public int GeneratedCount { get; set; }
}
