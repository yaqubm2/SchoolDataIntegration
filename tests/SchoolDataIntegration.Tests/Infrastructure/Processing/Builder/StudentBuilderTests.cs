using SchoolDataIntegration.Application;
using SchoolDataIntegration.Domain;
using SchoolDataIntegration.Infrastructure;
using Xunit;

namespace SchoolDataIntegration.Tests;

public class StudentBuilderTests
{
    private readonly StudentBuilder _builder = new();

    [Fact]
    public void Build_FromCsv_ParsesAllFields()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n" +
                   "S1,Ada,Lovelace,10,Active\n";

        var data = new RetrievedData { Content = csv, Format = DataFormat.Csv, Source = DataSourceKind.LocalCsvDrop };

        var students = _builder.Build(data);

        Assert.Single(students);
        Assert.Equal("S1", students[0].ExternalId);
        Assert.Equal("Ada", students[0].FirstName);
        Assert.Equal("Lovelace", students[0].LastName);
        Assert.Equal("10", students[0].YearLevel);
        Assert.Equal("Active", students[0].Status);
    }

    [Fact]
    public void Build_FromJsonArray_ParsesAllFields()
    {
        var json = """
        [
            { "externalId": "S1", "firstName": "Ada", "lastName": "Lovelace", "yearLevel": "10", "status": "Active" }
        ]
        """;

        var data = new RetrievedData { Content = json, Format = DataFormat.Json, Source = DataSourceKind.RestApi };

        var students = _builder.Build(data);

        Assert.Single(students);
        Assert.Equal("S1", students[0].ExternalId);
        Assert.Equal("Ada", students[0].FirstName);
    }

    [Fact]
    public void Build_FromJsonObjectWithStudentsProperty_ParsesAllFields()
    {
        var json = """
        {
            "students": [
                { "externalId": "S1", "firstName": "Ada", "lastName": "Lovelace", "yearLevel": "10", "status": "Active" }
            ]
        }
        """;

        var data = new RetrievedData { Content = json, Format = DataFormat.Json, Source = DataSourceKind.RestApi };

        var students = _builder.Build(data);

        Assert.Single(students);
        Assert.Equal("S1", students[0].ExternalId);
    }

    [Fact]
    public void Build_FromEmptyJson_ReturnsEmptyList()
    {
        var data = new RetrievedData { Content = "", Format = DataFormat.Json, Source = DataSourceKind.RestApi };

        var students = _builder.Build(data);

        Assert.Empty(students);
    }
}
