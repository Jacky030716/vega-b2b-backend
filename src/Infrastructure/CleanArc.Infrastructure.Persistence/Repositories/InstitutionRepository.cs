using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class InstitutionRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Institution>(dbContext), IInstitutionRepository
{
    public async Task<Institution> GetInstitutionWithStatsAsync(int id)
    {
        return await DbContext.Institutions.AsNoTracking()
            .Include(i => i.UserMemberships.Where(m => m.IsActive))
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<InstitutionUser?> GetPrimaryInstitutionForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await DbContext.InstitutionUsers
            .AsNoTracking()
            .Include(m => m.Institution)
            .Include(m => m.User)
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is not null)
        {
            return membership;
        }

        var legacyUser = await DbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.InstitutionId.HasValue)
            .Select(u => new { u.Id, InstitutionId = u.InstitutionId!.Value })
            .FirstOrDefaultAsync(cancellationToken);

        if (legacyUser is null)
        {
            return null;
        }

        var institution = await DbContext.Institutions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == legacyUser.InstitutionId, cancellationToken);

        return institution is null
            ? null
            : new InstitutionUser
            {
                InstitutionId = institution.Id,
                Institution = institution,
                UserId = legacyUser.Id,
                AccessScope = "Member access",
                IsPrimary = true,
                IsActive = true,
            };
    }

    public async Task<IReadOnlyList<InstitutionUser>> GetActiveUsersForInstitutionAsync(
        int institutionId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.InstitutionUsers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.InstitutionId == institutionId && m.IsActive)
            .OrderBy(m => m.User.UserName)
            .ToListAsync(cancellationToken);
    }

    public async Task<InstitutionUser> AssignUserToInstitutionAsync(
        int institutionId,
        int userId,
        string accessScope,
        bool isPrimary = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(accessScope)
            ? "Member access"
            : accessScope.Trim();

        var existing = await DbContext.InstitutionUsers
            .FirstOrDefaultAsync(
                m => m.InstitutionId == institutionId && m.UserId == userId && m.IsActive,
                cancellationToken);

        if (isPrimary)
        {
            var otherPrimaryMemberships = await DbContext.InstitutionUsers
                .Where(m => m.UserId == userId && m.IsActive && m.IsPrimary && m.InstitutionId != institutionId)
                .ToListAsync(cancellationToken);

            foreach (var membership in otherPrimaryMemberships)
            {
                membership.IsPrimary = false;
            }
        }

        if (existing is null)
        {
            existing = new InstitutionUser
            {
                InstitutionId = institutionId,
                UserId = userId,
                AccessScope = normalizedScope,
                IsPrimary = isPrimary,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
            };
            DbContext.InstitutionUsers.Add(existing);
        }
        else
        {
            existing.AccessScope = normalizedScope;
            existing.IsPrimary = isPrimary || existing.IsPrimary;
            existing.IsActive = true;
            existing.LeftAt = null;
        }

        if (isPrimary)
        {
            var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is not null)
            {
                user.InstitutionId = institutionId;
            }
        }

        await DbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> IsActiveInstitutionUserAsync(
        int institutionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.InstitutionUsers
            .AsNoTracking()
            .AnyAsync(
                m => m.InstitutionId == institutionId && m.UserId == userId && m.IsActive,
                cancellationToken);
    }

    public async Task UpdateSubscriptionAsync(
        int institutionId,
        string subscriptionTier,
        DateTime renewalDate,
        CancellationToken cancellationToken = default)
    {
        var institution = await DbContext.Institutions
            .FirstOrDefaultAsync(x => x.Id == institutionId, cancellationToken);
        if (institution is null)
            return;

        institution.SubscriptionTier = subscriptionTier;
        institution.RenewalDate = renewalDate;
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
