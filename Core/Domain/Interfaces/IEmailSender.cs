namespace SPSS.Domain.Interfaces;
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHTML = false);
}