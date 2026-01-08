namespace CrudDemo.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    Task SendRegistrationEmailAsync(string toEmail, string userName);
    Task SendSubscriptionEmailAsync(string toEmail, string userName, string courseName, decimal amount);
}
