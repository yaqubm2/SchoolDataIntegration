using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Transformer;

public interface IStudentTransformer
{
    Task<TransformResult> TransformAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default);
}
