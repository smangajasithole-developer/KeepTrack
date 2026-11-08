
using keeptrack.Data;
using keeptrack.Models;
using keeptrack.Pages.Account.ForgotPassword;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace keeptrack.Pages.Account.SignIn;

public class SignInModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PostgresKeepTrackDbContext _context;

    public SignInModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        PostgresKeepTrackDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public SignInInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class SignInInput
    {
        [Required(
            ErrorMessage = "Please enter your email or username.")]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
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
        // Prevent redirects to external websites.
        if (!Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = null;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var loginValue = Input.EmailOrUsername.Trim();

        ApplicationUser? user = null;

        // First, try to find the account by email.
        if (loginValue.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(loginValue);
        }

        // If no email match exists, try the username.
        if (user == null)
        {
            user = await _userManager.FindByNameAsync(loginValue);
        }

        // No account matches the supplied email or username.
        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Account does not exist.");

            return Page();
        }

        // An account exists, so check its password.
        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Clear the password-reset restriction after successful login.
            ForgotPasswordModel.ClearRequestLimit(user.Email);

            // Clear reset-related session data for this browser session.
            ForgotPasswordModel.ClearResetSession(HttpContext.Session);

            // Redirect to the requested local page if one was supplied.
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            // Otherwise, preserve the user's saved Start Page.
            var startPage = await _context.UserSettings
                .AsNoTracking()
                .Where(settings => settings.UserId == user.Id)
                .Select(settings => settings.StartPage)
                .FirstOrDefaultAsync();

            return startPage switch
            {
                "Tasks" => RedirectToPage("/Tasks/Tasks"),

                "Statistics" => RedirectToPage(
                    "/Statistics/Statistics"),

                "Dashboard" => RedirectToPage(
                    "/Dashboard/Dashboard"),

                _ => RedirectToPage("/Dashboard/Dashboard")
            };
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account is currently locked.");

            return Page();
        }

        // The account exists, but the supplied credentials are incorrect.
        ModelState.AddModelError(
            string.Empty,
            "Invalid username or password.");

        return Page();
    }
}
