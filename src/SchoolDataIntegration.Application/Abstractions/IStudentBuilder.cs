using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public interface IStudentBuilder
{
    List<StudentModel> Build(RetrievedData data);
}
