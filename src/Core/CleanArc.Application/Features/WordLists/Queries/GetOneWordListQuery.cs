using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Queries;

public record GetOneWordListQuery(int wordlistId, int userId): IRequest<OperationResult<GetOneWordListResult>>
{
}
