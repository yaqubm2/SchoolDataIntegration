using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Filter;

public interface IStudentFilter
{
    FilterResult Filter(string schoolId, IReadOnlyCollection<StudentModel> students);
}
