namespace SchoolDataIntegration.Api.Retriever;

public interface IRestApiClient
{
    Task<string> GetStudentsJsonAsync(string schoolId, CancellationToken ct = default);
}
