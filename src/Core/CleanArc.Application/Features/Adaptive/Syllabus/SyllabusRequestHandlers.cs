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
            request.TeacherId,
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

internal sealed class UpdateSyllabusVocabularyItemCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<UpdateSyllabusVocabularyItemCommand, VocabularyItemDto>
{
    public async ValueTask<VocabularyItemDto> Handle(
        UpdateSyllabusVocabularyItemCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.UpdateVocabularyItemAsync(
            request.ModuleId,
            request.ItemId,
            request.Request,
            cancellationToken);
}

internal sealed class DeleteSyllabusVocabularyItemCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<DeleteSyllabusVocabularyItemCommand, bool>
{
    public async ValueTask<bool> Handle(
        DeleteSyllabusVocabularyItemCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.DeleteVocabularyItemAsync(
            request.ModuleId,
            request.ItemId,
            cancellationToken);
}

internal sealed class UpdateSyllabusModuleCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<UpdateSyllabusModuleCommand, SyllabusModuleDto>
{
    public async ValueTask<SyllabusModuleDto> Handle(
        UpdateSyllabusModuleCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.UpdateModuleAsync(
            request.ModuleId,
            request.Request,
            request.TeacherId,
            cancellationToken);
}

internal sealed class DeleteSyllabusModuleCommandHandler(ISyllabusModuleService syllabusModuleService)
    : IRequestHandler<DeleteSyllabusModuleCommand, bool>
{
    public async ValueTask<bool> Handle(
        DeleteSyllabusModuleCommand request,
        CancellationToken cancellationToken)
        => await syllabusModuleService.DeleteModuleAsync(
            request.ModuleId,
            request.TeacherId,
            cancellationToken);
}

