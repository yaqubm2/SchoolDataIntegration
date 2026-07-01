using SchoolDataIntegration.Infrastructure;
using Xunit;

namespace SchoolDataIntegration.Tests;

public class CsvParserTests
{
    [Fact]
    public void Parse_SimpleCsv_ReturnsRowsKeyedByHeader()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n" +
                   "S1,Ada,Lovelace,10,Active\n" +
                   "S2,Alan,Turing,11,Active\n";

        var rows = CsvParser.Parse(csv);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Ada", rows[0]["FirstName"]);
        Assert.Equal("Turing", rows[1]["LastName"]);
    }

    [Fact]
    public void Parse_HandlesQuotedFieldsWithEmbeddedCommas()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n" +
                   "S1,\"Ada, Countess\",Lovelace,10,Active\n";

        var rows = CsvParser.Parse(csv);

        Assert.Single(rows);
        Assert.Equal("Ada, Countess", rows[0]["FirstName"]);
    }

    [Fact]
    public void Parse_HandlesEscapedQuotesInsideQuotedFields()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n" +
                   "S1,\"Ada \"\"The Enchantress\"\"\",Lovelace,10,Active\n";

        var rows = CsvParser.Parse(csv);

        Assert.Equal("Ada \"The Enchantress\"", rows[0]["FirstName"]);
    }

    [Fact]
    public void Parse_HandlesCrLfAndLfLineEndings()
    {
        var csvCrLf = "ExternalId,FirstName,LastName,YearLevel,Status\r\nS1,Ada,Lovelace,10,Active\r\n";
        var csvLf = "ExternalId,FirstName,LastName,YearLevel,Status\nS1,Ada,Lovelace,10,Active\n";

        Assert.Single(CsvParser.Parse(csvCrLf));
        Assert.Single(CsvParser.Parse(csvLf));
    }

    [Fact]
    public void Parse_MissingTrailingColumns_FillsEmptyString()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n" +
                   "S1,Ada\n";

        var rows = CsvParser.Parse(csv);

        Assert.Equal(string.Empty, rows[0]["LastName"]);
        Assert.Equal(string.Empty, rows[0]["Status"]);
    }

    [Fact]
    public void Parse_HeaderOnly_ReturnsNoRows()
    {
        var csv = "ExternalId,FirstName,LastName,YearLevel,Status\n";

        var rows = CsvParser.Parse(csv);

        Assert.Empty(rows);
    }
}
