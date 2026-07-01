using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Retriever;

public interface IFilePersister
{
    Task PersistAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default);
}
