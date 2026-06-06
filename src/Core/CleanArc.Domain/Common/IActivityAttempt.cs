using System;

namespace CleanArc.Domain.Common;

public interface IActivityAttempt
{
    int Id { get; }
    int StudentId { get; }
    int ActivityId { get; }
    string Status { get; }
    int? Score { get; }
    int? Stars { get; }
    DateTime? StartedAt { get; }
    DateTime? CompletedAt { get; }
}
