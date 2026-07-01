namespace SchoolDataIntegration.Api.Models;

public class ImportRecord
{
    #region constructor
    public ImportRecord()
    {
        ImportId = Guid.NewGuid();
        SchoolId = string.Empty;
    }

    #endregion

    #region properties
    public Guid ImportId { get; set; }

    public string SchoolId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int TotalRecords { get; set; }

    public int SuccessfulRecords { get; set; }

    public int FailedRecords { get; set; }

    public int DuplicateRecords { get; set; }

    public ImportStatus Status { get; set; } = ImportStatus.Processing;

    public string? ErrorSummary { get; set; }

    #endregion

}
