using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetInstitutionStats;

public class GetInstitutionStatsQuery : IRequest<OperationResult<InstitutionStatsDto>>
{
    public int InstitutionId { get; set; }
}

public class InstitutionStatsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MaxSeats { get; set; }
    public int SeatsUsed { get; set; }
    public string SubscriptionTier { get; set; }
    public DateTime RenewalDate { get; set; }
}
