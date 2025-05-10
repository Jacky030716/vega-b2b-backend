using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Word
{
    public class Word : BaseEntity
    {
        public string Text { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;

        public int WordListId { get; set; }
        public WordList WordList { get; set; } = null!;
    }
}
