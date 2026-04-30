using CleanArc.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.AIConfig;

public class AiAuditLogConfiguration : IEntityTypeConfiguration<AiAuditLog>
{
    public void Configure(EntityTypeBuilder<AiAuditLog> builder)
    {
        builder.ToTable("ai_audit_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UseCase).HasColumnName("use_case").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(160);
        builder.Property(x => x.PromptVersion).HasColumnName("prompt_version").HasMaxLength(40).IsRequired();
        builder.Property(x => x.InputPayloadJson).HasColumnName("input_payload_json").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.RawOutputJson).HasColumnName("raw_output_json").HasColumnType("jsonb");
        builder.Property(x => x.ParsedOutputJson).HasColumnName("parsed_output_json").HasColumnType("jsonb");
        builder.Property(x => x.ValidationStatus).HasColumnName("validation_status").HasMaxLength(40).HasDefaultValue("PENDING").IsRequired();
        builder.Property(x => x.ValidationErrorsJson).HasColumnName("validation_errors_json").HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(x => x.RelatedUserId).HasColumnName("related_user_id");
        builder.Property(x => x.RelatedClassroomId).HasColumnName("related_classroom_id");
        builder.Property(x => x.RelatedModuleId).HasColumnName("related_module_id");
        builder.Property(x => x.RelatedChallengeId).HasColumnName("related_challenge_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedTime).HasColumnName("created_time");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");

        builder.HasOne(x => x.RelatedChallenge)
            .WithMany()
            .HasForeignKey(x => x.RelatedChallengeId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => new { x.UseCase, x.CreatedAt });
        builder.HasIndex(x => x.RelatedUserId);
        builder.HasIndex(x => x.RelatedClassroomId);
        builder.HasIndex(x => x.RelatedModuleId);
        builder.HasIndex(x => x.RelatedChallengeId);
    }
}
