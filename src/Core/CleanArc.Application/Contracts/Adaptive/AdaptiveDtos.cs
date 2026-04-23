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
    string? ItemType);

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
    GeneratedAdaptiveChallengePreviewDto Preview);

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
    string WeaknessTagsJson);

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

public record StudentPerformanceDto(
    int StudentId,
    IReadOnlyList<StudentWordMasteryDto> Mastery,
    WeaknessSummaryDto WeaknessSummary,
    IReadOnlyList<AdaptiveRecommendationDto> RecommendedNextChallenges);
