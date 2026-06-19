using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArc.Application.Contracts.AdaptiveLearning;

public interface IChallengeGenerator
{
    string GameType { get; }
    Task<string> GenerateContentJsonAsync(IReadOnlyList<string> words, int difficulty, CancellationToken cancellationToken);
}
