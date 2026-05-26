using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

public sealed record GenerateSmartFeedbackCommand(
    string GameName,
    int Score,
    int StarsEarned,
    decimal? Accuracy,
    int? UserId) : IRequest<OperationResult<SmartFeedbackResult>>;

public sealed record SmartFeedbackResult(string Feedback);
