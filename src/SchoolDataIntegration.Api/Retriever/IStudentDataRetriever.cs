using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Retriever;

public interface IStudentDataRetriever
{
    Task<RetrievedData> RetrieveAsync(string schoolId, CancellationToken ct = default);
}
