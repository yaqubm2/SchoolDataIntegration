using SchoolDataIntegration.Api.Models;

namespace SchoolDataIntegration.Api.Processing;

public interface IProcessor
{
    Task<ImportSummary> ProcessAsync(StudentImportRequest request, CancellationToken ct = default);
}
