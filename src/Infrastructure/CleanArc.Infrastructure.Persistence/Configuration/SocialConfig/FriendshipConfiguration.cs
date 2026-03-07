using CleanArc.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
  public void Configure(EntityTypeBuilder<Friendship> builder)
  {
    builder.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
    builder.Property(f => f.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
    builder.HasOne(f => f.Requester).WithMany().HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.NoAction);
    builder.HasOne(f => f.Addressee).WithMany().HasForeignKey(f => f.AddresseeId).OnDelete(DeleteBehavior.NoAction);
  }
}
