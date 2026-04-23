using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Attempts;

internal sealed class StartAdaptiveAttemptCommandHandler(IAdaptiveAttemptService adaptiveAttemptService)
    : IRequestHandler<StartAdaptiveAttemptCommand, StartAdaptiveAttemptDto>
{
    public async ValueTask<StartAdaptiveAttemptDto> Handle(
        StartAdaptiveAttemptCommand request,
        CancellationToken cancellationToken)
        => await adaptiveAttemptService.StartAsync(
            request.Request,
            request.AuthenticatedStudentId,
            cancellationToken);
}

internal sealed class RecordAdaptiveItemAttemptCommandHandler(IAdaptiveAttemptService adaptiveAttemptService)
    : IRequestHandler<RecordAdaptiveItemAttemptCommand, StudentWordMasteryDto?>
{
    public async ValueTask<StudentWordMasteryDto?> Handle(
        RecordAdaptiveItemAttemptCommand request,
        CancellationToken cancellationToken)
        => await adaptiveAttemptService.RecordItemAsync(request.Request, cancellationToken);
}

internal sealed class CompleteAdaptiveAttemptCommandHandler(IAdaptiveAttemptService adaptiveAttemptService)
    : IRequestHandler<CompleteAdaptiveAttemptCommand, bool>
{
    public async ValueTask<bool> Handle(
        CompleteAdaptiveAttemptCommand request,
        CancellationToken cancellationToken)
    {
        await adaptiveAttemptService.CompleteAsync(request.Request, cancellationToken);
        return true;
    }
}
