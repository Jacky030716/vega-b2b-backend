using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class WordBridgeQuestionConfiguration : IEntityTypeConfiguration<WordBridgeQuestion>
{
  public void Configure(EntityTypeBuilder<WordBridgeQuestion> builder)
  {
    builder.Property(w => w.TargetWord).IsRequired();
    builder.Property(w => w.Translation).IsRequired();
    builder.Property(w => w.Difficulty).IsRequired();
    builder.Property(w => w.ImageUrl).HasDefaultValue(string.Empty);
  }
}
