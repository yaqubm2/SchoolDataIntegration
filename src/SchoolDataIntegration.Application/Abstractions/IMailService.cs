using SchoolDataIntegration.Domain;

namespace SchoolDataIntegration.Application;

public interface IMailService
{
    Task SendImportSummaryAsync(StudentImportCompletedEvent summary, CancellationToken ct = default);
}
