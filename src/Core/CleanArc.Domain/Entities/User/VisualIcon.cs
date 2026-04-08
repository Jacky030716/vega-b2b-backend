using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.User;

public class VisualIcon : IEntity
{
    public int Id { get; set; }
    public string Emoji { get; set; }
    public string Label { get; set; }
}
