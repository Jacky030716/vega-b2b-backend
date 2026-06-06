using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class DiamondTransactionConfiguration : IEntityTypeConfiguration<DiamondTransaction>
{
  public void Configure(EntityTypeBuilder<DiamondTransaction> builder)
  {
    builder.Property(d => d.UserId).IsRequired();
    builder.Property(d => d.Amount).IsRequired();
    builder.Property(d => d.Reason).IsRequired().HasMaxLength(50);
    // Composite index covers WHERE UserId = ? ORDER BY CreatedTime DESC in GetDiamondTransactionsAsync.
    builder.HasIndex(d => new { d.UserId, d.CreatedTime });
    builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
  }
}
