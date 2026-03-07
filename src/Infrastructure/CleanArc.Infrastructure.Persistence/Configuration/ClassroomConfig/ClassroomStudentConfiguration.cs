using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ClassroomStudentConfiguration : IEntityTypeConfiguration<ClassroomStudent>
{
  public void Configure(EntityTypeBuilder<ClassroomStudent> builder)
  {
    builder.HasIndex(cs => new { cs.ClassroomId, cs.UserId }).IsUnique();
    builder.HasOne(cs => cs.User).WithMany().HasForeignKey(cs => cs.UserId);
  }
}
