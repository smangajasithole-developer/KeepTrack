
using keeptrack.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace keeptrack.Pages;

public class EmailTestModel : PageModel
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public EmailTestModel(
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _emailSender = emailSender;
        _configuration = configuration;
    }

    [BindProperty]
    public string RecipientEmail { get; set; } = string.Empty;

    public string? Message { get; set; }

    public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
    {
        if (!_configuration.GetValue<bool>("EmailTest:Enabled") ||
            !_environmentIsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RecipientEmail))
        {
            Message = "Enter a recipient email address.";
            return Page();
        }

        try
        {
            await _emailSender.SendEmailAsync(
                RecipientEmail.Trim(),
                "KeepTrack Email Test",
                """
                <div style="font-family: Arial, sans-serif;">
                    <h2>KeepTrack Email Test</h2>
                    <p>Your email integration is working.</p>
                    <p>KeepTrack successfully sent this message through Brevo SMTP.</p>
                </div>
                """);

            Message = "Email sent successfully. Check the recipient inbox and spam folder.";
        }
        catch (Exception)
        {
            Message = "Email sending failed. Check the application logs for details.";
        }

        return Page();
    }

    private bool _environmentIsDevelopment()
    {
        return HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();
    }
}
