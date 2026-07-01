using SchoolDataIntegration.Api.Events;

namespace SchoolDataIntegration.Api.Mail;

public interface IMailService
{
    Task SendImportSummaryAsync(StudentImportCompletedEvent summary, CancellationToken ct = default);
}
