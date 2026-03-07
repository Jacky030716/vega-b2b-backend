using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Queries;

public record GetUserProgressQuery(int UserId) : IRequest<OperationResult<UserProgressDto>>;

public record UserProgressDto(int TotalXP, int CurrentLevel, string LevelName, int TotalQuizzesTaken, int TotalCorrectAnswers, int TotalTimePlayed, int? NextLevelXP);
