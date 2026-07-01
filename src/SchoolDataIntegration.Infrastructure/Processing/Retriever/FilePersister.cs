using System.Text;
using SchoolDataIntegration.Application;
using Microsoft.Extensions.Logging;
using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Infrastructure;

public class FilePersister : IFilePersister
{
    #region private fields
    private readonly IConfigLoader _configLoader;
    private readonly ILogger<FilePersister> _logger;

    #endregion

    #region constructors

    public FilePersister(IConfigLoader configLoader, ILogger<FilePersister> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    #endregion

    #region public method

    public async Task PersistAsync(string schoolId, IReadOnlyCollection<StudentModel> students, CancellationToken ct = default)
    {
        var folder = _configLoader.Load().LocalCsvDropFolder;

        try
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{schoolId}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("ExternalId,FirstName,LastName,YearLevel,Status");
            foreach (var s in students)
            {
                sb.AppendLine(string.Join(',',
                    CsvEscape(s.ExternalId),
                    CsvEscape(s.FirstName),
                    CsvEscape(s.LastName),
                    CsvEscape(s.YearLevel),
                    CsvEscape(s.Status)));
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
            _logger.LogInformation("Cached {Count} REST-sourced student records to {Path}", students.Count, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache REST-sourced student data for school {SchoolId} to disk", schoolId);
        }
    }

    #endregion

    #region private methods

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuoting)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    #endregion
}
