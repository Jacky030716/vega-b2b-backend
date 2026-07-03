using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("EventId")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        builder.Property(x => x.UnlockedBadgeIds)
            .HasMaxLength(1000)
            .IsRequired();
    }
}
