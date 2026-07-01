using Microsoft.Extensions.Logging.Abstractions;
using SchoolDataIntegration.Domain;
using SchoolDataIntegration.Infrastructure;
using Xunit;

namespace SchoolDataIntegration.Tests;

public class StudentFilterTests
{
    private readonly StudentFilter _filter = new(NullLogger<StudentFilter>.Instance);

    [Fact]
    public void Filter_KeepsAllRecords_WhenNoDuplicatesOrMissingIds()
    {
        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "S2", FirstName = "Alan", LastName = "Turing", YearLevel = "11", Status = "Active" }
        };

        var result = _filter.Filter("SCH-1", students);

        Assert.Equal(2, result.Students.Count);
        Assert.Equal(0, result.DuplicatesIgnored);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Filter_IgnoresDuplicateExternalIds_KeepingFirstOccurrence()
    {
        var students = new List<StudentModel>
        {
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "S1", FirstName = "Ada", LastName = "StaleLastName", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "S2", FirstName = "Alan", LastName = "Turing", YearLevel = "11", Status = "Active" }
        };

        var result = _filter.Filter("SCH-1", students);

        Assert.Equal(2, result.Students.Count);
        Assert.Equal(1, result.DuplicatesIgnored);
        Assert.Equal("Lovelace", result.Students.Single(s => s.ExternalId == "S1").LastName);
    }

    [Fact]
    public void Filter_DuplicateDetection_IsCaseInsensitiveOnExternalId()
    {
        var students = new List<StudentModel>
        {
            new() { ExternalId = "abc123", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "ABC123", FirstName = "Ada", LastName = "Lovelace", YearLevel = "10", Status = "Active" }
        };

        var result = _filter.Filter("SCH-1", students);

        Assert.Single(result.Students);
        Assert.Equal(1, result.DuplicatesIgnored);
    }

    [Fact]
    public void Filter_DropsRecords_WithMissingExternalId()
    {
        var students = new List<StudentModel>
        {
            new() { ExternalId = "", FirstName = "No", LastName = "Id", YearLevel = "10", Status = "Active" },
            new() { ExternalId = "S2", FirstName = "Alan", LastName = "Turing", YearLevel = "11", Status = "Active" }
        };

        var result = _filter.Filter("SCH-1", students);

        Assert.Single(result.Students);
        Assert.Single(result.ValidationErrors);
        Assert.Equal(0, result.DuplicatesIgnored);
    }

    [Fact]
    public void Filter_EmptyBatch_ReturnsEmptyResult()
    {
        var result = _filter.Filter("SCH-1", new List<StudentModel>());

        Assert.Empty(result.Students);
        Assert.Equal(0, result.DuplicatesIgnored);
        Assert.Empty(result.ValidationErrors);
    }
}
