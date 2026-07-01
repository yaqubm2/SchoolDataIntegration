
namespace SchoolDataIntegration.Application;

public interface IProcessor
{
    Task<ImportSummary> ProcessAsync(StudentImportRequest request, CancellationToken ct = default);
}
