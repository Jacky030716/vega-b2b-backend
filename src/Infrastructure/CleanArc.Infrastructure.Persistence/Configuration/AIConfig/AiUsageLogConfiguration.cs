using CleanArc.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.AIConfig;

public class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> builder)
    {
        builder.ToTable("ai_usage_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.FeatureType).HasColumnName("feature_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EndpointKey).HasColumnName("endpoint_key").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(160);
        builder.Property(x => x.RequestCount).HasColumnName("request_count").HasDefaultValue(1);
        builder.Property(x => x.Success).HasColumnName("success").HasDefaultValue(true);
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(80);
        builder.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(80);
        builder.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedTime).HasColumnName("created_time");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.UserId, x.FeatureType, x.CreatedAt });
        builder.HasIndex(x => x.FeatureType);
        builder.HasIndex(x => x.RelatedEntityType);
        builder.HasIndex(x => x.RelatedEntityId);
    }
}
