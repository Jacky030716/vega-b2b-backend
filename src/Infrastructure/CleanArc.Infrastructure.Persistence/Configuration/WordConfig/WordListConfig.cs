using CleanArc.Domain.Entities.Word;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.WordConfig
{
    internal class WordListConfig : IEntityTypeConfiguration<WordList>
    {
        public void Configure(EntityTypeBuilder<WordList> builder)
        {
            builder.ToTable("WordLists");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.WordLists)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
