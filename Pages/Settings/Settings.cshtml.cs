
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Task = System.Threading.Tasks.Task;
using System.Text;
using keeptrack.Data;
using keeptrack.Models;
using keeptrack.Pages.Account.ForgotPassword;
using keeptrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Settings;

public class SettingsModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<SettingsModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public ApplicationUser? CurrentUser { get; set; }
    public UserSettings? CurrentSettings { get; set; }

    public bool IsEditingProfile { get; set; }
    public bool IsChangingPassword { get; set; }

    public bool IsSecurityCodePending { get; set; }
    public bool IsSecurityVerified { get; set; }
    public bool CanRequestSecurityCode { get; set; } = true;
    public int SecurityRequestsUsed { get; set; }
    public long SecurityCodeExpiresAtUnixMs { get; set; }
    public long SecurityLockoutUntilUnixMs { get; set; }

    [BindProperty]
    public string DateFormat { get; set; } = "yyyy/MM/dd";

    [BindProperty]
    public string StartPage { get; set; } = "Dashboard";

    [BindProperty]
    public string Appearance { get; set; } = "System Default";

    [BindProperty]
    public ProfileInputModel ProfileInput { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    [BindProperty]
    public SecurityInputModel SecurityInput { get; set; } = new();

    [BindProperty]
    public DeleteAccountInputModel DeleteAccountInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    private const int SecurityCodeLifetimeSeconds = 60;
    private const int SecurityGrantLifetimeSeconds = 300;
    private const int MaximumSecurityCodeRequests = 3;
    private const int MaximumSecurityVerificationAttempts = 5;
    private static readonly TimeSpan SecurityRequestLockoutDuration =
        TimeSpan.FromHours(24);

    private const string SecurityCodeUserIdKey =
        "SettingsSecurity.CodeUserId";
    private const string SecurityCodeHashKey =
        "SettingsSecurity.CodeHash";
    private const string SecurityCodeExpiryKey =
        "SettingsSecurity.CodeExpiry";
    private const string SecurityCodeAttemptsKey =
        "SettingsSecurity.CodeAttempts";
    private const string SecurityVerifiedUserIdKey =
        "SettingsSecurity.VerifiedUserId";
    private const string SecurityVerifiedExpiryKey =
        "SettingsSecurity.VerifiedExpiry";

    // In-memory request limits. These survive browser sessions but reset
    // if the application process restarts. Use persistent storage in production.
    private static readonly ConcurrentDictionary<string, SecurityRequestState>
        SecurityRequestLimits = new();

    private sealed class SecurityRequestState
    {
        public int RequestsUsed { get; set; }
        public DateTimeOffset? LockoutUntil { get; set; }
        public object SyncRoot { get; } = new();
    }

    // =====================================================
    // INPUT MODELS
    // =====================================================

    public class ProfileInputModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        [StringLength(50)]
        public string Gender { get; set; } = string.Empty;

        // Kept temporarily so the current Razor page still compiles.
        // SaveProfile never changes these fields.
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;
    }

    public class PasswordInputModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Your new password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword),
            ErrorMessage = "The new passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class SecurityInputModel
    {
        public string? VerificationCode { get; set; }

        [StringLength(256,
            ErrorMessage = "Username cannot exceed 256 characters.")]
        public string? NewUserName { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256,
            ErrorMessage = "Email address cannot exceed 256 characters.")]
        public string? NewEmail { get; set; }

        [StringLength(256,
            ErrorMessage = "Email address cannot exceed 256 characters.")]
        public string? ConfirmNewEmail { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [StringLength(100,
            ErrorMessage = "Your new password cannot exceed 100 characters.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmNewPassword { get; set; }
    }

    public class DeleteAccountInputModel
    {
        [Required(ErrorMessage = "Enter your current password.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type DELETE to confirm account deletion.")]
        public string ConfirmationText { get; set; } = string.Empty;
    }

    // =====================================================
    // ALLOWED SETTINGS
    // =====================================================

    private static readonly string[] AllowedDateFormats =
    {
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd MMM yyyy",
        "yyyy MMM dd",
        "MMMM dd, yyyy"
    };

    private static readonly string[] AllowedStartPages =
    {
        "Dashboard",
        "Tasks",
        "Statistics"
    };

    private static readonly string[] AllowedAppearances =
    {
        "System Default",
        "Light",
        "Dark"
    };

    // =====================================================
    // LOAD SETTINGS PAGE
    // =====================================================

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);

        LoadGeneralSettings();
        LoadAppearance();
        LoadProfileInput();
        LoadSecurityState(CurrentUser);

        return Page();
    }

    // =====================================================
    // SAVE BROWSER PUSH SUBSCRIPTION
    // =====================================================

    public async Task<IActionResult> OnPostSaveBrowserPushSubscriptionAsync(
        [FromBody] BrowserPushSubscriptionRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.P256dh) ||
            string.IsNullOrWhiteSpace(request.Auth))
        {
            return BadRequest(new { error = "The subscription information is incomplete." });
        }

        if (request.Endpoint.Length > 4096 ||
            request.P256dh.Length > 512 ||
            request.Auth.Length > 512 ||
            !Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            return BadRequest(new { error = "The subscription information is invalid." });
        }

        var subscription = await _context.BrowserPushSubscriptions
            .FirstOrDefaultAsync(item => item.Endpoint == request.Endpoint);

        if (subscription != null && subscription.UserId != user.Id)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                error = "This browser subscription is already registered to another account."
            });
        }

        if (subscription == null)
        {
            subscription = new BrowserPushSubscription
            {
                UserId = user.Id,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.BrowserPushSubscriptions.Add(subscription);
        }
        else
        {
            subscription.P256dh = request.P256dh;
            subscription.Auth = request.Auth;
            subscription.IsActive = true;
        }

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    // =====================================================
    // DISABLE BROWSER PUSH SUBSCRIPTION
    // =====================================================

    public async Task<IActionResult> OnPostDisableBrowserPushSubscriptionAsync(
        [FromBody] BrowserPushSubscriptionRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest(new { error = "The subscription endpoint is missing." });

        var subscription = await _context.BrowserPushSubscriptions
            .FirstOrDefaultAsync(item =>
                item.UserId == user.Id &&
                item.Endpoint == request.Endpoint);

        if (subscription != null)
        {
            subscription.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { success = true });
    }

    // =====================================================
    // SEND ACCOUNT SECURITY CODE
    // =====================================================

    public async Task<IActionResult> OnPostSendSecurityCodeAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState();

        if (string.IsNullOrWhiteSpace(CurrentUser.Email))
        {
            ErrorMessage =
                "Your account does not have an email address. Please contact support.";
            return RedirectToPage();
        }

        if (!TryRegisterSecurityCodeRequest(
            CurrentUser.Id,
            out int requestsUsed,
            out DateTimeOffset? lockoutUntil))
        {
            ErrorMessage = lockoutUntil.HasValue
                ? $"You have reached the verification-code request limit. Try again after {lockoutUntil.Value.LocalDateTime:g}."
                : "You cannot request another verification code right now.";

            return RedirectToPage();
        }

        string code = RandomNumberGenerator
            .GetInt32(0, 1_000_000)
            .ToString("D6");

        string codeHash = HashSecurityCode(CurrentUser.Id, code);
        long expiresAt = DateTimeOffset.UtcNow
            .AddSeconds(SecurityCodeLifetimeSeconds)
            .ToUnixTimeSeconds();

        HttpContext.Session.SetString(SecurityCodeUserIdKey, CurrentUser.Id);
        HttpContext.Session.SetString(SecurityCodeHashKey, codeHash);
        HttpContext.Session.SetString(SecurityCodeExpiryKey, expiresAt.ToString());
        HttpContext.Session.SetInt32(SecurityCodeAttemptsKey, 0);

        // A new code request invalidates any previous verification grant.
        ClearSecurityGrant();

        try
        {
            string emailBody = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.6">
                    <h2>KeepTrack account security</h2>
                    <p>A request was made to change sensitive information
                    on your KeepTrack account.</p>
                    <p>Your verification code is:</p>
                    <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{code}</p>
                    <p>This code expires in 60 seconds.</p>
                    <p>If you did not request this code, you can ignore this email.
                    Do not share this code with anyone.</p>
                </div>
                """;

            await _emailSender.SendEmailAsync(
                CurrentUser.Email,
                "KeepTrack security verification code",
                emailBody);

            SuccessMessage =
                $"Verification code sent to {MaskEmail(CurrentUser.Email)}. Request {requestsUsed} of {MaximumSecurityCodeRequests}.";

            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send settings security code for user {UserId}.",
                CurrentUser.Id);

            // The request remains counted to prevent repeated email attempts.
            ErrorMessage =
                "The verification email could not be sent. Your request still counts toward the limit. Please try again if requests remain.";
        }

        return RedirectToPage();
    }

    // =====================================================
    // VERIFY ACCOUNT SECURITY CODE
    // =====================================================

    public async Task<IActionResult> OnPostVerifySecurityCodeAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState(nameof(SecurityInput));

        string? codeUserId =
            HttpContext.Session.GetString(SecurityCodeUserIdKey);
        string? savedHash =
            HttpContext.Session.GetString(SecurityCodeHashKey);
        string? expiryText =
            HttpContext.Session.GetString(SecurityCodeExpiryKey);

        if (!string.Equals(
                codeUserId, CurrentUser.Id, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(savedHash) ||
            !long.TryParse(expiryText, out long expiry))
        {
            ClearPendingSecurityCode();

            ErrorMessage =
                "There is no active verification code. Request a new code.";
            return RedirectToPage();
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
        {
            ClearPendingSecurityCode();

            ErrorMessage =
                "Your verification code has expired. Request a new code.";
            return RedirectToPage();
        }

        string submittedCode =
            (SecurityInput.VerificationCode ?? string.Empty).Trim();

        if (submittedCode.Length != 6 ||
            submittedCode.Any(character => !char.IsAsciiDigit(character)))
        {
            ModelState.AddModelError(
                "SecurityInput.VerificationCode",
                "Enter the six-digit code from your email.");

            await PrepareSettingsPageAsync(CurrentUser);
            IsSecurityCodePending = true;
            return Page();
        }

        string submittedHash = HashSecurityCode(CurrentUser.Id, submittedCode);

        byte[] expectedBytes;
        byte[] submittedBytes;

        try
        {
            expectedBytes = Convert.FromBase64String(savedHash);
            submittedBytes = Convert.FromBase64String(submittedHash);
        }
        catch (FormatException)
        {
            ClearPendingSecurityCode();

            ErrorMessage =
                "The verification code is invalid. Request a new code.";
            return RedirectToPage();
        }

        bool matches =
            expectedBytes.Length == submittedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(
                expectedBytes, submittedBytes);

        if (!matches)
        {
            int attempts =
                (HttpContext.Session.GetInt32(SecurityCodeAttemptsKey) ?? 0) + 1;

            HttpContext.Session.SetInt32(SecurityCodeAttemptsKey, attempts);

            if (attempts >= MaximumSecurityVerificationAttempts)
            {
                ClearPendingSecurityCode();

                ErrorMessage =
                    "Too many incorrect attempts. Request a new verification code.";
                return RedirectToPage();
            }

            ModelState.AddModelError(
                "SecurityInput.VerificationCode",
                $"That code is incorrect. You have {MaximumSecurityVerificationAttempts - attempts} attempt(s) remaining.");

            await PrepareSettingsPageAsync(CurrentUser);
            IsSecurityCodePending = true;
            return Page();
        }

        ClearPendingSecurityCode();

        HttpContext.Session.SetString(
            SecurityVerifiedUserIdKey, CurrentUser.Id);

        HttpContext.Session.SetString(
            SecurityVerifiedExpiryKey,
            DateTimeOffset.UtcNow
                .AddSeconds(SecurityGrantLifetimeSeconds)
                .ToUnixTimeSeconds()
                .ToString());

        _logger.LogInformation(
            "Settings security verification succeeded for user {UserId}.",
            CurrentUser.Id);

        SuccessMessage =
            "Your identity has been verified. You can now edit your account security details for the next five minutes.";

        ErrorMessage = null;

        return RedirectToPage();
    }

    // =====================================================
    // SAVE VERIFIED ACCOUNT SECURITY CHANGES
    // =====================================================

    public async Task<IActionResult> OnPostSaveSecurityChangesAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState(nameof(SecurityInput));

        if (!HasValidSecurityGrant(CurrentUser.Id))
        {
            ClearSecurityGrant();

            ErrorMessage =
                "For your security, verify your email again before changing account details.";

            return RedirectToPage();
        }

        SecurityInput.NewUserName =
            SecurityInput.NewUserName?.Trim();

        SecurityInput.NewEmail =
            SecurityInput.NewEmail?.Trim();

        SecurityInput.ConfirmNewEmail =
            SecurityInput.ConfirmNewEmail?.Trim();

        SecurityInput.CurrentPassword ??= string.Empty;
        SecurityInput.NewPassword ??= string.Empty;
        SecurityInput.ConfirmNewPassword ??= string.Empty;

        // Determine which changes the user actually requested.
        bool usernameProvided =
            !string.IsNullOrWhiteSpace(SecurityInput.NewUserName);

        bool emailProvided =
            !string.IsNullOrWhiteSpace(SecurityInput.NewEmail) ||
            !string.IsNullOrWhiteSpace(SecurityInput.ConfirmNewEmail);

        // A password change is requested only when the user enters
        // a new password or its confirmation.
        //
        // CurrentPassword alone must not trigger password validation.
        // This allows username and email changes without changing
        // the user's password.
        bool passwordProvided =
            !string.IsNullOrWhiteSpace(SecurityInput.NewPassword) ||
            !string.IsNullOrWhiteSpace(SecurityInput.ConfirmNewPassword);

        // Verification is already complete. This save form does not need
        // another verification code.
        ModelState.Remove("SecurityInput.VerificationCode");

        // Remove validation state for sections the user did not use.
        if (!usernameProvided)
            ModelState.Remove("SecurityInput.NewUserName");

        if (!emailProvided)
        {
            ModelState.Remove("SecurityInput.NewEmail");
            ModelState.Remove("SecurityInput.ConfirmNewEmail");
        }

        if (!passwordProvided)
        {
            ModelState.Remove("SecurityInput.CurrentPassword");
            ModelState.Remove("SecurityInput.NewPassword");
            ModelState.Remove("SecurityInput.ConfirmNewPassword");
        }

        bool usernameChanged = usernameProvided &&
            !string.Equals(
                SecurityInput.NewUserName,
                CurrentUser.UserName,
                StringComparison.Ordinal);

        bool emailChanged = emailProvided &&
            !string.Equals(
                SecurityInput.NewEmail,
                CurrentUser.Email,
                StringComparison.OrdinalIgnoreCase);

        if (!usernameProvided && !emailProvided && !passwordProvided)
        {
            ModelState.AddModelError(
                string.Empty,
                "Enter at least one account detail you want to change.");
        }

        // USERNAME: validate only when the user entered a username.
        if (usernameProvided)
        {
            if (SecurityInput.NewUserName!.Length > 256)
            {
                ModelState.AddModelError(
                    "SecurityInput.NewUserName",
                    "Username cannot exceed 256 characters.");
            }

            if (usernameChanged)
            {
                var existingUser = await _userManager.FindByNameAsync(
                    SecurityInput.NewUserName!);

                if (existingUser != null &&
                    existingUser.Id != CurrentUser.Id)
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewUserName",
                        "That username is already in use.");
                }
            }
        }

        // EMAIL: require both email fields only when the user is changing email.
        if (emailProvided)
        {
            if (string.IsNullOrWhiteSpace(SecurityInput.NewEmail))
            {
                ModelState.AddModelError(
                    "SecurityInput.NewEmail",
                    "Enter your new email address.");
            }
            else
            {
                if (!new EmailAddressAttribute().IsValid(
                        SecurityInput.NewEmail))
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewEmail",
                        "Enter a valid email address.");
                }

                if (SecurityInput.NewEmail.Length > 256)
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewEmail",
                        "Email address cannot exceed 256 characters.");
                }
            }

            if (string.IsNullOrWhiteSpace(SecurityInput.ConfirmNewEmail))
            {
                ModelState.AddModelError(
                    "SecurityInput.ConfirmNewEmail",
                    "Confirm your new email address.");
            }
            else if (!string.Equals(
                         SecurityInput.NewEmail,
                         SecurityInput.ConfirmNewEmail,
                         StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "SecurityInput.ConfirmNewEmail",
                    "The email addresses do not match.");
            }

            if (emailChanged &&
                !string.IsNullOrWhiteSpace(SecurityInput.NewEmail) &&
                new EmailAddressAttribute().IsValid(SecurityInput.NewEmail))
            {
                var existingUser = await _userManager.FindByEmailAsync(
                    SecurityInput.NewEmail);

                if (existingUser != null &&
                    existingUser.Id != CurrentUser.Id)
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewEmail",
                        "That email address is already in use.");
                }
            }
        }

        // PASSWORD: require password fields only when the user is changing
        // their password.
        if (passwordProvided)
        {
            if (string.IsNullOrWhiteSpace(SecurityInput.CurrentPassword))
            {
                ModelState.AddModelError(
                    "SecurityInput.CurrentPassword",
                    "Enter your current password.");
            }
            else if (!await _userManager.CheckPasswordAsync(
                         CurrentUser,
                         SecurityInput.CurrentPassword))
            {
                ModelState.AddModelError(
                    "SecurityInput.CurrentPassword",
                    "Your current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(SecurityInput.NewPassword))
            {
                ModelState.AddModelError(
                    "SecurityInput.NewPassword",
                    "Enter a new password.");
            }
            else
            {
                if (SecurityInput.NewPassword.Length < 8)
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewPassword",
                        "Your new password must be at least 8 characters long.");
                }

                if (SecurityInput.NewPassword.Length > 100)
                {
                    ModelState.AddModelError(
                        "SecurityInput.NewPassword",
                        "Your new password cannot exceed 100 characters.");
                }

                if (!string.Equals(
                        SecurityInput.NewPassword,
                        SecurityInput.ConfirmNewPassword,
                        StringComparison.Ordinal))
                {
                    ModelState.AddModelError(
                        "SecurityInput.ConfirmNewPassword",
                        "The new passwords do not match.");
                }

                if (SecurityInput.NewPassword.Length >= 8 &&
                    SecurityInput.NewPassword.Length <= 100)
                {
                    foreach (var validator in _userManager.PasswordValidators)
                    {
                        var validation = await validator.ValidateAsync(
                            _userManager,
                            CurrentUser,
                            SecurityInput.NewPassword);

                        foreach (var error in validation.Errors)
                        {
                            ModelState.AddModelError(
                                "SecurityInput.NewPassword",
                                error.Description);
                        }
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PrepareSettingsPageAsync(CurrentUser);
            IsSecurityVerified = HasValidSecurityGrant(CurrentUser.Id);
            return Page();
        }

        string? oldUserName = CurrentUser.UserName;
        string? oldEmail = CurrentUser.Email;
        bool oldEmailConfirmed = CurrentUser.EmailConfirmed;

        try
        {
            if (usernameChanged)
                CurrentUser.UserName = SecurityInput.NewUserName;

            if (emailChanged)
            {
                CurrentUser.Email = SecurityInput.NewEmail;
                CurrentUser.EmailConfirmed = false;
            }

            if (usernameChanged || emailChanged)
            {
                var updateResult = await _userManager.UpdateAsync(CurrentUser);

                if (!updateResult.Succeeded)
                {
                    CurrentUser.UserName = oldUserName;
                    CurrentUser.Email = oldEmail;
                    CurrentUser.EmailConfirmed = oldEmailConfirmed;

                    foreach (var error in updateResult.Errors)
                    {
                        string key = error.Code switch
                        {
                            "DuplicateUserName" or "InvalidUserName" =>
                                "SecurityInput.NewUserName",

                            "DuplicateEmail" or "InvalidEmail" =>
                                "SecurityInput.NewEmail",

                            _ => string.Empty
                        };

                        ModelState.AddModelError(key, error.Description);
                    }

                    await PrepareSettingsPageAsync(CurrentUser);
                    IsSecurityVerified = HasValidSecurityGrant(CurrentUser.Id);
                    return Page();
                }
            }

            if (passwordProvided)
            {
                var passwordResult = await _userManager.ChangePasswordAsync(
                    CurrentUser,
                    SecurityInput.CurrentPassword!,
                    SecurityInput.NewPassword!);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(
                            "SecurityInput.NewPassword",
                            error.Description);
                    }

                    await PrepareSettingsPageAsync(CurrentUser);
                    IsSecurityVerified = HasValidSecurityGrant(CurrentUser.Id);
                    return Page();
                }

                await _userManager.UpdateSecurityStampAsync(CurrentUser);

                ClearSecurityGrant();
                ClearPendingSecurityCode();

                await _signInManager.SignOutAsync();

                _logger.LogInformation(
                    "Account security details and password updated for user {UserId}.",
                    CurrentUser.Id);

                SuccessMessage =
                    "Your account security details were updated. Please sign in again using your new credentials.";

                ErrorMessage = null;

                return RedirectToPage("/Account/SignIn/SignIn");
            }

            ClearSecurityGrant();
            ClearPendingSecurityCode();

            await _signInManager.RefreshSignInAsync(CurrentUser);

            SuccessMessage = emailChanged
                ? "Account details updated. Your new email address must be verified before it can be used as a confirmed email."
                : usernameChanged
                    ? "Your username was updated successfully."
                    : "Account details updated successfully.";

            ErrorMessage = null;

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update account security details for user {UserId}.",
                CurrentUser.Id);

            ErrorMessage =
                "Your account security details could not be updated. Please try again.";

            await PrepareSettingsPageAsync(CurrentUser);
            IsSecurityVerified = HasValidSecurityGrant(CurrentUser.Id);
            return Page();
        }
    }

    // =====================================================
    // SAVE GENERAL SETTINGS
    // =====================================================

    public async Task<IActionResult> OnPostSaveGeneralAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState(nameof(DateFormat), nameof(StartPage));

        DateFormat = Request.Form["DateFormat"].ToString().Trim();

        string? matchedDateFormat = AllowedDateFormats.FirstOrDefault(
            format => string.Equals(
                format, DateFormat, StringComparison.OrdinalIgnoreCase));

        if (matchedDateFormat == null)
        {
            ModelState.AddModelError(
                nameof(DateFormat),
                "Please select a supported date format.");
        }
        else
        {
            DateFormat = matchedDateFormat;
        }

        if (!AllowedStartPages.Contains(StartPage))
        {
            ModelState.AddModelError(
                nameof(StartPage),
                "Please select a supported start page.");
        }

        if (!ModelState.IsValid)
        {
            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            LoadAppearance();
            LoadProfileInput();
            LoadSecurityState(CurrentUser);
            return Page();
        }

        try
        {
            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            CurrentSettings.DateFormat = DateFormat;
            CurrentSettings.StartPage = StartPage;

            await _context.SaveChangesAsync();

            SuccessMessage = "General settings saved successfully.";
            ErrorMessage = null;

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save general settings for user {UserId}.",
                CurrentUser.Id);

            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            LoadGeneralSettings();
            LoadAppearance();
            LoadProfileInput();
            LoadSecurityState(CurrentUser);

            ErrorMessage = "Settings could not be saved. Please try again.";
            return Page();
        }
    }

    // =====================================================
    // SAVE APPEARANCE
    // =====================================================

    public async Task<IActionResult> OnPostSaveAppearanceAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState(nameof(Appearance));

        string submittedAppearance =
            Request.Form["Appearance"].ToString().Trim();

        string? selectedAppearance = AllowedAppearances.FirstOrDefault(
            option => string.Equals(
                option, submittedAppearance, StringComparison.OrdinalIgnoreCase));

        if (selectedAppearance == null)
        {
            ModelState.AddModelError(
                nameof(Appearance),
                "Please select a supported appearance.");

            Appearance = "System Default";
            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            LoadGeneralSettings();
            LoadProfileInput();
            LoadSecurityState(CurrentUser);
            return Page();
        }

        Appearance = selectedAppearance;

        try
        {
            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            CurrentSettings.Appearance = selectedAppearance;

            await _context.SaveChangesAsync();

            SuccessMessage = "Appearance settings saved successfully.";
            ErrorMessage = null;

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save appearance settings for user {UserId}.",
                CurrentUser.Id);

            CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
            LoadGeneralSettings();
            LoadAppearance();
            LoadProfileInput();
            LoadSecurityState(CurrentUser);

            ErrorMessage =
                "Appearance settings could not be saved. Please try again.";

            return Page();
        }
    }

    // =====================================================
    // SAVE PERSONAL INFORMATION
    // Username and email are intentionally not changed here.
    // They must go through the verified security workflow.
    // =====================================================

    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        IsEditingProfile = true;
        RemoveUnrelatedModelState(nameof(ProfileInput));

        // Email and username are managed by Password & Security.
        // Ignore their validation errors when saving personal information.
        ModelState.Remove("ProfileInput.Email");
        ModelState.Remove("ProfileInput.UserName");

        ProfileInput.FirstName =
            ProfileInput.FirstName?.Trim() ?? string.Empty;
        ProfileInput.MiddleName =
            string.IsNullOrWhiteSpace(ProfileInput.MiddleName)
                ? null
                : ProfileInput.MiddleName.Trim();
        ProfileInput.LastName =
            ProfileInput.LastName?.Trim() ?? string.Empty;
        ProfileInput.Gender =
            ProfileInput.Gender?.Trim() ?? string.Empty;

        // Ignore submitted email/username values in this handler.
        ProfileInput.Email = CurrentUser.Email ?? string.Empty;
        ProfileInput.UserName = CurrentUser.UserName ?? string.Empty;

        CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
        LoadGeneralSettings();
        LoadAppearance();
        LoadSecurityState(CurrentUser);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            CurrentUser.FirstName = ProfileInput.FirstName;
            CurrentUser.MiddleName = ProfileInput.MiddleName;
            CurrentUser.LastName = ProfileInput.LastName;
            CurrentUser.Gender = ProfileInput.Gender;

            var result = await _userManager.UpdateAsync(CurrentUser);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                CurrentUser = await _userManager.GetUserAsync(User);
                return Page();
            }

            _logger.LogInformation(
                "Personal information updated for user {UserId}.",
                CurrentUser.Id);

            SuccessMessage = "Profile updated successfully.";
            ErrorMessage = null;

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update personal information for user {UserId}.",
                CurrentUser?.Id ?? "Unknown");

            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser != null)
                await PrepareSettingsPageAsync(CurrentUser);

            ErrorMessage =
                "Your profile could not be updated. Please try again.";

            return Page();
        }
    }

    // =====================================================
    // LEGACY CHANGE PASSWORD HANDLER
    // Requires the same server-side verification grant.
    // The new UI will use SaveSecurityChanges instead.
    // =====================================================

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        if (!HasValidSecurityGrant(CurrentUser.Id))
        {
            ErrorMessage =
                "Verify your email through Password & Security before changing your password.";
            return RedirectToPage();
        }

        IsChangingPassword = true;
        RemoveUnrelatedModelState(nameof(PasswordInput));

        CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
        LoadGeneralSettings();
        LoadAppearance();
        LoadProfileInput();
        LoadSecurityState(CurrentUser);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await _userManager.ChangePasswordAsync(
                CurrentUser,
                PasswordInput.CurrentPassword,
                PasswordInput.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            await _userManager.UpdateSecurityStampAsync(CurrentUser);

            ClearSecurityGrant();
            ClearPendingSecurityCode();

            await _signInManager.SignOutAsync();

            SuccessMessage =
                "Your password has been changed. Please sign in again.";

            return RedirectToPage("/Account/SignIn/SignIn");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to change password for user {UserId}.",
                CurrentUser.Id);

            ModelState.AddModelError(
                string.Empty,
                "Your password could not be changed. Please try again.");

            return Page();
        }
    }

    // =====================================================
    // DELETE ACCOUNT
    // =====================================================

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);

        if (CurrentUser == null)
            return RedirectToPage("/Account/SignIn/SignIn");

        RemoveUnrelatedModelState(nameof(DeleteAccountInput));

        DeleteAccountInput.CurrentPassword =
            DeleteAccountInput.CurrentPassword?.Trim() ?? string.Empty;
        DeleteAccountInput.ConfirmationText =
            DeleteAccountInput.ConfirmationText?.Trim() ?? string.Empty;

        CurrentSettings = await GetOrCreateSettingsAsync(CurrentUser.Id);
        LoadGeneralSettings();
        LoadAppearance();
        LoadProfileInput();
        LoadSecurityState(CurrentUser);

        if (!string.Equals(
            DeleteAccountInput.ConfirmationText,
            "DELETE",
            StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                "DeleteAccountInput.ConfirmationText",
                "Type DELETE exactly to confirm.");
        }

        if (!ModelState.IsValid)
            return Page();

        bool passwordIsValid = await _userManager.CheckPasswordAsync(
            CurrentUser,
            DeleteAccountInput.CurrentPassword);

        if (!passwordIsValid)
        {
            ModelState.AddModelError(
                "DeleteAccountInput.CurrentPassword",
                "Your current password is incorrect.");

            return Page();
        }

        string userId = CurrentUser.Id;
        string? userEmail = CurrentUser.Email;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var userTasks = await _context.Tasks
                .Where(task => task.UserId == userId)
                .ToListAsync();

            _context.Tasks.RemoveRange(userTasks);

            var userSettings = await _context.UserSettings
                .Where(settings => settings.UserId == userId)
                .ToListAsync();

            _context.UserSettings.RemoveRange(userSettings);

            await _context.SaveChangesAsync();

            var deleteResult = await _userManager.DeleteAsync(CurrentUser);

            if (!deleteResult.Succeeded)
            {
                await transaction.RollbackAsync();

                foreach (var error in deleteResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                CurrentSettings = await GetOrCreateSettingsAsync(userId);
                LoadGeneralSettings();
                LoadAppearance();
                LoadProfileInput();

                return Page();
            }

            await transaction.CommitAsync();

            ForgotPasswordModel.ClearRequestLimit(userEmail);
            ForgotPasswordModel.ClearResetSession(HttpContext.Session);
            ClearPendingSecurityCode();
            ClearSecurityGrant();

            await _signInManager.SignOutAsync();

            return RedirectToPage("/Home/Home");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Failed to delete account for user {UserId}.",
                userId);

            ErrorMessage =
                "Your account could not be deleted. Please try again.";

            return RedirectToPage();
        }
    }

    // =====================================================
    // SECURITY HELPERS
    // =====================================================

    private bool TryRegisterSecurityCodeRequest(
        string userId,
        out int requestsUsed,
        out DateTimeOffset? lockoutUntil)
    {
        SecurityRequestState state = SecurityRequestLimits.GetOrAdd(
            userId,
            _ => new SecurityRequestState());

        lock (state.SyncRoot)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (state.LockoutUntil.HasValue &&
                state.LockoutUntil.Value > now)
            {
                requestsUsed = state.RequestsUsed;
                lockoutUntil = state.LockoutUntil;
                return false;
            }

            if (state.LockoutUntil.HasValue &&
                state.LockoutUntil.Value <= now)
            {
                state.RequestsUsed = 0;
                state.LockoutUntil = null;
            }

            if (state.RequestsUsed >= MaximumSecurityCodeRequests)
            {
                state.LockoutUntil = now.Add(SecurityRequestLockoutDuration);
                requestsUsed = state.RequestsUsed;
                lockoutUntil = state.LockoutUntil;
                return false;
            }

            state.RequestsUsed++;

            // The third request is allowed, but immediately starts the lockout
            // against any additional requests.
            if (state.RequestsUsed >= MaximumSecurityCodeRequests)
            {
                state.LockoutUntil = now.Add(SecurityRequestLockoutDuration);
            }

            requestsUsed = state.RequestsUsed;
            lockoutUntil = state.LockoutUntil;

            return true;
        }
    }

    private static string HashSecurityCode(string userId, string code)
    {
        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"{userId}:{code}"));

        return Convert.ToBase64String(hash);
    }

    private bool HasValidSecurityGrant(string userId)
    {
        string? verifiedUserId =
            HttpContext.Session.GetString(SecurityVerifiedUserIdKey);
        string? expiryText =
            HttpContext.Session.GetString(SecurityVerifiedExpiryKey);

        if (!string.Equals(
                verifiedUserId, userId, StringComparison.Ordinal) ||
            !long.TryParse(expiryText, out long expiry))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
        {
            ClearSecurityGrant();
            return false;
        }

        return true;
    }

    private void LoadSecurityState(ApplicationUser user)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string? codeUserId =
            HttpContext.Session.GetString(SecurityCodeUserIdKey);
        string? codeHash =
            HttpContext.Session.GetString(SecurityCodeHashKey);
        string? expiryText =
            HttpContext.Session.GetString(SecurityCodeExpiryKey);

        IsSecurityCodePending =
            string.Equals(codeUserId, user.Id, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(codeHash) &&
            long.TryParse(expiryText, out long codeExpiry) &&
            codeExpiry >= now;

        SecurityCodeExpiresAtUnixMs = IsSecurityCodePending
            ? long.Parse(expiryText!) * 1000
            : 0;

        IsSecurityVerified = HasValidSecurityGrant(user.Id);

        if (SecurityRequestLimits.TryGetValue(user.Id, out var state))
        {
            lock (state.SyncRoot)
            {
                SecurityRequestsUsed = state.RequestsUsed;

                if (state.LockoutUntil.HasValue &&
                    state.LockoutUntil.Value > DateTimeOffset.UtcNow)
                {
                    SecurityLockoutUntilUnixMs =
                        state.LockoutUntil.Value.ToUnixTimeMilliseconds();
                    CanRequestSecurityCode = false;
                }
                else
                {
                    SecurityLockoutUntilUnixMs = 0;
                    CanRequestSecurityCode = true;
                }
            }
        }
        else
        {
            SecurityRequestsUsed = 0;
            SecurityLockoutUntilUnixMs = 0;
            CanRequestSecurityCode = true;
        }
    }

    private void ClearPendingSecurityCode()
    {
        HttpContext.Session.Remove(SecurityCodeUserIdKey);
        HttpContext.Session.Remove(SecurityCodeHashKey);
        HttpContext.Session.Remove(SecurityCodeExpiryKey);
        HttpContext.Session.Remove(SecurityCodeAttemptsKey);
    }

    private void ClearSecurityGrant()
    {
        HttpContext.Session.Remove(SecurityVerifiedUserIdKey);
        HttpContext.Session.Remove(SecurityVerifiedExpiryKey);
    }

    private static string MaskEmail(string email)
    {
        int at = email.IndexOf('@');

        if (at <= 0)
            return "***";

        string name = email[..at];
        string domain = email[at..];

        string visible = name.Length == 1
            ? name
            : name[..Math.Min(2, name.Length)];

        return $"{visible}***{domain}";
    }

    private async System.Threading.Tasks.Task PrepareSettingsPageAsync(ApplicationUser user)
    {
        CurrentUser = user;
        CurrentSettings = await GetOrCreateSettingsAsync(user.Id);

        LoadGeneralSettings();
        LoadAppearance();
        LoadProfileInput();
        LoadSecurityState(user);
    }

    // =====================================================
    // GENERAL SETTINGS HELPERS
    // =====================================================

    private void LoadGeneralSettings()
    {
        if (CurrentSettings == null)
            return;

        DateFormat = AllowedDateFormats.Contains(CurrentSettings.DateFormat)
            ? CurrentSettings.DateFormat
            : "yyyy/MM/dd";

        StartPage = AllowedStartPages.Contains(CurrentSettings.StartPage)
            ? CurrentSettings.StartPage
            : "Dashboard";
    }

    // =====================================================
    // APPEARANCE HELPER
    // =====================================================

    private void LoadAppearance()
    {
        if (CurrentSettings == null)
        {
            Appearance = "System Default";
            return;
        }

        string? savedAppearance = AllowedAppearances.FirstOrDefault(
            option => string.Equals(
                option,
                CurrentSettings.Appearance,
                StringComparison.OrdinalIgnoreCase));

        Appearance = savedAppearance ?? "System Default";
    }

    // =====================================================
    // PROFILE HELPER
    // =====================================================

    private void LoadProfileInput()
    {
        if (CurrentUser == null)
            return;

        ProfileInput = new ProfileInputModel
        {
            FirstName = CurrentUser.FirstName,
            MiddleName = CurrentUser.MiddleName,
            LastName = CurrentUser.LastName,
            Gender = CurrentUser.Gender,
            Email = CurrentUser.Email ?? string.Empty,
            UserName = CurrentUser.UserName ?? string.Empty
        };
    }

    // =====================================================
    // MODEL STATE HELPER
    // =====================================================

    private void RemoveUnrelatedModelState(
        params string[] propertyPrefixes)
    {
        foreach (var key in ModelState.Keys.ToList())
        {
            bool isRelated = propertyPrefixes.Any(prefix =>
                key.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase));

            if (!isRelated)
                ModelState.Remove(key);
        }
    }

    // =====================================================
    // USER SETTINGS HELPER
    // =====================================================

    private async System.Threading.Tasks.Task<UserSettings> GetOrCreateSettingsAsync(string userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (settings != null)
            return settings;

        settings = new UserSettings
        {
            UserId = userId,
            Language = "English",
            TimeZone = "Africa/Johannesburg",
            DateFormat = "yyyy/MM/dd",
            StartPage = "Dashboard",
            DefaultStartImmediately = true,
            DefaultReminderEnabled = false,
            DefaultReminderValue = null,
            DefaultReminderUnit = null,
            Appearance = "System Default",
            AccentColour = "Default",
            TaskLayout = "Comfortable"
        };

        _context.UserSettings.Add(settings);
        await _context.SaveChangesAsync();

        return settings;
    }
}


