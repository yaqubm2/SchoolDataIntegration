using System.Text.Json;
using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Builder;

public class StudentBuilder : IStudentBuilder
{
    #region private static field
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #endregion

    #region public method

    public List<StudentModel> Build(RetrievedData data)
    {
        return data.Format switch
        {
            DataFormat.Csv => BuildFromCsv(data.Content),
            DataFormat.Json => BuildFromJson(data.Content),
            _ => throw new NotSupportedException($"Unsupported data format: {data.Format}")
        };
    }

    #endregion

    #region private method

    private static List<StudentModel> BuildFromCsv(string csvContent)
    {
        var rows = CsvParser.Parse(csvContent);
        var students = new List<StudentModel>(rows.Count);

        foreach (var row in rows)
        {
            students.Add(new StudentModel
            {
                ExternalId = row.GetValueOrDefault("ExternalId", string.Empty).Trim(),
                FirstName = row.GetValueOrDefault("FirstName", string.Empty).Trim(),
                LastName = row.GetValueOrDefault("LastName", string.Empty).Trim(),
                YearLevel = row.GetValueOrDefault("YearLevel", string.Empty).Trim(),
                Status = row.GetValueOrDefault("Status", string.Empty).Trim()
            });
        }

        return students;
    }

    private static List<StudentModel> BuildFromJson(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new List<StudentModel>();
        }

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        var arrayElement = root.ValueKind switch
        {
            JsonValueKind.Array => root,
            JsonValueKind.Object when root.TryGetProperty("students", out var nested) => nested,
            _ => throw new JsonException("Expected a JSON array of students, or an object with a 'students' array property.")
        };

        var students = new List<StudentModel>();
        foreach (var element in arrayElement.EnumerateArray())
        {
            var dto = element.Deserialize<StudentModel>(JsonOptions) ?? new StudentModel();
            students.Add(dto);
        }

        return students;
    }

    #endregion
}
