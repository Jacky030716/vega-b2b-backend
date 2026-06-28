using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
  public void Configure(EntityTypeBuilder<Classroom> builder)
  {
    builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
    builder.Ignore(c => c.Subject);
    builder.Ignore(c => c.YearLevel);
    builder.Property(c => c.Thumbnail).HasColumnName("Thumbnail").HasDefaultValue(string.Empty);
    builder.Property(c => c.ThumbnailType).HasColumnName("thumbnail_type").HasMaxLength(24).HasDefaultValue("DEFAULT");
    builder.Property(c => c.ThumbnailUrl).HasColumnName("thumbnail_url");
    builder.Property(c => c.ThumbnailAssetId).HasColumnName("thumbnail_asset_id");
    builder.Property(c => c.ThumbnailPrompt).HasColumnName("thumbnail_prompt").HasMaxLength(600);
    builder.Property(c => c.ThumbnailGeneratedAt).HasColumnName("thumbnail_generated_at");
    builder.Property(c => c.JoinCode).IsRequired().HasMaxLength(4);
    builder.HasIndex(c => c.JoinCode).IsUnique();
    builder.Property(c => c.IsActive).HasDefaultValue(true);
    builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
    builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");
    builder.HasOne(c => c.Teacher).WithMany().HasForeignKey(c => c.TeacherId);
    builder.HasMany(c => c.Students).WithOne(s => s.Classroom).HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Cascade);
    builder.HasMany(c => c.Subjects).WithOne(s => s.Classroom).HasForeignKey(s => s.ClassroomId).OnDelete(DeleteBehavior.Cascade);
    builder.HasMany(c => c.Modules).WithOne(m => m.Classroom).HasForeignKey(m => m.ClassroomId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class ClassroomSubjectConfiguration : IEntityTypeConfiguration<ClassroomSubject>
{
  public void Configure(EntityTypeBuilder<ClassroomSubject> builder)
  {
    builder.ToTable("classroom_subjects");
    builder.Property(c => c.Id).HasColumnName("id");
    builder.Property(c => c.ClassroomId).HasColumnName("classroom_id").IsRequired();
    builder.Property(c => c.Subject).HasColumnName("subject").HasMaxLength(100).IsRequired();
    builder.Property(c => c.CreatedTime).HasColumnName("created_at");
    builder.Property(c => c.ModifiedDate).HasColumnName("updated_at");

    builder.HasIndex(c => new { c.ClassroomId, c.Subject }).IsUnique();
  }
}

public class ClassroomModuleConfiguration : IEntityTypeConfiguration<ClassroomModule>
{
  public void Configure(EntityTypeBuilder<ClassroomModule> builder)
  {
    builder.ToTable("classroom_modules");
    builder.Property(c => c.Id).HasColumnName("id");
    builder.Property(c => c.ClassroomId).HasColumnName("classroom_id").IsRequired();
    builder.Property(c => c.ModuleId).HasColumnName("module_id").IsRequired();
    builder.Property(c => c.CreatedTime).HasColumnName("created_at");
    builder.Property(c => c.ModifiedDate).HasColumnName("updated_at");

    builder.HasOne(c => c.Module)
        .WithMany(m => m.ClassroomModules)
        .HasForeignKey(c => c.ModuleId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(c => new { c.ClassroomId, c.ModuleId }).IsUnique();
    builder.HasIndex(c => c.ModuleId);
  }
}
