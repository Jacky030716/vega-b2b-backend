using CleanArc.Domain.Entities.Institution;

namespace CleanArc.Application.Contracts.Persistence;

public interface IInstitutionRepository
{
    Task<Institution> GetInstitutionWithStatsAsync(int id);
    Task<InstitutionUser?> GetPrimaryInstitutionForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstitutionUser>> GetActiveUsersForInstitutionAsync(int institutionId, CancellationToken cancellationToken = default);
    Task<InstitutionUser> AssignUserToInstitutionAsync(
        int institutionId,
        int userId,
        string accessScope,
        bool isPrimary = true,
        CancellationToken cancellationToken = default);
    Task<bool> IsActiveInstitutionUserAsync(int institutionId, int userId, CancellationToken cancellationToken = default);
    Task UpdateSubscriptionAsync(
        int institutionId,
        string subscriptionTier,
        DateTime renewalDate,
        CancellationToken cancellationToken = default);
}
