namespace SchoolDataIntegration.Application;

public class ImportSummary
{
    #region Properties
    public Guid ImportId { get; set; }

    public string SchoolId { get; set; } = string.Empty;

    public int Total { get; set; }

    public int Success { get; set; }

    public int Failed { get; set; }

    public int Duplicates { get; set; }

    public string Status { get; set; } = string.Empty;

    #endregion
}
