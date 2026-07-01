using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public interface IStudentFilter
{
    FilterResult Filter(string schoolId, IReadOnlyCollection<StudentModel> students);
}
