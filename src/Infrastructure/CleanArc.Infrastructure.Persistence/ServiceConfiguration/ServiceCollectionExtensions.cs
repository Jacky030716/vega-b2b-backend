using CleanArc.Application.Contracts.Persistence;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using CleanArc.Infrastructure.Persistence.Repositories;
using CleanArc.Infrastructure.Persistence.SeedDatabaseService;
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
        services.AddScoped<IQuizContentRepository, QuizContentRepository>();
        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
        services.AddScoped<ISeedQuizData, SeedQuizData>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            //options
            //.UseSqlServer(configuration.GetConnectionString("SqlServer")); 
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

    public static async Task SeedQuizDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seedService = scope.ServiceProvider.GetRequiredService<ISeedQuizData>();
        await seedService.Seed();
    }
}