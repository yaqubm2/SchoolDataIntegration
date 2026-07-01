using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Builder;

public interface IStudentBuilder
{
    List<StudentModel> Build(RetrievedData data);
}
