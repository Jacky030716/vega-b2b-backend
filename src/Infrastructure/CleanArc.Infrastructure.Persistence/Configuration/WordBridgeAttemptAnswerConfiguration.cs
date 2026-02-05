using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class WordBridgeAttemptAnswerConfiguration : IEntityTypeConfiguration<WordBridgeAttemptAnswer>
{
  public void Configure(EntityTypeBuilder<WordBridgeAttemptAnswer> builder)
  {
    builder.Property(a => a.TargetWord).IsRequired();
  }
}
