using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Queries
{
    public record GetUserWordListsQuery(int UserId) : IRequest<OperationResult<List<GetUserWordListsResult>>>;
}
