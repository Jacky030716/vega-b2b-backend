using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Syllabus;

internal sealed class GetSyllabusModulesQueryHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<GetSyllabusModulesQuery, IReadOnlyList<SyllabusModuleDto>>
{
    public async ValueTask<IReadOnlyList<SyllabusModuleDto>> Handle(
        GetSyllabusModulesQuery request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.GetModulesAsync(
            request.Subject,
            request.Language,
            request.YearLevel,
            cancellationToken);
}

internal sealed class GetSyllabusModuleByIdQueryHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<GetSyllabusModuleByIdQuery, SyllabusModuleDto?>
{
    public async ValueTask<SyllabusModuleDto?> Handle(
        GetSyllabusModuleByIdQuery request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.GetModuleAsync(request.ModuleId, cancellationToken);
}

internal sealed class GetSyllabusModuleVocabularyQueryHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<GetSyllabusModuleVocabularyQuery, IReadOnlyList<VocabularyItemDto>>
{
    public async ValueTask<IReadOnlyList<VocabularyItemDto>> Handle(
        GetSyllabusModuleVocabularyQuery request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.GetVocabularyAsync(request.ModuleId, cancellationToken);
}

internal sealed class CreateSyllabusModuleCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<CreateSyllabusModuleCommand, SyllabusModuleDto>
{
    public async ValueTask<SyllabusModuleDto> Handle(
        CreateSyllabusModuleCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.CreateModuleAsync(request.Request, cancellationToken);
}

internal sealed class CreateSyllabusVocabularyItemCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<CreateSyllabusVocabularyItemCommand, VocabularyItemDto>
{
    public async ValueTask<VocabularyItemDto> Handle(
        CreateSyllabusVocabularyItemCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.CreateVocabularyItemAsync(
            request.ModuleId,
            request.Request,
            cancellationToken);
}
