using CleanArc.Domain.Entities.Institution;

namespace CleanArc.Application.Contracts.Persistence;

public interface IInstitutionRepository
{
    Task<Institution> GetInstitutionWithStatsAsync(int id);
}
