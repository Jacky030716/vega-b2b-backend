using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Syllabus;

public sealed record GetSyllabusModulesQuery(
    string? Subject,
    string? Language,
    int? YearLevel,
    int? TeacherId = null)
    : IRequest<IReadOnlyList<SyllabusModuleDto>>;

public sealed record UpdateSyllabusModuleCommand(
    int ModuleId,
    UpdateSyllabusModuleRequest Request,
    int TeacherId)
    : IRequest<SyllabusModuleDto>;

public sealed record DeleteSyllabusModuleCommand(
    int ModuleId,
    int TeacherId)
    : IRequest<bool>;

public sealed record GetSyllabusModuleByIdQuery(
    int ModuleId)
    : IRequest<SyllabusModuleDto?>;

public sealed record GetSyllabusModuleVocabularyQuery(
    int ModuleId)
    : IRequest<IReadOnlyList<VocabularyItemDto>>;

public sealed record CreateSyllabusModuleCommand(
    CreateSyllabusModuleRequest Request)
    : IRequest<SyllabusModuleDto>;

public sealed record CreateSyllabusVocabularyItemCommand(
    int ModuleId,
    CreateVocabularyItemRequest Request)
    : IRequest<VocabularyItemDto>;

public sealed record UpdateSyllabusVocabularyItemCommand(
    int ModuleId,
    int ItemId,
    CreateVocabularyItemRequest Request)
    : IRequest<VocabularyItemDto>;

public sealed record DeleteSyllabusVocabularyItemCommand(
    int ModuleId,
    int ItemId)
    : IRequest<bool>;

