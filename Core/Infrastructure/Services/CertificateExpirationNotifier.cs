using Microsoft.EntityFrameworkCore;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Services;

namespace SPSS.Infrastructure.Services;

public sealed class CertificateExpirationNotifier(
    ILogger<CertificateExpirationNotifier> logger,
    IStorageBroker storage,
    ITemplateRenderer templateRenderer,
    IEmailSender emailSender,
    EmailConfig emailConfig,
    CertificateExpirationNotificationConfig notificationConfig)
{
    private const string TemplateFileName = "certificate-expiry.html";
    private const string EmailSubject = "Action Required: Your Certificate is Expiring";

    public async Task NotifyExpiringCertificates(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var thresholdDate = now.AddMonths(1);

        var expiringCertificates = await storage.Certificates
            .Include(c => c.Participant)
            .Where(c => c.Active
                && !c.IsRevoked
                && !c.IsExpirationWarningSent
                && c.ExpireDate > now
                && c.ExpireDate <= thresholdDate)
            .ToListAsync(ct);

        foreach (var cert in expiringCertificates)
        {
            try
            {
                var certificateId = cert.Id ?? cert.Subject;
                var values = new Dictionary<string, string>
                {
                    { "ParticipantName", cert.Participant?.Name ?? cert.ParticipantId },
                    { "CertificateName", cert.Subject },
                    { "BIC", cert.Participant?.BicNo ?? cert.ParticipantId },
                    { "ExpirationDate", cert.ExpireDate.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "DaysUntilExpiration", Math.Ceiling((cert.ExpireDate - now).TotalDays).ToString("0") }
                };

                var body = await templateRenderer.RenderAsync(emailConfig.TemplatesDirectory, TemplateFileName, values, ct);
                var warningSent = false;

                if (string.IsNullOrWhiteSpace(cert.Participant?.Email))
                {
                    logger.LogWarning("Cannot send participant expiration warning for certificate {CertId} because participant {ParticipantId} has no email configured.", cert.Id, cert.ParticipantId);
                }
                else
                {
                    warningSent |= await TrySendExpirationWarning(certificateId, cert.Participant.Email, body);
                }

                foreach (var itRecipient in notificationConfig.ITRecipients
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    warningSent |= await TrySendExpirationWarning(certificateId, itRecipient, body);
                }

                if (!warningSent)
                {
                    logger.LogWarning("No expiration warning email was sent for certificate {CertId}.", cert.Id);
                    continue;
                }

                cert.IsExpirationWarningSent = true;
                logger.LogInformation("Successfully sent expiration warning for certificate {CertId}", cert.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send expiration warning for certificate {CertId}", cert.Id);
            }
        }

        if (expiringCertificates.Any())
        {
            await storage.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> TrySendExpirationWarning(string certificateId, string toEmail, string body)
    {
        try
        {
            await emailSender.SendEmailAsync(
                toEmail: toEmail,
                subject: EmailSubject,
                body: body,
                attachmentPath: null,
                isHTML: true);

            logger.LogInformation("Successfully sent expiration warning for certificate {CertId} to {Email}", certificateId, toEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send expiration warning for certificate {CertId} to {Email}", certificateId, toEmail);
            return false;
        }
    }
}
