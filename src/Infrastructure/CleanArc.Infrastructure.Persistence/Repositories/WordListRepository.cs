using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Word;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories
{
    internal class WordListRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<WordList>(dbContext), IWordListRepository
    {
        public async Task CreateWordListAsync(WordList wordList)
        {
            await base.AddAsync(wordList);
        }

        public async Task<List<WordList>> GetUserWordListsAsync(int userId)
        {
            return await base.TableNoTracking.Include(wl => wl.Words).Where(wl => wl.UserId == userId).ToListAsync();
        }

        public async Task<WordList> GetWordListByIdAndUserIdAsync(int wordListId, int userId)
        {
            var wordlist = await base.TableNoTracking
                .Include(wl => wl.Words)
                .FirstOrDefaultAsync(wl => wl.Id == wordListId && wl.UserId == userId);

            if (wordlist is not null)
                base.DbContext.Attach(wordlist);

            return wordlist;
        }

        public async Task DeleteUserWordListAsync(int wordListId, int userId)
        {
            await UpdateAsync(wl => wl.Id == wordListId && wl.UserId == userId, wl => wl.SetProperty(wl => wl.IsDeleted, true));
        }
    }
}
