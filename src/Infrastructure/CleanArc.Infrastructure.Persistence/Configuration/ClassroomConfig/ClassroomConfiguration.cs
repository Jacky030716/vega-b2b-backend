using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
  public void Configure(EntityTypeBuilder<Classroom> builder)
  {
    builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
    builder.Property(c => c.Subject).IsRequired().HasMaxLength(100);
    builder.Property(c => c.JoinCode).IsRequired().HasMaxLength(4);
    builder.HasIndex(c => c.JoinCode).IsUnique();
    builder.Property(c => c.IsActive).HasDefaultValue(true);
    builder.HasOne(c => c.Teacher).WithMany().HasForeignKey(c => c.TeacherId);
    builder.HasMany(c => c.Students).WithOne(s => s.Classroom).HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Cascade);
  }
}
