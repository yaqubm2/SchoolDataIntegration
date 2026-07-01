namespace SchoolDataIntegration.Application;

public interface IStudentDataRetriever
{
    Task<RetrievedData> RetrieveAsync(string schoolId, CancellationToken ct = default);
}
