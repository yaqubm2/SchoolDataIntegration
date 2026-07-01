using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public class RetrievedData
{
    public required string Content { get; init; }

    public required DataFormat Format { get; init; }

    public required DataSourceKind Source { get; init; }
}
