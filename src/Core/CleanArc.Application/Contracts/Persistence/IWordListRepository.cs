using CleanArc.Domain.Entities.Order;
using CleanArc.Domain.Entities.Word;

namespace CleanArc.Application.Contracts.Persistence
{
    public interface IWordListRepository
    {
        Task<List<WordList>> GetUserWordListsAsync(int userId);
        Task CreateWordListAsync(WordList wordList);
        Task<WordList> GetWordListByIdAndUserIdAsync(int wordListId, int userId);
        Task DeleteUserWordListAsync(int wordListId, int userId);

    }
}
