using CleanArc.Application.Contracts.DTOs.Word;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Queries;

internal class GetOneWordListHandler : IRequestHandler<GetOneWordListQuery, OperationResult<GetOneWordListResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOneWordListHandler(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    public async ValueTask<OperationResult<GetOneWordListResult>> Handle(GetOneWordListQuery request, CancellationToken token)
    {
        var wordlist = await _unitOfWork.WordListRepository.GetWordListByIdAndUserIdAsync(request.wordlistId, request.userId);

        if (wordlist is null)
        {
            return OperationResult<GetOneWordListResult>.NotFoundResult("Wordlist does not exist");
        }

        var result = new GetOneWordListResult(
            WordlistId: wordlist.Id,
            WordlistTitle: wordlist.Title,
            Words: wordlist.Words.Select(w => new CreateWordDto(w.Text, w.ImageUrl)).ToList()
        );

        return OperationResult<GetOneWordListResult>.SuccessResult(result);
    }
}
