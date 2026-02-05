using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Features.Quizzes.Models;

namespace CleanArc.Application.Contracts.Persistence;

public interface IQuizContentRepository
{
  Task<IReadOnlyList<QuizSummaryDto>> GetQuizzesAsync(string type, string gameType);
  Task<QuizDetailDto> GetQuizByIdAsync(string quizId);
  Task<IReadOnlyList<object>> GetQuizQuestionsAsync(string quizId);
  Task<string> GetQuestionTypeAsync(string quizId, string questionId);
  Task<IReadOnlyList<GameCatalogItemDto>> GetGameCatalogAsync();
  Task<GameConfigDto> GetGameConfigAsync(string gameType);
}
