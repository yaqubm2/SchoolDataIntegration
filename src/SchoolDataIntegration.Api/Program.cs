using SchoolDataIntegration.Application;
using SchoolDataIntegration.Domain;
using SchoolDataIntegration.Infrastructure;

namespace SchoolDataIntegration.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        RegisterServices(builder.Services);

        var app = builder.Build();

        await EnsureDatabaseCreatedAsync(app.Services);
        SubscribeToImportCompletedEvent(app.Services);
        MapEndpoints(app);

        await app.RunAsync();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "config.xml");

        // All processors (retriever, builder, filter, transformer, mail,
        // events, EF Core) and the IProcessor use case they implement live
        // in Infrastructure; the Api project only wires it up.
        services.AddSchoolDataIntegrationInfrastructure(configPath);
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
