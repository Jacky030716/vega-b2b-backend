using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.AskAuditor;

public class AskAuditorQuery : IRequest<OperationResult<AskAuditorResult>>
{
    public int InstitutionId { get; set; }
    public string Question { get; set; }
    public int? UserId { get; set; }
}

public class AskAuditorResult
{
    public string Answer { get; set; }
    public IReadOnlyList<int> MatchedUserIds { get; set; } = [];
}
