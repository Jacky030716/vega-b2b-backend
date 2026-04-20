using CleanArc.Domain.Entities.Sticker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StickerGiftTransactionConfiguration : IEntityTypeConfiguration<StickerGiftTransaction>
{
  public void Configure(EntityTypeBuilder<StickerGiftTransaction> builder)
  {
    builder.Property(g => g.SenderUserId).IsRequired();
    builder.Property(g => g.RecipientUserId).IsRequired();
    builder.Property(g => g.SourceStickerId).IsRequired();
    builder.Property(g => g.RecipientStickerId).IsRequired();
    builder.Property(g => g.DiamondCost).IsRequired();
    builder.Property(g => g.Status).IsRequired();

    builder.HasIndex(g => g.SenderUserId);
    builder.HasIndex(g => g.RecipientUserId);
    builder.HasIndex(g => new { g.RecipientUserId, g.Status });
    builder.HasIndex(g => g.RecipientStickerId).IsUnique();

    builder.HasOne(g => g.SenderUser)
      .WithMany()
      .HasForeignKey(g => g.SenderUserId);

    builder.HasOne(g => g.RecipientUser)
      .WithMany()
      .HasForeignKey(g => g.RecipientUserId);

    builder.HasOne(g => g.SourceSticker)
      .WithMany(s => s.SourceGiftTransactions)
      .HasForeignKey(g => g.SourceStickerId);

    builder.HasOne(g => g.RecipientSticker)
      .WithMany()
      .HasForeignKey(g => g.RecipientStickerId);
  }
}
