
using keeptrack.Configuration;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace keeptrack.Services;

public class BrevoEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        IOptions<EmailSettings> options,
        ILogger<BrevoEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpUsername) ||
            string.IsNullOrWhiteSpace(_settings.SmtpPassword) ||
            string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "Email settings are incomplete. Check your User Secrets configuration.");
        }

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        message.To.Add(
            MailboxAddress.Parse(recipientEmail));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();

        try
        {
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.SmtpUsername,
                _settings.SmtpPassword);

            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {RecipientEmail}.",
                recipientEmail);

            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
