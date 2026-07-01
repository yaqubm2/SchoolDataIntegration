namespace SchoolDataIntegration.Api.Models;

/// <summary>Response body returned from POST /student-imports.</summary>
public class ImportSummary
{
    public Guid ImportId { get; set; }

    public string SchoolId { get; set; } = string.Empty;

    public int Total { get; set; }

    public int Success { get; set; }

    public int Failed { get; set; }

    public int Duplicates { get; set; }

    public string Status { get; set; } = string.Empty;
}
