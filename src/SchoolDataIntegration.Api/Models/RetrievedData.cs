namespace SchoolDataIntegration.Api.Models;

public enum DataFormat
{
    Csv,
    Json
}

public enum DataSourceKind
{
    LocalCsvDrop,
    RestApi
}
public class RetrievedData
{
    public required string Content { get; init; }

    public required DataFormat Format { get; init; }

    public required DataSourceKind Source { get; init; }
}
