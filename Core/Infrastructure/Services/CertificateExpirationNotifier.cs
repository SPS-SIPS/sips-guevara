using Microsoft.EntityFrameworkCore;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Services;

namespace SPSS.Infrastructure.Services;

public sealed class CertificateExpirationNotifier(
    ILogger<CertificateExpirationNotifier> logger,
    IStorageBroker storage,
    ITemplateRenderer templateRenderer,
    IEmailSender emailSender)
{
    public async Task NotifyExpiringCertificates(CancellationToken ct)
    {
        var thresholdDate = DateTimeOffset.UtcNow.AddMonths(1);

        var expiringCertificates = await storage.Certificates
            .Include(c => c.Participant)
            .Where(c => c.Active && !c.IsRevoked && !c.IsExpirationWarningSent && c.ExpireDate <= thresholdDate)
            .ToListAsync(ct);

        foreach (var cert in expiringCertificates)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cert.Participant?.Email))
                {
                    logger.LogWarning("Cannot send expiration warning for certificate {CertId} because participant {ParticipantId} has no email configured.", cert.Id, cert.ParticipantId);
                    continue;
                }

                var values = new Dictionary<string, string>
                {
                    { "ParticipantName", cert.Participant.Name },
                    { "CertificateName", cert.Name },
                    { "BIC", cert.Participant.BicNo },
                    { "ExpirationDate", cert.ExpireDate.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                var body = await templateRenderer.RenderAsync("", "certificate-expiry.html", values, ct);

                await emailSender.SendEmailAsync(
                    toEmail: cert.Participant.Email,
                    subject: "Action Required: Your Certificate is Expiring",
                    body: body,
                    attachmentPath: null,
                    isHTML: true);

                cert.IsExpirationWarningSent = true;
                logger.LogInformation("Successfully sent expiration warning for certificate {CertId} to {Email}", cert.Id, cert.Participant.Email);
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
}
