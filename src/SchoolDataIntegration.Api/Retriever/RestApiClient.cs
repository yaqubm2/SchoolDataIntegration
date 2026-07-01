using System.Net.Http.Headers;
using System.Text;
using SchoolDataIntegration.Api.Config;

namespace SchoolDataIntegration.Api.Retriever;

public class RestApiClient : IRestApiClient
{
    #region private fields

    private readonly HttpClient _httpClient;
    private readonly IConfigLoader _configLoader;
    private readonly ILogger<RestApiClient> _logger;

    #endregion

    #region constructor

    public RestApiClient(HttpClient httpClient, IConfigLoader configLoader, ILogger<RestApiClient> logger)
    {
        _httpClient = httpClient;
        _configLoader = configLoader;
        _logger = logger;
    }

    #endregion

    #region public Methods

    public async Task<string> GetStudentsJsonAsync(string schoolId, CancellationToken ct = default)
    {
        var config = _configLoader.Load().RestApi;

        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            throw new InvalidOperationException(
                "RestApi.BaseUrl is not configured in config.xml; cannot retrieve student data.");
        }

        var requestUri = $"{config.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(schoolId)}/students";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var basicAuthValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthValue);

        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30);

        _logger.LogInformation("Retrieving students for school {SchoolId} from REST API {RequestUri}", schoolId, requestUri);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }

    #endregion

}
