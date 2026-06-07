using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CleanArc.Application.Contracts.AI;

public interface IVegaAgentService
{
    Task<List<VegaRecommendationDto>> GetInboxRecommendationsAsync(int classroomId, int teacherId, CancellationToken cancellationToken);
    Task<bool> ApproveRecommendationAsync(int classroomId, string recommendationId, int teacherId, object? payload, CancellationToken cancellationToken);
}
