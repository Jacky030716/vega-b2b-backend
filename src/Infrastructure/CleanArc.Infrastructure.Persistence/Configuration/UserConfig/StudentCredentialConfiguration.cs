using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.UserConfig;

internal class StudentCredentialConfiguration : IEntityTypeConfiguration<StudentCredential>
{
  public void Configure(EntityTypeBuilder<StudentCredential> builder)
  {
    builder.ToTable("StudentCredentials", "usr");

    builder.Property(sc => sc.StudentLoginCode)
        .IsRequired()
        .HasMaxLength(16);

    builder.Property(sc => sc.VisualPasswordHash)
        .IsRequired();

    builder.Property(sc => sc.IsActive)
        .HasDefaultValue(true);

    builder.HasIndex(sc => sc.StudentLoginCode)
        .IsUnique();

    builder.HasIndex(sc => new { sc.ClassroomId, sc.UserId })
        .IsUnique();

    builder.HasOne(sc => sc.User)
        .WithMany()
        .HasForeignKey(sc => sc.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(sc => sc.Classroom)
        .WithMany()
        .HasForeignKey(sc => sc.ClassroomId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
