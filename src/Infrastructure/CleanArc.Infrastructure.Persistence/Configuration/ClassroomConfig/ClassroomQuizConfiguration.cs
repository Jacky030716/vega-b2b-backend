using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ClassroomQuizConfiguration : IEntityTypeConfiguration<ClassroomQuiz>
{
  public void Configure(EntityTypeBuilder<ClassroomQuiz> builder)
  {
    builder.Property(cq => cq.QuizId).IsRequired();
    builder.HasIndex(cq => new { cq.ClassroomId, cq.QuizId }).IsUnique();
  }
}
