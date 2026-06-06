using System;

namespace CleanArc.Domain.Common;

public interface IPlayableActivity
{
    int Id { get; }
    int? ClassroomId { get; }
    string Title { get; }
    string? Description { get; }
    string? Subject { get; }
    DateTime? DueAt { get; }
    string Status { get; }
    string ConfigJson { get; }
}
