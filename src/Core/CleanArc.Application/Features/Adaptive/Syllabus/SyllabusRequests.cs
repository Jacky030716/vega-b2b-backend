using CleanArc.Application.Contracts.Adaptive;
using Mediator;

namespace CleanArc.Application.Features.Adaptive.Syllabus;

public sealed record GetSyllabusModulesQuery(
    string? Subject,
    string? Language,
    int? YearLevel)
    : IRequest<IReadOnlyList<SyllabusModuleDto>>;

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
