namespace SchoolDataIntegration.Application;

public interface IRestApiClient
{
    Task<string> GetStudentsJsonAsync(string schoolId, CancellationToken ct = default);
}
