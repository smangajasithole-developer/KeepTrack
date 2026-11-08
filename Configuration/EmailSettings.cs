
namespace keeptrack.Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "KeepTrack";
}
