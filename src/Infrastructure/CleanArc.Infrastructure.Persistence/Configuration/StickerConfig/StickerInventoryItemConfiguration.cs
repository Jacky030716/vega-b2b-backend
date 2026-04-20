using CleanArc.Domain.Entities.Sticker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StickerInventoryItemConfiguration : IEntityTypeConfiguration<StickerInventoryItem>
{
  public void Configure(EntityTypeBuilder<StickerInventoryItem> builder)
  {
    builder.Property(s => s.OwnerUserId).IsRequired();
    builder.Property(s => s.CreatorUserId).IsRequired();
    builder.Property(s => s.ImageUrl).IsRequired().HasMaxLength(1000);
    builder.Property(s => s.OwnershipSource).IsRequired();
    builder.Property(s => s.PromptChoicesJson).IsRequired().HasDefaultValue("{}");
    builder.Property(s => s.GenerationModel).HasMaxLength(100);

    builder.HasIndex(s => s.OwnerUserId);
    builder.HasIndex(s => s.CreatorUserId);
    builder.HasIndex(s => s.SourceStickerId);

    builder.HasOne(s => s.OwnerUser)
      .WithMany()
      .HasForeignKey(s => s.OwnerUserId);

    builder.HasOne(s => s.CreatorUser)
      .WithMany()
      .HasForeignKey(s => s.CreatorUserId);

    builder.HasOne(s => s.SourceSticker)
      .WithMany(s => s.ClonedStickers)
      .HasForeignKey(s => s.SourceStickerId)
      .IsRequired(false)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
