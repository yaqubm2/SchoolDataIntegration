namespace SchoolDataIntegration.Api.Events;

/// <summary>
/// Published once an import run finishes (successfully, partially, or
/// entirely failed). Subscribers (currently: the mail notifier) use this to
/// react without the Processor needing to know who's listening.
/// </summary>
public class StudentImportCompletedEvent
{
    public required Guid ImportId { get; init; }
    public required string SchoolId { get; init; }
    public required int Total { get; init; }
    public required int Success { get; init; }
    public required int Failed { get; init; }
    public required int Duplicates { get; init; }
    public required string Status { get; init; }
    public required DateTime CompletedAt { get; init; }
}
