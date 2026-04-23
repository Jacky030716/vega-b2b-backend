using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Queries.GetInstitutionStats;

internal class GetInstitutionStatsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetInstitutionStatsQuery, OperationResult<InstitutionStatsDto>>
{
    public async ValueTask<OperationResult<InstitutionStatsDto>> Handle(GetInstitutionStatsQuery request, CancellationToken cancellationToken)
    {
        var institution = await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(request.InstitutionId);

        if (institution == null)
        {
            return OperationResult<InstitutionStatsDto>.NotFoundResult("Institution not found");
        }

        // Dynamically update seats used based on actual attached users, 
        // or just use the pre-calculated DB value. Here we sync them.
        var actualSeatsUsed = institution.Users.Count;
        if (actualSeatsUsed != institution.SeatsUsed)
        {
            institution.SeatsUsed = actualSeatsUsed;
            // In a real app we might save this back, but for now we just return accurate stats.
        }

        var dto = new InstitutionStatsDto
        {
            Id = institution.Id,
            Name = institution.Name ?? "Vega Institution",
            MaxSeats = institution.MaxSeats,
            SeatsUsed = actualSeatsUsed,
            SubscriptionTier = institution.SubscriptionTier,
            RenewalDate = institution.RenewalDate
        };

        return OperationResult<InstitutionStatsDto>.SuccessResult(dto);
    }
}
