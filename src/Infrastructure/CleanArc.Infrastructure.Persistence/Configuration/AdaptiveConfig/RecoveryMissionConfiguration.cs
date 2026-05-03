using CleanArc.Domain.Entities.Adaptive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.AdaptiveConfig;

public class RecoveryMissionConfiguration : IEntityTypeConfiguration<RecoveryMission>
{
    public void Configure(EntityTypeBuilder<RecoveryMission> builder)
    {
        builder.ToTable("recovery_missions");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedTime).HasColumnName("created_at");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.ClassroomId).HasColumnName("classroom_id");
        builder.Property(x => x.ModuleId).HasColumnName("module_id");
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(600).IsRequired();
        builder.Property(x => x.RecommendedGameType).HasColumnName("recommended_game_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DifficultyLevel).HasColumnName("difficulty_level").HasDefaultValue(1);
        builder.Property(x => x.TargetWordsJson).HasColumnName("target_words_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RewardJson).HasColumnName("reward_json").HasColumnType("jsonb").HasDefaultValue("{\"xp\":50,\"diamonds\":2}");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).HasDefaultValue(RecoveryMissionStatuses.Pending);
        builder.Property(x => x.GeneratedBy).HasColumnName("generated_by").HasMaxLength(24).HasDefaultValue(RecoveryMissionGeneratedBy.System);
        builder.Property(x => x.ApprovedByTeacherId).HasColumnName("approved_by_teacher_id");
        builder.Property(x => x.AiAuditLogId).HasColumnName("ai_audit_log_id");
        builder.Property(x => x.AvailableUntil).HasColumnName("available_until");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.ArchiveAt).HasColumnName("archive_at");
        builder.Property(x => x.LinkedChallengeId).HasColumnName("linked_challenge_id");
        builder.Property(x => x.WeakSkill).HasColumnName("weak_skill").HasMaxLength(40).HasDefaultValue("MIXED");
        builder.Property(x => x.TriggerSnapshotJson).HasColumnName("trigger_snapshot_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RewardClaimed).HasColumnName("reward_claimed").HasDefaultValue(false);

        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.ApprovedByTeacher).WithMany().HasForeignKey(x => x.ApprovedByTeacherId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.AiAuditLog).WithMany().HasForeignKey(x => x.AiAuditLogId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.LinkedChallenge).WithMany().HasForeignKey(x => x.LinkedChallengeId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.StudentId, x.ClassroomId, x.ModuleId, x.WeakSkill, x.Status });
        builder.HasIndex(x => x.LinkedChallengeId);
        builder.HasIndex(x => x.ArchiveAt);
    }
}
