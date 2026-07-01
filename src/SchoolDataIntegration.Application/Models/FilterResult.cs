using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public class FilterResult
{
    public List<StudentModel> Students { get; init; } = new();
    
    public int DuplicatesIgnored { get; init; }
    
    public List<string> ValidationErrors { get; init; } = new();
}
