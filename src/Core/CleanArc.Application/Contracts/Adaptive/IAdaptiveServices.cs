namespace CleanArc.Application.Contracts.Adaptive;

public interface ISyllabusModuleService
{
    Task<IReadOnlyList<SyllabusModuleDto>> GetModulesAsync(string? subject, string? language, int? yearLevel, CancellationToken cancellationToken);
    Task<SyllabusModuleDto?> GetModuleAsync(int moduleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VocabularyItemDto>> GetVocabularyAsync(int moduleId, CancellationToken cancellationToken);
    Task<SyllabusModuleDto> CreateModuleAsync(CreateSyllabusModuleRequest request, CancellationToken cancellationToken);
    Task<VocabularyItemDto> CreateVocabularyItemAsync(int moduleId, CreateVocabularyItemRequest request, CancellationToken cancellationToken);
}

public interface ISyllabusModuleIngestionService
{
    Task<SyllabusIngestionResult> IngestAsync(SyllabusSeedDocument document, CancellationToken cancellationToken);
}

public interface IChallengeGenerator
{
    Task<GeneratedAdaptiveChallengePreviewDto> GenerateAsync(GenerateAdaptiveChallengeRequest request, CancellationToken cancellationToken);
}

public interface IRecommendationEngine
{
    Task<IReadOnlyList<AdaptiveRecommendationDto>> RecommendForStudentAsync(int studentId, GenerateAdaptiveChallengeRequest? context, CancellationToken cancellationToken);
}

public interface IMasteryEngine
{
    Task<StudentWordMasteryDto?> ApplyItemAttemptAsync(SubmitAdaptiveItemAttemptRequest request, CancellationToken cancellationToken);
}

public interface IChallengeOrchestrator
{
    Task<IReadOnlyList<AdaptiveRecommendationDto>> RecommendForStudentAsync(int studentId, GenerateAdaptiveChallengeRequest? request, CancellationToken cancellationToken);
    Task<GeneratedAdaptiveChallengePreviewDto> GenerateAsync(GenerateAdaptiveChallengeRequest request, CancellationToken cancellationToken);
    Task<AssignedAdaptiveChallengeDto> AssignAsync(AssignAdaptiveChallengeRequest request, CancellationToken cancellationToken);
    Task<GeneratedAdaptiveChallengePreviewDto?> GetChallengeAsync(int challengeId, CancellationToken cancellationToken);
}

public interface IAdaptiveAttemptService
{
    Task<StartAdaptiveAttemptDto> StartAsync(StartAdaptiveAttemptRequest request, int authenticatedStudentId, CancellationToken cancellationToken);
    Task<StudentWordMasteryDto?> RecordItemAsync(SubmitAdaptiveItemAttemptRequest request, CancellationToken cancellationToken);
    Task CompleteAsync(CompleteAdaptiveAttemptRequest request, CancellationToken cancellationToken);
}

public interface IAdaptiveAnalyticsService
{
    Task<IReadOnlyList<StudentWordMasteryDto>> GetMasteryAsync(int studentId, CancellationToken cancellationToken);
    Task<WeaknessSummaryDto> GetWeaknessSummaryAsync(int studentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdaptiveRecommendationDto>> GetRecommendedNextChallengesAsync(int studentId, CancellationToken cancellationToken);
    Task<ClassWeaknessOverviewDto> GetClassWeaknessOverviewAsync(int classId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ModuleProgressDto>> GetModuleProgressAsync(int classId, CancellationToken cancellationToken);
    Task<StudentPerformanceDto> GetStudentPerformanceAsync(int studentId, CancellationToken cancellationToken);
}
