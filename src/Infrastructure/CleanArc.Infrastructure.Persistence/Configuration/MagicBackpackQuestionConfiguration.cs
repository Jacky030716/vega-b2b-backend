using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class MagicBackpackQuestionConfiguration : IEntityTypeConfiguration<MagicBackpackQuestion>
{
  public void Configure(EntityTypeBuilder<MagicBackpackQuestion> builder)
  {
    builder.Property(m => m.Theme).IsRequired();
    builder.Property(m => m.AgeGroup).IsRequired();

    builder.HasMany(m => m.Items)
        .WithOne(i => i.MagicBackpackQuestion)
        .HasForeignKey(i => i.MagicBackpackQuestionId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(m => m.Sequence)
        .WithOne(s => s.MagicBackpackQuestion)
        .HasForeignKey(s => s.MagicBackpackQuestionId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
