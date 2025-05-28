using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands;

public class UpdateWordListCommandHandler : IRequestHandler<UpdateWordListCommand, OperationResult<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWordListCommandHandler(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    public async ValueTask<OperationResult<bool>> Handle(UpdateWordListCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var wordListId = request.WordListId;

        var wordlist = await _unitOfWork.WordListRepository.GetWordListByIdAndUserIdAsync(wordListId, userId);

        if (wordlist is null)
        {
            return OperationResult<bool>.NotFoundResult("Specified WordList not found");
        }

        wordlist.Title = request.Title;

        // Get the existing words within the word list
        var existingWords = wordlist.Words.ToList();

        foreach (var wordDto in request.Words)
        {
            var existingWord = existingWords.FirstOrDefault(w => w.Text.ToLower() == wordDto.Text.ToLower());

            if (existingWord != null)
            {
                existingWord.Text = wordDto.Text;
                existingWord.ImageUrl = wordDto.ImageUrl ?? existingWord.ImageUrl;
            }

            else
            {
                wordlist.Words.Add(new Domain.Entities.Word.Word()
                {
                    Text = wordDto.Text,
                    ImageUrl = wordDto.ImageUrl,
                });
            }
        }

        // Remove words that are not in the request (soft delete)
        var requestTexts = request.Words.Select(w => w.Text.ToLower()).ToList();

        foreach (var existingWord in existingWords)
        {
            if (!requestTexts.Contains(existingWord.Text.ToLower()))
            {
                existingWord.IsDeleted = true;
            }
        }

        await _unitOfWork.CommitAsync();

        return OperationResult<bool>.SuccessResult(true);
    }
}
