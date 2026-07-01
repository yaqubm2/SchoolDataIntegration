namespace SchoolDataIntegration.Domain;

public class StudentImportCompletedEvent
{
    #region Properties
    public required Guid ImportId { get; init; }
    public required string SchoolId { get; init; }
    public required int Total { get; init; }
    public required int Success { get; init; }
    public required int Failed { get; init; }
    public required int Duplicates { get; init; }
    public required string Status { get; init; }
    public required DateTime CompletedAt { get; init; }

    #endregion
}
