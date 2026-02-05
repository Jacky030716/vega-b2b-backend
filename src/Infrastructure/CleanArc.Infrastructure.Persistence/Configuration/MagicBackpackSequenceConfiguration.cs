using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class MagicBackpackSequenceConfiguration : IEntityTypeConfiguration<MagicBackpackSequence>
{
  public void Configure(EntityTypeBuilder<MagicBackpackSequence> builder)
  {
    builder.Property(s => s.ItemId).IsRequired();
    builder.Property(s => s.Order).IsRequired();
  }
}
