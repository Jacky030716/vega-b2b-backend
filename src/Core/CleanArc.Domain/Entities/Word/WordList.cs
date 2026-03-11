using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Word
{
    public class WordList : BaseEntity<int>
    {
        public string Title { get; set; } = String.Empty;
        public bool IsDeleted { get; set; }
        public ICollection<Word> Words { get; set; } = new List<Word>();
        public int UserId { get; set; }
        public User.User User { get; set; } = null!;
    }
}
