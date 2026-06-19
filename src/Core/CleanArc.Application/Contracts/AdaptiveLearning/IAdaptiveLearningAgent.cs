using System.Threading;
using System.Threading.Tasks;

namespace CleanArc.Application.Contracts.AdaptiveLearning;

public interface IAdaptiveLearningAgent
{
    Task EvaluateAndTriggerDraftAsync(int studentId, int triggeringAttemptId, bool isSpellingTest, CancellationToken cancellationToken);
}
