using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class MagicBackpackAttemptSelectionConfiguration : IEntityTypeConfiguration<MagicBackpackAttemptSelection>
{
  public void Configure(EntityTypeBuilder<MagicBackpackAttemptSelection> builder)
  {
    builder.Property(s => s.ItemId).IsRequired();
    builder.Property(s => s.Order).IsRequired();
  }
}
