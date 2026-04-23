using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Adaptive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.AdaptiveConfig;

public class SyllabusModuleConfiguration : IEntityTypeConfiguration<SyllabusModule>
{
    public void Configure(EntityTypeBuilder<SyllabusModule> builder)
    {
        ConfigureBase(builder, "syllabus_modules");
        builder.Property(x => x.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()").IsRequired();
        builder.Property(x => x.ModuleCode).HasColumnName("module_code").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(20).IsRequired();
        builder.Property(x => x.YearLevel).HasColumnName("year_level").IsRequired();
        builder.Property(x => x.Term).HasColumnName("term").HasMaxLength(50);
        builder.Property(x => x.Week).HasColumnName("week");
        builder.Property(x => x.UnitNumber).HasColumnName("unit_number");
        builder.Property(x => x.UnitTitle).HasColumnName("unit_title").HasMaxLength(200);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.ModuleCode).IsUnique();
        builder.HasIndex(x => new { x.Subject, x.Language, x.YearLevel, x.Term, x.Week });
    }

    internal static void ConfigureBase<TEntity>(EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : BaseEntity<int>
    {
        builder.ToTable(tableName);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedTime).HasColumnName("created_at");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");
    }
}

public class VocabularyItemConfiguration : IEntityTypeConfiguration<VocabularyItem>
{
    public void Configure(EntityTypeBuilder<VocabularyItem> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "vocabulary_items");
        builder.Property(x => x.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()").IsRequired();
        builder.Property(x => x.ModuleId).HasColumnName("module_id");
        builder.Property(x => x.Word).HasColumnName("word").HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedWord).HasColumnName("normalized_word").HasMaxLength(120).IsRequired();
        builder.Property(x => x.BmText).HasColumnName("bm_text").HasMaxLength(240).IsRequired();
        builder.Property(x => x.ZhText).HasColumnName("zh_text").HasMaxLength(240);
        builder.Property(x => x.EnText).HasColumnName("en_text").HasMaxLength(240);
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(100).IsRequired();
        builder.Property(x => x.YearLevel).HasColumnName("year_level");
        builder.Property(x => x.SyllablesJson).HasColumnName("syllables_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.SyllableText).HasColumnName("syllable_text").HasMaxLength(240);
        builder.Property(x => x.ItemType).HasColumnName("item_type").HasMaxLength(30).HasDefaultValue("WORD");
        builder.Property(x => x.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        builder.Property(x => x.PhoneticHint).HasColumnName("phonetic_hint");
        builder.Property(x => x.PronunciationText).HasColumnName("pronunciation_text");
        builder.Property(x => x.DifficultyLevel).HasColumnName("difficulty_level");
        builder.Property(x => x.MeaningText).HasColumnName("meaning_text");
        builder.Property(x => x.ExampleSentence).HasColumnName("example_sentence");
        builder.Property(x => x.ImageUrl).HasColumnName("image_url");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasOne(x => x.Module).WithMany(x => x.VocabularyItems).HasForeignKey(x => x.ModuleId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => new { x.ModuleId, x.NormalizedWord }).IsUnique();
        builder.HasIndex(x => new { x.ModuleId, x.DisplayOrder });
    }
}

public class GameTemplateConfiguration : IEntityTypeConfiguration<GameTemplate>
{
    public void Configure(EntityTypeBuilder<GameTemplate> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "game_templates");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.SupportsAdaptiveDifficulty).HasColumnName("supports_adaptive_difficulty");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class ChallengeItemConfiguration : IEntityTypeConfiguration<ChallengeItem>
{
    public void Configure(EntityTypeBuilder<ChallengeItem> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "challenge_items");
        builder.Property(x => x.ChallengeId).HasColumnName("challenge_id");
        builder.Property(x => x.VocabularyItemId).HasColumnName("vocabulary_item_id");
        builder.Property(x => x.SequenceNo).HasColumnName("sequence_no");
        builder.Property(x => x.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.HasOne(x => x.Challenge).WithMany(x => x.ChallengeItems).HasForeignKey(x => x.ChallengeId);
        builder.HasOne(x => x.VocabularyItem).WithMany().HasForeignKey(x => x.VocabularyItemId).IsRequired(false);
        builder.HasIndex(x => new { x.ChallengeId, x.SequenceNo }).IsUnique();
    }
}

public class StudentChallengeAttemptConfiguration : IEntityTypeConfiguration<StudentChallengeAttempt>
{
    public void Configure(EntityTypeBuilder<StudentChallengeAttempt> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "student_challenge_attempts");
        builder.Property(x => x.ChallengeId).HasColumnName("challenge_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.AttemptNo).HasColumnName("attempt_no");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.TotalScore).HasColumnName("total_score");
        builder.Property(x => x.CompletionStatus).HasColumnName("completion_status").HasMaxLength(40);
        builder.Property(x => x.AverageResponseTimeMs).HasColumnName("average_response_time_ms");
        builder.Property(x => x.TotalHintsUsed).HasColumnName("total_hints_used");
        builder.Property(x => x.TotalRetries).HasColumnName("total_retries");
        builder.Property(x => x.DeviceInfo).HasColumnName("device_info");
        builder.HasOne(x => x.Challenge).WithMany().HasForeignKey(x => x.ChallengeId);
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        builder.HasIndex(x => new { x.ChallengeId, x.StudentId, x.AttemptNo }).IsUnique();
    }
}

public class StudentChallengeItemAttemptConfiguration : IEntityTypeConfiguration<StudentChallengeItemAttempt>
{
    public void Configure(EntityTypeBuilder<StudentChallengeItemAttempt> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "student_challenge_item_attempts");
        builder.Property(x => x.StudentChallengeAttemptId).HasColumnName("student_challenge_attempt_id");
        builder.Property(x => x.ChallengeItemId).HasColumnName("challenge_item_id");
        builder.Property(x => x.VocabularyItemId).HasColumnName("vocabulary_item_id");
        builder.Property(x => x.GameTemplateId).HasColumnName("game_template_id");
        builder.Property(x => x.PresentedAt).HasColumnName("presented_at");
        builder.Property(x => x.AnsweredAt).HasColumnName("answered_at");
        builder.Property(x => x.ResponseTimeMs).HasColumnName("response_time_ms");
        builder.Property(x => x.WasCorrect).HasColumnName("was_correct");
        builder.Property(x => x.FirstAttemptCorrect).HasColumnName("first_attempt_correct");
        builder.Property(x => x.RetriesCount).HasColumnName("retries_count");
        builder.Property(x => x.HintsUsed).HasColumnName("hints_used");
        builder.Property(x => x.AnswerText).HasColumnName("answer_text");
        builder.Property(x => x.ExpectedAnswerText).HasColumnName("expected_answer_text");
        builder.Property(x => x.SpeechConfidence).HasColumnName("speech_confidence").HasPrecision(5, 2);
        builder.Property(x => x.ErrorType).HasColumnName("error_type").HasMaxLength(80);
        builder.Property(x => x.RawTelemetryJson).HasColumnName("raw_telemetry_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.HasOne(x => x.StudentChallengeAttempt).WithMany(x => x.ItemAttempts).HasForeignKey(x => x.StudentChallengeAttemptId);
        builder.HasOne(x => x.ChallengeItem).WithMany().HasForeignKey(x => x.ChallengeItemId);
        builder.HasOne(x => x.VocabularyItem).WithMany().HasForeignKey(x => x.VocabularyItemId).IsRequired(false);
        builder.HasOne(x => x.GameTemplate).WithMany().HasForeignKey(x => x.GameTemplateId).IsRequired(false);
    }
}

public class StudentWordMasteryConfiguration : IEntityTypeConfiguration<StudentWordMastery>
{
    public void Configure(EntityTypeBuilder<StudentWordMastery> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "student_word_mastery");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.VocabularyItemId).HasColumnName("vocabulary_item_id");
        builder.Property(x => x.ModuleId).HasColumnName("module_id");
        builder.Property(x => x.MasteryScore).HasColumnName("mastery_score");
        builder.Property(x => x.MasteryLevel).HasColumnName("mastery_level").HasMaxLength(40);
        builder.Property(x => x.TotalAttempts).HasColumnName("total_attempts");
        builder.Property(x => x.CorrectAttempts).HasColumnName("correct_attempts");
        builder.Property(x => x.FirstTryCorrectCount).HasColumnName("first_try_correct_count");
        builder.Property(x => x.AverageResponseTimeMs).HasColumnName("average_response_time_ms");
        builder.Property(x => x.TotalHintsUsed).HasColumnName("total_hints_used");
        builder.Property(x => x.TotalRetries).HasColumnName("total_retries");
        builder.Property(x => x.LastPracticedAt).HasColumnName("last_practiced_at");
        builder.Property(x => x.NextReviewAt).HasColumnName("next_review_at");
        builder.Property(x => x.WeaknessTagsJson).HasColumnName("weakness_tags_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.LastGameTemplateId).HasColumnName("last_game_template_id");
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        builder.HasOne(x => x.VocabularyItem).WithMany().HasForeignKey(x => x.VocabularyItemId);
        builder.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId).IsRequired(false);
        builder.HasOne(x => x.LastGameTemplate).WithMany().HasForeignKey(x => x.LastGameTemplateId).IsRequired(false);
        builder.HasIndex(x => new { x.StudentId, x.VocabularyItemId }).IsUnique();
    }
}

public class StudentSkillProfileConfiguration : IEntityTypeConfiguration<StudentSkillProfile>
{
    public void Configure(EntityTypeBuilder<StudentSkillProfile> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "student_skill_profiles");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(100);
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(20);
        builder.Property(x => x.SpellingRecallScore).HasColumnName("spelling_recall_score");
        builder.Property(x => x.PronunciationRecallScore).HasColumnName("pronunciation_recall_score");
        builder.Property(x => x.SyllableAssemblyScore).HasColumnName("syllable_assembly_score");
        builder.Property(x => x.VisualMemoryScore).HasColumnName("visual_memory_score");
        builder.Property(x => x.AuditoryMemoryScore).HasColumnName("auditory_memory_score");
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        builder.HasIndex(x => new { x.StudentId, x.Subject, x.Language }).IsUnique();
    }
}

public class ErrorPatternLogConfiguration : IEntityTypeConfiguration<ErrorPatternLog>
{
    public void Configure(EntityTypeBuilder<ErrorPatternLog> builder)
    {
        SyllabusModuleConfiguration.ConfigureBase(builder, "error_pattern_logs");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.VocabularyItemId).HasColumnName("vocabulary_item_id");
        builder.Property(x => x.ChallengeItemAttemptId).HasColumnName("challenge_item_attempt_id");
        builder.Property(x => x.PatternType).HasColumnName("pattern_type").HasMaxLength(80);
        builder.Property(x => x.ObservedValue).HasColumnName("observed_value");
        builder.Property(x => x.ExpectedValue).HasColumnName("expected_value");
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        builder.HasOne(x => x.VocabularyItem).WithMany().HasForeignKey(x => x.VocabularyItemId).IsRequired(false);
        builder.HasOne(x => x.ChallengeItemAttempt).WithMany().HasForeignKey(x => x.ChallengeItemAttemptId).IsRequired(false);
    }
}
