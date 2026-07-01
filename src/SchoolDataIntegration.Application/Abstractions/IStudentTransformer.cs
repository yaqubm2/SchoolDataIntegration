using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public interface IStudentTransformer
{
    Task<TransformResult> TransformAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default);
}
