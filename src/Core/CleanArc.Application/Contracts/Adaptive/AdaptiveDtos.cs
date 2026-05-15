namespace CleanArc.Application.Contracts.Adaptive;

public record SyllabusModuleDto(
    int Id,
    Guid PublicId,
    string ModuleCode,
    string Subject,
    string Language,
    int YearLevel,
    string Term,
    int? Week,
    int? UnitNumber,
    string UnitTitle,
    string Title,
    string Description,
    string SourceType,
    bool IsActive);

public record VocabularyItemDto(
    int Id,
    Guid PublicId,
    int ModuleId,
    string Word,
    string NormalizedWord,
    string BmText,
    string? ZhText,
    string? EnText,
    string Language,
    string Subject,
    int YearLevel,
    string SyllablesJson,
    string? SyllableText,
    string ItemType,
    int DisplayOrder,
    string? PhoneticHint,
    string? PronunciationText,
    int DifficultyLevel,
    string? MeaningText,
    string? ExampleSentence,
    string? ImageUrl,
    bool IsActive);

public record CreateSyllabusModuleRequest(
    string? ModuleCode,
    string Subject,
    string Language,
    int YearLevel,
    string? Term,
    int? Week,
    int? UnitNumber,
    string? UnitTitle,
    string Title,
    string? Description,
    string? SourceType);

public record CreateVocabularyItemRequest(
    string Word,
    string? BmText,
    string? ZhText,
    string? EnText,
    string? Language,
    string? Subject,
    int? YearLevel,
    string? SyllablesJson,
    string? SyllableText,
    string? ItemType,
    int? DisplayOrder,
    string? PhoneticHint,
    string? PronunciationText,
    int? DifficultyLevel,
    string? MeaningText,
    string? ExampleSentence,
    string? ImageUrl);

public record AdaptiveChallengeItemDto(
    int? ChallengeItemId,
    int? VocabularyItemId,
    string Word,
    string? NormalizedWord,
    string? Hint,
    string? MeaningText,
    string? ExampleSentence,
    string? SyllablesJson,
    int DifficultyLevel,
    string? BmText = null,
    string? ZhText = null,
    string? EnText = null,
    string? SyllableText = null,
    string? ItemType = null,
    int? DisplayOrder = null,
    string? SyllablePoolJson = null,
    string? DistractorsJson = null,
    string? CorrectOrderJson = null,
    string? SpellCatcherSpecJson = null);

public record SpellCatcherMeaningDto(
    string En,
    string Zh);

public record SpellCatcherPreviewPhaseDto(
    bool Enabled,
    int DurationMs,
    bool ShowMeaning,
    bool PlayAudio);

public record SpellCatcherChallengePhaseDto(
    bool ShowMeaningHint,
    bool ShowFirstLetter,
    bool ShowSyllableHint,
    bool AllowRetry,
    int MaxAttempts,
    bool EnableTimePressure);

public record SpellCatcherUiConfigDto(
    SpellCatcherPreviewPhaseDto PreviewPhase,
    SpellCatcherChallengePhaseDto ChallengePhase);

public record SpellCatcherAudioConfigDto(
    string TtsText,
    string Language,
    bool ShouldAutoPlay);

public record SpellCatcherHintsDto(
    string Level1,
    string Level2,
    string Level3);

public record SpellCatcherSpecDto(
    string GameType,
    string TargetWord,
    string ScrambledLetters,
    IReadOnlyList<string> LetterPool,
    SpellCatcherMeaningDto Meaning,
    IReadOnlyList<string> Syllables,
    int DifficultyLevel,
    SpellCatcherUiConfigDto UiConfig,
    SpellCatcherAudioConfigDto AudioConfig,
    SpellCatcherHintsDto Hints);

public record SyllableSushiMeaningDto(
    string En,
    string Zh);

public record SyllableSushiUiConfigDto(
    bool ShowPreview,
    int PreviewDurationMs,
    bool AllowAudio,
    int MaxAttempts,
    IReadOnlyList<string> HintLevels);

public record SyllableSushiSpecDto(
    string GameType,
    string TargetWord,
    SyllableSushiMeaningDto Meaning,
    IReadOnlyList<string> CorrectSyllables,
    IReadOnlyList<string> SyllablePool,
    IReadOnlyList<int> CorrectOrder,
    IReadOnlyList<string> Distractors,
    int DifficultyLevel,
    SyllableSushiUiConfigDto UiConfig);

public record SyllabusSeedDocument(
    string? SourceType,
    string? Subject,
    int? YearLevel,
    IReadOnlyList<SyllabusSeedSubject>? Subjects,
    IReadOnlyList<SyllabusSeedModule>? Modules);

public record SyllabusSeedSubject(
    string? Subject,
    string? Language,
    IReadOnlyList<SyllabusSeedModule>? Modules);

public record SyllabusSeedLanguage(
    string? Primary,
    IReadOnlyList<string>? Translations);

public record SyllabusSeedModule(
    string? ModuleCode,
    int? UnitNumber,
    string? UnitTitle,
    SyllabusSeedLanguage? Language,
    IReadOnlyList<SyllabusSeedItem>? Items);

public record SyllabusSeedText(
    string? Ms,
    string? Zh,
    string? En);

public record SyllabusSeedItem(
    int? DisplayOrder,
    SyllabusSeedText? Text,
    IReadOnlyList<string>? Syllables,
    string? SyllableText,
    string? ItemType,
    string? Pinyin);

public record SyllabusIngestionResult(
    int ModulesCreated,
    int ModulesUpdated,
    int ItemsCreated,
    int ItemsUpdated,
    int ItemsRejected,
    IReadOnlyList<string> Logs,
    IReadOnlyList<string> Errors);

public record GenerateAdaptiveChallengeRequest(
    string TargetType,
    int? StudentId,
    int? ClassId,
    string Objective,
    string SourceType,
    int? ModuleId,
    string? PreferredGameTemplateCode,
    string? LearningFocus,
    IReadOnlyList<string>? ManualWords,
    string? AiPrompt,
    string? SourceText);

public record GeneratedAdaptiveChallengePreviewDto(
    string Title,
    string Description,
    string GameTemplateCode,
    string GameKey,
    string Category,
    int DifficultyLevel,
    int? ModuleId,
    int? StudentId,
    int? ClassId,
    string ChallengeMode,
    string SourceType,
    string ContentData,
    string ConfigJson,
    IReadOnlyList<AdaptiveChallengeItemDto> Items,
    SyllableSushiSpecDto? SyllableSushiSpec = null,
    SpellCatcherSpecDto? SpellCatcherSpec = null);

public record AssignAdaptiveChallengeRequest(
    int CreatedByTeacherId,
    int? StudentId,
    int? ClassId,
    DateTime? DueAt,
    GeneratedAdaptiveChallengePreviewDto Preview,
    string? Subject = null,
    int? CustomModuleId = null,
    string? AiGenerationStatus = null,
    string? AiUseCase = null,
    int? AiAuditLogId = null);

public record AssignedAdaptiveChallengeDto(
    int ChallengeId,
    string Title,
    string GameTemplateCode,
    string GameKey,
    int ItemCount,
    int? StudentId,
    int? ClassId);

public record AdaptiveRecommendationDto(
    string Objective,
    string RecommendedGameTemplateCode,
    string GameKey,
    string Reason,
    int WordCount,
    IReadOnlyList<AdaptiveChallengeItemDto> Items);

public record StartAdaptiveAttemptRequest(int ChallengeId, int? StudentId, string? DeviceInfo);
public record StartAdaptiveAttemptDto(int StudentChallengeAttemptId, int ChallengeId, int AttemptNo);

public record SubmitAdaptiveItemAttemptRequest(
    int StudentChallengeAttemptId,
    int ChallengeItemId,
    int? VocabularyItemId,
    int? GameTemplateId,
    DateTime? PresentedAt,
    DateTime? AnsweredAt,
    int? ResponseTimeMs,
    bool WasCorrect,
    bool FirstAttemptCorrect,
    int RetriesCount,
    int HintsUsed,
    string? AnswerText,
    string? ExpectedAnswerText,
    decimal? SpeechConfidence,
    string? ErrorType,
    string? RawTelemetryJson);

public record CompleteAdaptiveAttemptRequest(
    int StudentChallengeAttemptId,
    int TotalScore,
    string CompletionStatus);

public record StudentWordMasteryDto(
    int Id,
    int StudentId,
    int VocabularyItemId,
    int? ModuleId,
    string Word,
    int MasteryScore,
    string MasteryLevel,
    int TotalAttempts,
    int CorrectAttempts,
    DateTime? LastPracticedAt,
    DateTime? NextReviewAt,
    string WeaknessTagsJson,
    bool IsDueForReview,
    string? ErrorPatternsJson);

public record WordMasterySummaryDto(
    int Id,
    int StudentId,
    int VocabularyItemId,
    int? ModuleId,
    string Word,
    int MasteryScore,
    string MasteryLevel,
    int TotalAttempts,
    int CorrectAttempts,
    DateTime? LastPracticedAt,
    DateTime? NextReviewAt,
    bool IsDueForReview,
    string WeaknessTagsJson,
    string? ErrorPatternsJson);

public record ChallengeAttemptSummaryDto(
    int ChallengeId,
    int StudentId,
    int LegacyAttemptCount,
    int AdaptiveAttemptCount,
    int CompletedLegacyAttemptCount,
    int CompletedAdaptiveAttemptCount,
    int ItemAttemptCount,
    int BestScore,
    int BestStars,
    DateTime? LastAttemptAt);

public record WeaknessSummaryDto(
    int StudentId,
    int WeakWordCount,
    int OverdueReviewCount,
    IReadOnlyList<StudentWordMasteryDto> WeakWords,
    IReadOnlyList<string> RecommendedGameTemplateCodes);

public record ClassWeaknessOverviewDto(
    int ClassId,
    int WeakWordCount,
    int OverdueReviewCount,
    IReadOnlyList<StudentWordMasteryDto> WeakWords);

public record ModuleProgressDto(
    int ClassId,
    int ModuleId,
    string ModuleTitle,
    int VocabularyCount,
    int PracticedWordCount,
    decimal AverageMasteryScore);

public record ModuleProgressSummaryDto(
    int ClassroomId,
    int ModuleId,
    string Title,
    string Subject,
    int YearLevel,
    int VocabularyCount,
    int ChallengeCount,
    int CompletedChallengeCount,
    int ProgressPercent,
    int WeakWordCount,
    decimal AverageScore,
    DateTime? LastActivityAt,
    string Status);

public record StudentPerformanceDto(
    int StudentId,
    IReadOnlyList<StudentWordMasteryDto> Mastery,
    WeaknessSummaryDto WeaknessSummary,
    IReadOnlyList<AdaptiveRecommendationDto> RecommendedNextChallenges);

public record StudentPerformanceSummaryDto(
    int StudentId,
    IReadOnlyList<WordMasterySummaryDto> Mastery,
    WeaknessSummaryDto WeaknessSummary,
    IReadOnlyList<ChallengeAttemptSummaryDto> Attempts,
    IReadOnlyList<AdaptiveRecommendationDto> RecommendedNextChallenges);

public record AttemptConsistencyIssueDto(
    string Severity,
    string Code,
    string Message,
    int? StudentId,
    int? ChallengeId,
    int? ModuleId,
    int? LegacyAttemptId,
    int? AdaptiveAttemptId);

public record AttemptConsistencyReportDto(
    int ClassroomId,
    int? ModuleId,
    int? StudentId,
    int? ChallengeId,
    int IssueCount,
    IReadOnlyList<AttemptConsistencyIssueDto> Issues,
    DateTime CheckedAt);

public record AttemptConsistencyHealthDto(
    int MissingAdaptiveAttempts,
    int MissingLegacyAttempts,
    int MissingItemTelemetry,
    int MissingWordMasteryUpdates,
    IReadOnlyList<int> AffectedStudentIds,
    IReadOnlyList<int> AffectedChallengeIds,
    string Severity,
    string SuggestedFix,
    DateTime CheckedAt);

public record RecommendedActionDto(
    string Type,
    string Title,
    string Description);

public record ClassroomModuleOverviewDto(
    int ClassroomId,
    string ClassroomName,
    int YearLevel,
    string JoinCode,
    int StudentCount,
    int ActiveChallengeCount,
    IReadOnlyList<RecommendedActionDto> RecommendedActions,
    IReadOnlyList<SubjectModuleGroupDto> SubjectGroups,
    CustomModuleSummaryDto CustomModule);

public record SubjectModuleGroupDto(
    string Subject,
    int ModuleCount,
    int ProgressPercent,
    IReadOnlyList<ModuleSummaryDto> Modules);

public record ModuleSummaryDto(
    int ModuleId,
    string ModuleTitle,
    int? UnitNumber,
    string Subject,
    int YearLevel,
    int VocabularyCount,
    int GeneratedChallengeCount,
    int ActiveChallengeCount,
    int ProgressPercent,
    int WeakWordCount,
    int ChallengeCount,
    int CompletedChallengeCount,
    decimal AverageScore,
    DateTime? LastActivityAt,
    string Status);

public record CustomModuleSummaryDto(
    int CustomModuleId,
    string Name,
    int ChallengeCount,
    int ActiveChallengeCount);

public record ModuleChallengeDto(
    int ChallengeId,
    string Title,
    string GameKey,
    string GameType,
    string LifecycleState,
    string Status,
    int ProgressPercent,
    DateTime LastUpdated,
    bool CanDelete,
    IReadOnlyList<string> SelectedWords,
    string? RecommendedGameType,
    int? AiDifficultyLevel,
    string? AiReason,
    string? AiFocusType,
    string? AiValidationStatus,
    IReadOnlyList<string> AiValidationErrors,
    string? AiProvider,
    bool WasFallbackUsed,
    string? ValidationStatus,
    IReadOnlyList<string> TrustIndicators,
    string GenerationSource);

public record GenerateModuleChallengeRequest(
    int ClassroomId,
    string GameType,
    string Mode);

public record CreateCustomModuleChallengeRequest(
    string Title,
    string GameType,
    IReadOnlyList<string>? Items);

public record RenameCustomModuleRequest(string Name);

public record StudentModuleTrackDto(
    int ModuleId,
    string ModuleTitle,
    string Subject,
    int? UnitNumber,
    int YearLevel,
    int VocabularyCount,
    int ActiveChallengeCount,
    int CompletedChallengeCount,
    int ProgressPercent,
    bool Recommended,
    int ChallengeCount,
    int WeakWordCount,
    decimal AverageScore,
    DateTime? LastActivityAt,
    string Status);

public record StudentModuleProgressionDto(
    int ModuleId,
    string ModuleTitle,
    string Subject,
    int? UnitNumber,
    IReadOnlyList<StudentProgressionNodeDto> Nodes);

public record StudentProgressionNodeDto(
    string NodeId,
    int ChallengeId,
    string Type,
    string GameKey,
    string Title,
    string Description,
    string Status,
    int Progress,
    bool IsRecommended,
    int BestStars,
    string ContentData,
    int DifficultyLevel,
    int OrderIndex);

public record StudentCustomChallengeDto(
    int ChallengeId,
    string Title,
    string Description,
    string GameKey,
    string Type,
    string Status,
    int Progress,
    bool IsRecommended,
    int BestStars,
    string ContentData,
    DateTime LastUpdated);

public record RecoveryMissionRewardDto(int Xp, int Diamonds);

public record RecoveryMissionPreviewRequest(
    int ClassroomId,
    int? ModuleId,
    string? Mode);

public record RecoveryMissionPreviewDto(
    string Title,
    string Reason,
    string WeakSkill,
    string SourceType,
    IReadOnlyList<string> TargetWords,
    string RecommendedGameType,
    int DifficultyLevel,
    string SupportStrategy,
    RecoveryMissionRewardDto Reward,
    int EstimatedMinutes,
    string GeneratedBy,
    int? AiAuditLogId,
    string TriggerSnapshotJson);

public record CreateRecoveryMissionRequest(
    int ClassroomId,
    int? ModuleId,
    string? Mode,
    RecoveryMissionPreviewDto? Preview);

public record RecoveryMissionDto(
    int Id,
    int StudentId,
    int ClassroomId,
    int? ModuleId,
    string Title,
    string Reason,
    string WeakSkill,
    string SourceType,
    IReadOnlyList<string> TargetWords,
    string RecommendedGameType,
    int DifficultyLevel,
    string Status,
    RecoveryMissionRewardDto Reward,
    DateTime? AvailableUntil,
    DateTime? CompletedAt,
    DateTime? ArchiveAt,
    int? LinkedChallengeId,
    string? GameKey,
    string? ContentData,
    int EstimatedMinutes);

public record RecoveryMissionStartDto(
    RecoveryMissionDto Mission,
    int MissionId,
    int ChallengeId,
    string GameKey,
    string Title,
    string Description,
    int DifficultyLevel,
    string ContentData);

public record RecoveryMissionCompleteDto(
    bool Success,
    int XpAwarded,
    int DiamondsAwarded,
    DateTime ArchiveAt,
    RecoveryMissionDto Mission);

public record SpellingTestConfigDto(
    int? WordCount,
    int? Difficulty,
    bool IncludeUnmasteredOnly,
    bool RandomizeOrder,
    bool AllowRetries,
    int? TimeLimitSeconds);

public record CreateSpellingTestRequest(
    string Subject,
    string Title,
    string? Description,
    IReadOnlyList<int> ModuleIds,
    DateTime DueAt,
    SpellingTestConfigDto? Config);

public record UpdateSpellingTestRequest(
    string? Title,
    string? Description,
    DateTime? DueAt,
    string? Status);

public record SpellingTestSummaryDto(
    int Id,
    int ClassroomId,
    string Subject,
    string Title,
    string? Description,
    IReadOnlyList<int> ModuleIds,
    IReadOnlyList<string> ModuleTitles,
    DateTime DueAt,
    string Status,
    int WordCount,
    int AssignedCount,
    int CompletedCount,
    int OverdueCount,
    SpellingTestConfigDto Config,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record StudentSpellingTestSummaryDto(
    int Id,
    int ClassroomId,
    string Subject,
    string Title,
    string? Description,
    IReadOnlyList<string> ModuleTitles,
    DateTime DueAt,
    string Status,
    string StudentStatus,
    int WordCount,
    int? Score,
    int? Stars,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ConfirmedAt,
    int? RemainingSeconds,
    DateTime? ModalSeenAt,
    DateTime? DismissedAt,
    bool ShouldShowModal);

public record SpellingTestQuestionDto(
    int VocabularyItemId,
    string Word,
    string? MeaningText,
    string? ExampleSentence,
    string? BmText,
    string? EnText,
    string? ZhText,
    string? SyllableText,
    int DifficultyLevel);

public record StudentSpellingTestDetailDto(
    StudentSpellingTestSummaryDto Test,
    IReadOnlyList<SpellingTestQuestionDto> Questions,
    int? RemainingSeconds,
    IReadOnlyList<SpellingTestAnswerDto> SavedAnswers);

public record SpellingTestAnswerDto(
    int VocabularyItemId,
    string AnswerText,
    int? ResponseTimeMs,
    int? RetriesCount,
    int? HintsUsed);

public record SubmitSpellingTestRequest(IReadOnlyList<SpellingTestAnswerDto> Answers);

public record PauseSpellingTestRequest(IReadOnlyList<SpellingTestAnswerDto> Answers);

public record SubmitSpellingTestResultDto(
    int TestId,
    string StudentStatus,
    int Score,
    int Stars,
    int CorrectCount,
    int TotalCount,
    DateTime CompletedAt);

public record SpellingTestStudentResultRowDto(
    int StudentId,
    string StudentName,
    string Status,
    int? Score,
    int? Stars,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public record SpellingTestResultsDto(
    int TestId,
    string Title,
    DateTime DueAt,
    int AssignedCount,
    int CompletedCount,
    int OverdueCount,
    IReadOnlyList<SpellingTestStudentResultRowDto> Students);

public interface IRecoveryMissionService
{
    Task<RecoveryMissionPreviewDto> PreviewAsync(int studentId, RecoveryMissionPreviewRequest request, int teacherId, CancellationToken cancellationToken);
    Task<RecoveryMissionDto> CreateAsync(int studentId, CreateRecoveryMissionRequest request, int teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryMissionDto>> GetForTeacherAsync(int studentId, int teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryMissionDto>> GetActiveForStudentAsync(int studentId, CancellationToken cancellationToken);
    Task<RecoveryMissionStartDto> StartAsync(int missionId, int studentId, CancellationToken cancellationToken);
    Task<RecoveryMissionCompleteDto> CompleteAsync(int missionId, int studentId, CancellationToken cancellationToken);
}

public interface ISpellingTestService
{
    Task<SpellingTestSummaryDto> CreateAsync(int classroomId, CreateSpellingTestRequest request, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<SpellingTestSummaryDto>> GetForTeacherAsync(int classroomId, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<SpellingTestSummaryDto> GetTeacherDetailAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<SpellingTestResultsDto> GetTeacherResultsAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<SpellingTestSummaryDto> UpdateAsync(int testId, UpdateSpellingTestRequest request, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(int testId, int teacherId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentSpellingTestSummaryDto>> GetActiveForStudentAsync(int studentId, CancellationToken cancellationToken);
    Task<StudentSpellingTestDetailDto> GetStudentDetailAsync(int testId, int studentId, CancellationToken cancellationToken);
    Task<StudentSpellingTestDetailDto> StartAsync(int testId, int studentId, CancellationToken cancellationToken);
    Task<StudentSpellingTestDetailDto> ResumeAsync(int testId, int studentId, CancellationToken cancellationToken);
    Task<StudentSpellingTestDetailDto> PauseAsync(int testId, int studentId, PauseSpellingTestRequest request, CancellationToken cancellationToken);
    Task<SubmitSpellingTestResultDto> SubmitAsync(int testId, int studentId, SubmitSpellingTestRequest request, CancellationToken cancellationToken);
    Task<StudentSpellingTestSummaryDto> DismissModalAsync(int testId, int studentId, CancellationToken cancellationToken);
}
