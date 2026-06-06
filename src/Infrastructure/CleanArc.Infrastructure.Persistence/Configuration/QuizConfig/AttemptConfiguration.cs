using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.QuizConfig;

public class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        // Covers GetPriorCompletedAttemptForChallengeAsync and GetUserBestAttemptsForGameAsync
        // which both filter on (UserId, ChallengeId) and check IsCompleted.
        builder.HasIndex(a => new { a.UserId, a.ChallengeId, a.IsCompleted });

        // Used by GetUserBestAttemptsForGameAsync for full user history per challenge.
        builder.HasIndex(a => new { a.UserId, a.ChallengeId });
    }
}
