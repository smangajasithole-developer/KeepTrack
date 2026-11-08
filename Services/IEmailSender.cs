
namespace keeptrack.Services;

public interface IEmailSender
{
    System.Threading.Tasks.Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody);
}
