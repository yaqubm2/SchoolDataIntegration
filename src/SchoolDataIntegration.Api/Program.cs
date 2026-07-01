using Microsoft.EntityFrameworkCore;
using SchoolDataIntegration.Api.Builder;
using SchoolDataIntegration.Api.Config;
using SchoolDataIntegration.Api.Data;
using SchoolDataIntegration.Api.Events;
using SchoolDataIntegration.Api.Filter;
using SchoolDataIntegration.Api.Mail;
using SchoolDataIntegration.Api.Models;
using SchoolDataIntegration.Api.Processing;
using SchoolDataIntegration.Api.Retriever;
using SchoolDataIntegration.Api.Transformer;

namespace SchoolDataIntegration.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configLoader = RegisterServices(builder.Services);

        var app = builder.Build();

        await EnsureDatabaseCreatedAsync(app.Services);
        SubscribeToImportCompletedEvent(app.Services);
        MapEndpoints(app);

        await app.RunAsync();
    }

    private static ConfigLoader RegisterServices(IServiceCollection services)
    {
        // --- Configuration -----------------------------------------------------
        // Business/integration config lives in XML (config.xml), separate from
        // ASP.NET Core's own host config (appsettings.json) used for Kestrel/logging.
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "config.xml");
        var configLoader = new ConfigLoader(configPath);
        services.AddSingleton<IConfigLoader>(configLoader);

        // --- Database ------------------------------------------------------------
        services.AddDbContext<SchoolDbContext>(options =>
            options.UseSqlite(configLoader.Load().Database.ConnectionString));

        // --- Pipeline stages -------------------------------------------------------
        services.AddHttpClient<IRestApiClient, RestApiClient>();
        services.AddScoped<IStudentDataRetriever, StudentDataRetriever>();
        services.AddScoped<IFilePersister, FilePersister>();
        services.AddScoped<IStudentBuilder, StudentBuilder>();
        services.AddScoped<IStudentFilter, StudentFilter>();
        services.AddScoped<IStudentTransformer, StudentTransformer>();
        services.AddScoped<IProcessor, Processor>();

        // --- Events / notifications ------------------------------------------------
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.AddScoped<IMailService, SmtpMailService>();

        return configLoader;
    }

    private static async Task EnsureDatabaseCreatedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private static void SubscribeToImportCompletedEvent(IServiceProvider services)
    {
        var eventPublisher = services.GetRequiredService<IEventPublisher>();
        eventPublisher.Subscribe<StudentImportCompletedEvent>(async (evt, ct) =>
        {
            using var scope = services.CreateScope();
            var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
            await mailService.SendImportSummaryAsync(evt, ct);
        });
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/student-imports", HandleStudentImportAsync);
    }

    private static async Task<IResult> HandleStudentImportAsync(
        StudentImportRequest request, IProcessor processor, CancellationToken ct)
    {
        try
        {
            var summary = await processor.ProcessAsync(request, ct);
            return Results.Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Student import could not be completed");
        }
    }
}