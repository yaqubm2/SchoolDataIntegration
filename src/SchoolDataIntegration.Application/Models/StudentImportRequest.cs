using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public class StudentImportRequest
{
    public string SchoolId { get; set; } = string.Empty;

    public List<StudentModel>? Students { get; set; }
}
