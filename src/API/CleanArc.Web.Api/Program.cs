using System.Diagnostics;
using System.Text.Json;
using Asp.Versioning.Builder;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Models.Common;
using CleanArc.Application.ServiceConfiguration;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.CrossCutting.Logging;
using CleanArc.Infrastructure.Identity.Identity.Dtos;
using CleanArc.Infrastructure.Identity.Identity.SeedDatabaseService;
using CleanArc.Infrastructure.Identity.Jwt;
using CleanArc.Infrastructure.Identity.ServiceConfiguration;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.ServiceConfiguration;
using CleanArc.SharedKernel.Extensions;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using CleanArc.Web.Api.Controllers.V1.UserManagement;
using CleanArc.Web.Plugins.Grpc;
using CleanArc.WebFramework.EndpointFilters;
using CleanArc.WebFramework.Filters;
using CleanArc.WebFramework.Middlewares;
using CleanArc.WebFramework.ServiceConfiguration;
using CleanArc.WebFramework.Swagger;
using CleanArc.WebFramework.WebExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Serilog;

IdentityModelEventSource.ShowPII = true;
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggingConfiguration.ConfigureLogger);

var configuration = builder.Configuration;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;

builder.Services.Configure<IdentitySettings>(configuration.GetSection(nameof(IdentitySettings)));

var identitySettings = configuration.GetSection(nameof(IdentitySettings)).Get<IdentitySettings>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(OkResultAttribute));
    options.Filters.Add(typeof(NotFoundResultAttribute));
    options.Filters.Add(typeof(ContentResultFilterAttribute));
    options.Filters.Add(typeof(ModelStateValidationAttribute));
    options.Filters.Add(typeof(BadRequestResultFilterAttribute));

}).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
    options.SuppressMapClientErrors = true;
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddCarter(configurator: configurator => { configurator.WithEmptyValidators(); });

builder.Services.AddApplicationServices()
    .RegisterIdentityServices(identitySettings)
    .AddPersistenceServices(configuration)
    .AddWebFrameworkServices();

builder.Services.RegisterValidatorsAsServices();
builder.Services.AddExceptionHandler<ExceptionHandler>();


#region Plugin Services Configuration

builder.Services.ConfigureGrpcPluginServices();

#endregion

builder.Services.AddAutoMapper(expression =>
{
    expression.AddMaps(typeof(User), typeof(JwtService), typeof(UserController));
});

var app = builder.Build();


await app.ApplyMigrationsAsync();

if (args.Length >= 2 && args[0].Equals("--seed-syllabus", StringComparison.OrdinalIgnoreCase))
{
    var seedPath = ResolveSeedPath(args[1], app.Environment.ContentRootPath);
    if (!File.Exists(seedPath))
        throw new FileNotFoundException("Syllabus seed file not found.", seedPath);

    await using var scope = app.Services.CreateAsyncScope();
    var ingestionService = scope.ServiceProvider.GetRequiredService<ISyllabusModuleIngestionService>();
    await using var seedStream = File.OpenRead(seedPath);
    var document = await JsonSerializer.DeserializeAsync<SyllabusSeedDocument>(
        seedStream,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (document is null)
        throw new InvalidOperationException("Syllabus seed file is empty or invalid.");

    var result = await ingestionService.IngestAsync(document, CancellationToken.None);
    Console.WriteLine($"Syllabus seed completed. Modules created: {result.ModulesCreated}, modules updated: {result.ModulesUpdated}, items created: {result.ItemsCreated}, items updated: {result.ItemsUpdated}, items rejected: {result.ItemsRejected}.");
    foreach (var line in result.Logs)
        Console.WriteLine(line);
    foreach (var error in result.Errors)
        Console.Error.WriteLine(error);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Server Error"
        });
    });
});
app.UseSwaggerAndUI();

app.MapCarter();
app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.ConfigureGrpcPipeline();

await app.RunAsync();

static string ResolveSeedPath(string inputPath, string contentRootPath)
{
    if (Path.IsPathRooted(inputPath))
        return Path.GetFullPath(inputPath);

    var candidates = new[]
    {
        Path.GetFullPath(inputPath),
        Path.GetFullPath(Path.Combine(contentRootPath, inputPath)),
        Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "..", inputPath))
    };

    return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
}


