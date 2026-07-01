using Microsoft.Extensions.Logging;
using SchoolDataIntegration.Application;
using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Infrastructure;

public class StudentFilter : IStudentFilter
{
    #region private field
    private readonly ILogger<StudentFilter> _logger;

    #endregion

    #region constructor
    public StudentFilter(ILogger<StudentFilter> logger)
    {
        _logger = logger;
    }

    #endregion

    #region public method

    public FilterResult Filter(string schoolId, IReadOnlyCollection<StudentModel> students)
    {
        var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<StudentModel>(students.Count);
        var validationErrors = new List<string>();
        var duplicatesIgnored = 0;

        foreach (var student in students)
        {
            if (string.IsNullOrWhiteSpace(student.ExternalId))
            {
                validationErrors.Add(
                    $"Record for '{student.FirstName} {student.LastName}' skipped: missing ExternalId.");
                continue;
            }

            var duplicatekey = student.ExternalId.Trim();

            if (!seenExternalIds.Add(duplicatekey))
            {
                duplicatesIgnored++;
                _logger.LogWarning(
                    "Ignoring duplicate student record for school {SchoolId}, ExternalId {ExternalId} within the same import batch",
                    schoolId, duplicatekey);
                continue;
            }

            kept.Add(student);
        }

        return new FilterResult
        {
            Students = kept,
            DuplicatesIgnored = duplicatesIgnored,
            ValidationErrors = validationErrors
        };
    }

    #endregion
}
