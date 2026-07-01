using SchoolDataIntegration.Api.Config;
using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Retriever;

public class StudentDataRetriever : IStudentDataRetriever
{
    #region private fields
    private readonly IConfigLoader _configLoader;
    private readonly IRestApiClient _restApiClient;
    private readonly ILogger<StudentDataRetriever> _logger;
    #endregion

    #region constructor

    public StudentDataRetriever(IConfigLoader configLoader, IRestApiClient restApiClient, ILogger<StudentDataRetriever> logger)
    {
        _configLoader = configLoader;
        _restApiClient = restApiClient;
        _logger = logger;
    }

    #endregion

    #region public method
    public async Task<RetrievedData> RetrieveAsync(string schoolId, CancellationToken ct = default)
    {
        var csvPath = Path.Combine(_configLoader.Load().LocalCsvDropFolder, $"{schoolId}.csv");

        if (File.Exists(csvPath))
        {
            _logger.LogInformation("Found local CSV drop for school {SchoolId} at {Path}", schoolId, csvPath);
            var content = await File.ReadAllTextAsync(csvPath, ct);
            return new RetrievedData
            {
                Content = content,
                Format = DataFormat.Csv,
                Source = DataSourceKind.LocalCsvDrop
            };
        }

        _logger.LogInformation("No local CSV drop found for school {SchoolId} at {Path}; falling back to REST API", schoolId, csvPath);
        var json = await _restApiClient.GetStudentsJsonAsync(schoolId, ct);
        return new RetrievedData
        {
            Content = json,
            Format = DataFormat.Json,
            Source = DataSourceKind.RestApi
        };
    }

    #endregion

}
