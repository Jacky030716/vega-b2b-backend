using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class MagicBackpackAttemptAnswerConfiguration : IEntityTypeConfiguration<MagicBackpackAttemptAnswer>
{
  public void Configure(EntityTypeBuilder<MagicBackpackAttemptAnswer> builder)
  {
    builder.HasMany(a => a.Selections)
        .WithOne(s => s.MagicBackpackAttemptAnswer)
        .HasForeignKey(s => s.MagicBackpackAttemptAnswerId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
