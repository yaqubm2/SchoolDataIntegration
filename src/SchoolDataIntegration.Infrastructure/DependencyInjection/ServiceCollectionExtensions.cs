using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolDataIntegration.Application;

namespace SchoolDataIntegration.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchoolDataIntegrationInfrastructure(this IServiceCollection services, string configPath)
    {
        var configLoader = new ConfigLoader(configPath);
        services.AddSingleton<IConfigLoader>(configLoader);

        services.AddDbContext<SchoolDbContext>(options =>
            options.UseSqlite(configLoader.Load().Database.ConnectionString));

        services.AddHttpClient<IRestApiClient, RestApiClient>();
        services.AddScoped<IStudentDataRetriever, StudentDataRetriever>();
        services.AddScoped<IFilePersister, FilePersister>();
        services.AddScoped<IStudentBuilder, StudentBuilder>();
        services.AddScoped<IStudentFilter, StudentFilter>();
        services.AddScoped<IStudentTransformer, StudentTransformer>();
        services.AddScoped<IProcessor, Processor>();

        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.AddScoped<IMailService, SmtpMailService>();

        return services;
    }
}
