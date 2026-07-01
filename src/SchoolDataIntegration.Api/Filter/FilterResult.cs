using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Filter;

public class FilterResult
{
    /// <summary>De-duplicated, ready-to-persist student records.</summary>
    public List<StudentModel> Students { get; init; } = new();

    /// <summary>Count of records skipped because they repeated an ExternalId already seen in this same import.</summary>
    public int DuplicatesIgnored { get; init; }

    /// <summary>Count of records skipped because they were missing a required field (e.g. ExternalId).</summary>
    public List<string> ValidationErrors { get; init; } = new();
}
