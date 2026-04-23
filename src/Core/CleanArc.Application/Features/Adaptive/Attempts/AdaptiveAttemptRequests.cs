using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Attempts;

public sealed record StartAdaptiveAttemptCommand(
    StartAdaptiveAttemptRequest Request,
    int AuthenticatedStudentId)
    : IRequest<StartAdaptiveAttemptDto>;

public sealed record RecordAdaptiveItemAttemptCommand(
    SubmitAdaptiveItemAttemptRequest Request)
    : IRequest<StudentWordMasteryDto?>;

public sealed record CompleteAdaptiveAttemptCommand(
    CompleteAdaptiveAttemptRequest Request)
    : IRequest<bool>;
