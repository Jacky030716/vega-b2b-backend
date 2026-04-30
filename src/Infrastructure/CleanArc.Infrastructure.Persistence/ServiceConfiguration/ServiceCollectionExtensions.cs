using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Contracts.Infrastructure.Exports;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using CleanArc.Infrastructure.Persistence.Repositories;
using CleanArc.Infrastructure.Persistence.SeedDatabaseService;
using CleanArc.Infrastructure.Persistence.Services.AI;
using CleanArc.Infrastructure.Persistence.Services;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.RAG;
using CleanArc.Infrastructure.Persistence.Services.Stickers;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.ServiceConfiguration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISeedGameData, SeedGameData>();

        // New repositories
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IClassroomRepository, ClassroomRepository>();
        services.AddScoped<IProgressionRepository, ProgressionRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IChallengeRepository, ChallengeRepository>();
        services.AddScoped<IStickerRepository, StickerRepository>();
        services.AddScoped<IInstitutionUserReportRepository, InstitutionUserReportRepository>();
        services.AddScoped<IAchievementTrackingService, AchievementTrackingService>();
        services.AddScoped<ISyllabusModuleService, SyllabusModuleService>();
        services.AddScoped<ISyllabusModuleIngestionService, SyllabusModuleIngestionService>();
        services.AddScoped<IChallengeGenerator, ChallengeGenerator>();
        services.AddSingleton<IAiPromptRegistry, AiPromptRegistry>();
        services.AddScoped<IAiAuditService, AiAuditService>();
        services.AddScoped<IChallengeAiPipelineService, ChallengeAiPipelineService>();
        services.AddScoped<IRecommendationEngine, RecommendationEngine>();
        services.AddScoped<IMasteryEngine, MasteryEngine>();
        services.AddScoped<IChallengeOrchestrator, ChallengeOrchestrator>();
        services.AddScoped<IClassroomModuleManagementService, ClassroomModuleManagementService>();
        services.AddScoped<IStudentModuleProgressionService, StudentModuleProgressionService>();
        services.AddScoped<IAdaptiveAttemptService, AdaptiveAttemptService>();
        services.AddScoped<IAdaptiveAnalyticsService, AdaptiveAnalyticsService>();
        services.AddScoped<CleanArc.Application.Contracts.Infrastructure.IClassroomGeneratorService, ClassroomGeneratorService>();
        services.AddScoped<CleanArc.Application.Contracts.Infrastructure.IStudentImportService, StudentImportService>();
        services.AddScoped<CleanArc.Application.Contracts.Infrastructure.IRosterPdfGenerator, RosterPdfGenerator>();
        services.AddScoped<CleanArc.Application.Contracts.Infrastructure.IClassroomSetupWizardService, ClassroomSetupWizardService>();

        services.Configure<HuggingFaceStickerOptions>(configuration.GetSection(HuggingFaceStickerOptions.SectionName));
        services.Configure<CloudinaryStickerOptions>(configuration.GetSection(CloudinaryStickerOptions.SectionName));
        services.Configure<OllamaChallengeOptions>(configuration.GetSection(OllamaChallengeOptions.SectionName));
        services.Configure<GoogleAiOptions>(configuration.GetSection(GoogleAiOptions.SectionName));
        services.Configure<RagVectorStoreOptions>(configuration.GetSection(RagVectorStoreOptions.SectionName));

        services.AddHttpClient<IAiGenerationService, GoogleAiService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GoogleAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 60);
        });

        services.AddHttpClient<ITextEmbeddingService, OllamaChallengeOrchestrator>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OllamaChallengeOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds > 0 ? options.RequestTimeoutSeconds : 90);
        });

        services.AddSingleton<IRagVectorStore, SqliteRagVectorStore>();
        services.AddScoped<IRagRetrievalService, RagRetrievalService>();
        services.AddScoped<IChallengeDocumentExtractor, ChallengeDocumentExtractorService>();
        services.AddScoped<IInstitutionUserCsvExportService, CsvExportService>();

        services.AddScoped<IStickerPromptCatalogService, StickerPromptCatalogService>();
        services.AddHttpClient<IStickerImageGenerationService, HuggingFaceStickerImageGenerationService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<HuggingFaceStickerOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds > 0 ? options.RequestTimeoutSeconds : 60);
        });

        services.AddHttpClient<IStickerImageStorageService, CloudinaryStickerImageStorageService>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL"));
        });

        return services;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (context is null)
            throw new Exception("Database Context Not Found");

        await context.Database.MigrateAsync();
    }

    public static async Task SeedGameDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seedGameData = scope.ServiceProvider.GetService<ISeedGameData>();

        if (seedGameData is null)
            throw new Exception("Seed Game Data Service Not Found");

        await seedGameData.Seed();
    }

}
