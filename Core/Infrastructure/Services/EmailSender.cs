using System.Net;
using System.Net.Mail;

namespace SPSS.Infrastructure.Services;
public class EmailSender(EmailConfig config, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly EmailConfig _config = config;
    private readonly ILogger<EmailSender> _logger = logger;

    public async Task SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null, bool isHTML = false)
    {
        using var smtpClient = new SmtpClient(_config.Server)
        {
            Port = _config.Port,
            Credentials = new NetworkCredential(_config.Username, _config.Password),
            EnableSsl = _config.IsSSL,
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_config.Username),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHTML,
        };
        
        mailMessage.To.Add(toEmail);

        foreach (var cc in (_config.CC ?? string.Empty)
                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(x => !string.Equals(x, toEmail, StringComparison.OrdinalIgnoreCase)))
        {
            mailMessage.CC.Add(new MailAddress(cc));
        }

        if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
        {
            var attachment = new Attachment(attachmentPath);
            mailMessage.Attachments.Add(attachment);
        }

        try
        {
            _logger.LogInformation("SMTP sending: server={Server}, port={Port}, ssl={SSL}, from={From}, to={To}, ccCount={CcCount}, subject={Subject}, attachment={Attachment}",
                _config.Server, _config.Port, _config.IsSSL, _config.Username, toEmail, mailMessage.CC.Count, subject, Path.GetFileName(attachmentPath ?? string.Empty));

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("SMTP sent OK: to={To}, subject={Subject}, bodyLength={BodyLength}", toEmail, subject, body?.Length ?? 0);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP send failed: statusCode={StatusCode}, server={Server}, port={Port}, ssl={SSL}, from={From}, to={To}, subject={Subject}",
                ex.StatusCode, _config.Server, _config.Port, _config.IsSSL, _config.Username, toEmail, subject);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed: server={Server}, port={Port}, ssl={SSL}, from={From}, to={To}, subject={Subject}",
                _config.Server, _config.Port, _config.IsSSL, _config.Username, toEmail, subject);
            throw;
        }
    }
}
