using System.Net;
using System.Net.Mail;

namespace SPSS.Infrastructure.Services;
public class EmailSender(EmailConfig config, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly EmailConfig _config = config;
    private readonly ILogger<EmailSender> _logger = logger;

    public async Task SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null, bool isHTML = false)
    {
        var smtpClient = new SmtpClient(_config.Server)
        {
            Port = _config.Port,
            Credentials = new NetworkCredential(_config.Username, _config.Password),
            EnableSsl = _config.IsSSL,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_config.Username),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHTML,
        };

        mailMessage.To.Add(toEmail);

        if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
        {
            mailMessage.Attachments.Add(new Attachment(attachmentPath));
        }

        await smtpClient.SendMailAsync(mailMessage);

        _logger.LogInformation("Email sent to {toEmail} with subject {subject} and body {body}", toEmail, subject, body);
    }
}
