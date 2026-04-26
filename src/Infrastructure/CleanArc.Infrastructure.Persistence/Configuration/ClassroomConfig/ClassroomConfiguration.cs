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
    builder.Property(c => c.YearLevel).HasColumnName("year_level").HasDefaultValue(1);
    builder.Property(c => c.JoinCode).IsRequired().HasMaxLength(4);
    builder.HasIndex(c => c.JoinCode).IsUnique();
    builder.Property(c => c.IsActive).HasDefaultValue(true);
    builder.HasOne(c => c.Teacher).WithMany().HasForeignKey(c => c.TeacherId);
    builder.HasMany(c => c.Students).WithOne(s => s.Classroom).HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class CustomModuleConfiguration : IEntityTypeConfiguration<CustomModule>
{
  public void Configure(EntityTypeBuilder<CustomModule> builder)
  {
    builder.ToTable("custom_modules");
    builder.Property(c => c.Id).HasColumnName("id");
    builder.Property(c => c.ClassroomId).HasColumnName("classroom_id").IsRequired();
    builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(160).HasDefaultValue("Custom Module").IsRequired();
    builder.Property(c => c.YearLevel).HasColumnName("year_level").HasDefaultValue(1);
    builder.Property(c => c.CreatedByTeacherId).HasColumnName("created_by_teacher_id").IsRequired();
    builder.Property(c => c.CreatedTime).HasColumnName("created_at");
    builder.Property(c => c.ModifiedDate).HasColumnName("updated_at");

    builder.HasOne(c => c.Classroom)
        .WithMany(c => c.CustomModules)
        .HasForeignKey(c => c.ClassroomId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(c => c.CreatedByTeacher)
        .WithMany()
        .HasForeignKey(c => c.CreatedByTeacherId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(c => c.ClassroomId).IsUnique();
  }
}
