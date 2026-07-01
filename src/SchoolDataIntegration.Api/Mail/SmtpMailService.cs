using System.Net;
using System.Net.Mail;
using SchoolDataIntegration.Api.Config;
using SchoolDataIntegration.Api.Events;

namespace SchoolDataIntegration.Api.Mail;

/// <summary>
/// Sends the post-import summary email. Recipient/SMTP settings come from
/// config.xml. When Mail.DryRun is true (the safe default), the email is
/// logged instead of actually sent - handy for local/dev environments
/// without a real SMTP relay configured.
/// </summary>
public class SmtpMailService : IMailService
{
    private readonly IConfigLoader _configLoader;
    private readonly ILogger<SmtpMailService> _logger;

    public SmtpMailService(IConfigLoader configLoader, ILogger<SmtpMailService> logger)
    {
        _configLoader = configLoader;
        _logger = logger;
    }

    public async Task SendImportSummaryAsync(StudentImportCompletedEvent summary, CancellationToken ct = default)
    {
        var config = _configLoader.Load().Mail;

        var subject = $"Student import {summary.Status} - school {summary.SchoolId} ({summary.ImportId})";
        var body =
            $"Import {summary.ImportId} for school {summary.SchoolId} completed at {summary.CompletedAt:u}.\n\n" +
            $"Total:      {summary.Total}\n" +
            $"Successful: {summary.Success}\n" +
            $"Failed:     {summary.Failed}\n" +
            $"Duplicates: {summary.Duplicates}\n" +
            $"Status:     {summary.Status}\n";

        if (config.DryRun || string.IsNullOrWhiteSpace(config.ToAddress) || string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            _logger.LogInformation(
                "[DryRun mail] To={ToAddress} Subject={Subject}\n{Body}",
                config.ToAddress, subject, body);
            return;
        }

        using var message = new MailMessage(config.FromAddress, config.ToAddress, subject, body);
        using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
        {
            EnableSsl = config.EnableSsl,
            Credentials = CredentialCache.DefaultNetworkCredentials
        };

        try
        {
            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Sent import summary email for import {ImportId} to {ToAddress}", summary.ImportId, config.ToAddress);
        }
        catch (Exception ex)
        {
            // Notification failure must never surface as an import failure -
            // the import itself already succeeded/failed independently of
            // whether we could tell anyone about it.
            _logger.LogError(ex, "Failed to send import summary email for import {ImportId}", summary.ImportId);
        }
    }
}
