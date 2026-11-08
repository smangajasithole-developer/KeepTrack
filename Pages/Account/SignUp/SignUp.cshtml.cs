
using System.ComponentModel.DataAnnotations;
using keeptrack.Models;
using keeptrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace keeptrack.Pages.Account.SignUp;

public class SignUpModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SignUpModel> _logger;

    public SignUpModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<SignUpModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public SignUpInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class SignUpInput
    {
        [Required(ErrorMessage = "Please enter your first name.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please enter your last name.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your gender.")]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a password.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        if (!Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }
    }

    public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
    {
        // Only permit redirects to local application URLs.
        if (!Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();

        var username = string.IsNullOrWhiteSpace(Input.Username)
            ? email
            : Input.Username.Trim();

        var user = new ApplicationUser
        {
            FirstName = Input.FirstName.Trim(),

            MiddleName = string.IsNullOrWhiteSpace(Input.MiddleName)
                ? null
                : Input.MiddleName.Trim(),

            LastName = Input.LastName.Trim(),

            Gender = Input.Gender.Trim(),

            UserName = username,

            Email = email
        };

        var result = await _userManager.CreateAsync(
            user,
            Input.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // Send a welcome email without blocking registration.
            try
            {
                _logger.LogInformation(
                    "Attempting to send welcome email for user {UserId}.",
                    user.Id);

                var firstName = System.Net.WebUtility.HtmlEncode(
                    user.FirstName);

                await _emailSender.SendEmailAsync(
                    email,
                    "Welcome to KeepTrack!",
                    $"""
                    <div style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
                        <h2>Welcome to KeepTrack, {firstName}!</h2>

                        <p>Your account has been created successfully.</p>

                        <p>KeepTrack helps you organise your tasks and keep track
                        of what needs to get done.</p>

                        <p>You can now sign in and start managing your tasks.</p>

                        <p>Thank you for joining KeepTrack!</p>

                        <p>Best regards,<br>
                        The KeepTrack Team</p>
                    </div>
                    """);

                _logger.LogInformation(
                    "Welcome email sent successfully for user {UserId}.",
                    user.Id);
            }
            catch (Exception ex)
            {
                // Email failure must not prevent successful registration.
                _logger.LogError(
                    ex,
                    "Welcome email failed for user {UserId}.",
                    user.Id);
            }

            // Return to the requested local page when supplied.
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            // Keep normal registration behaviour unchanged.
            return RedirectToPage("/Dashboard/Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }
}


