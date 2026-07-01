using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public interface IFilePersister
{
    Task PersistAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default);
}
