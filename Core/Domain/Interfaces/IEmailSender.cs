namespace SPSS.Domain.Interfaces;
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body, string? attachmentPath = null, bool isHTML = false);
}