using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Queries
{
    internal class GetUserWordListsHandler : IRequestHandler<GetUserWordListsQuery, OperationResult<List<GetUserWordListsResult>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserWordListsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async ValueTask<OperationResult<List<GetUserWordListsResult>>> Handle(GetUserWordListsQuery request, CancellationToken token)
        {
            var wordLists = await _unitOfWork.WordListRepository.GetUserWordListsAsync(request.UserId);

            if (!wordLists.Any())
            {
                return OperationResult<List<GetUserWordListsResult>>.NotFoundResult("You Don't Have Any Word Lists");
            }

            var result = wordLists.Select(c => new GetUserWordListsResult(c.Id, c.Title));

            return OperationResult<List<GetUserWordListsResult>>.SuccessResult(result.ToList());
        }
    }
}
