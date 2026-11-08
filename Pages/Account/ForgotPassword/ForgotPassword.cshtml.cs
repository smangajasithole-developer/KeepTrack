
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using keeptrack.Models;
using keeptrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace keeptrack.Pages.Account.ForgotPassword;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordModel> _logger;

    private static readonly TimeSpan CodeLifetime =
        TimeSpan.FromSeconds(60);

    private static readonly TimeSpan VerifiedLifetime =
        TimeSpan.FromMinutes(5);

    private static readonly TimeSpan RequestLockoutLifetime =
        TimeSpan.FromHours(24);

    private const int MaximumCodeSends = 3;

    private const string ResetUserIdKey = "KeepTrack.Reset.UserId";
    private const string ResetEmailKey = "KeepTrack.Reset.Email";
    private const string ResetCodeHashKey = "KeepTrack.Reset.CodeHash";
    private const string ResetExpiryKey = "KeepTrack.Reset.Expiry";
    private const string ResetVerifiedKey = "KeepTrack.Reset.Verified";
    private const string ResetVerifiedExpiryKey = "KeepTrack.Reset.VerifiedExpiry";
    private const string ResetSendCountKey = "KeepTrack.Reset.SendCount";

    private const string AccountDoesNotExistMessage =
        "Account does not exist.";

    private const string GenericStatusMessage =
        "If an account matches that email address, a reset code will be sent. Check your inbox and spam folder.";

    /*
     * Tracks reset-code requests across browser sessions while the
     * application remains running.
     *
     * For production, use persistent database storage so an
     * application restart cannot reset the limit.
     */
    private sealed record RequestLimit(
        int SendCount,
        long? LockoutExpiresAtUnix);

    private static readonly ConcurrentDictionary<string, RequestLimit>
        RequestLimits = new(StringComparer.OrdinalIgnoreCase);

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public ResetInput Input { get; set; } = new();

    public bool CodeSent { get; set; }

    public bool CodeVerified { get; set; }

    public bool CanResendCode { get; set; } = true;

    public bool CanRequestCode { get; set; } = true;

    public string? StatusMessage { get; set; }

    // Both timestamps are Unix milliseconds for JavaScript.
    public long? CodeExpiresAtUnix { get; set; }

    public long? LockoutExpiresAtUnix { get; set; }

    public class ResetInput
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "Enter the six-digit verification code.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "The verification code must contain six digits.")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a new password.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Your password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ============================================================
    // PUBLIC CLEANUP METHODS
    // ============================================================

    public static void ClearRequestLimit(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var key = email.Trim().ToUpperInvariant();

        RequestLimits.TryRemove(key, out _);
    }

    public static void ClearResetSession(ISession session)
    {
        session.Remove(ResetUserIdKey);
        session.Remove(ResetEmailKey);
        session.Remove(ResetCodeHashKey);
        session.Remove(ResetExpiryKey);
        session.Remove(ResetVerifiedKey);
        session.Remove(ResetVerifiedExpiryKey);
        session.Remove(ResetSendCountKey);
    }

    // ============================================================
    // GET
    // ============================================================

    public void OnGet()
    {
        ClearResetState();
    }

    // ============================================================
    // SEND INITIAL RESET CODE
    // ============================================================

    public async System.Threading.Tasks.Task<IActionResult>
        OnPostSendCodeAsync()
    {
        ModelState.Clear();

        if (!ValidateEmail())
        {
            return Page();
        }

        var email = Input.Email.Trim();
        Input.Email = email;

        ClearResetState();
        Input.Email = email;

        // Check whether the account still exists first.
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // Deleted or nonexistent accounts do not get a reset countdown.
            ClearRequestLimit(email);

            StatusMessage = AccountDoesNotExistMessage;
            CodeSent = false;
            CodeVerified = false;
            CanRequestCode = true;
            CanResendCode = false;
            CodeExpiresAtUnix = null;
            LockoutExpiresAtUnix = null;

            return Page();
        }

        // Register a request only for an existing account.
        if (!TryRegisterCodeRequest(
                email,
                out var sendCount,
                out var lockoutExpiry))
        {
            HttpContext.Session.SetString(ResetEmailKey, email);

            CanRequestCode = false;
            CanResendCode = false;
            LockoutExpiresAtUnix = lockoutExpiry;

            StatusMessage =
                "You have used all 3 reset-code requests. Please try again when the countdown reaches 00:00:00.";

            return Page();
        }

        HttpContext.Session.SetString(ResetEmailKey, email);

        HttpContext.Session.SetString(
            ResetSendCountKey,
            sendCount.ToString());

        CodeSent = true;
        CanResendCode = sendCount < MaximumCodeSends;
        CanRequestCode = sendCount < MaximumCodeSends;
        LockoutExpiresAtUnix = lockoutExpiry;

        StatusMessage = BuildRequestStatus(sendCount);

        var sent = await GenerateAndSendCodeAsync(user, email);

        if (!sent)
        {
            ClearResetState();

            Input.Email = email;

            ModelState.AddModelError(
                string.Empty,
                "We couldn't send the reset email right now. Please try again later.");

            CodeSent = false;
            CanResendCode = false;

            SetLockoutForDisplay(email);

            return Page();
        }

        SetCodeExpiryForDisplay();
        SetLockoutForDisplay(email);

        return Page();
    }

    // ============================================================
    // RESEND RESET CODE
    // ============================================================

    public async System.Threading.Tasks.Task<IActionResult>
        OnPostResendCodeAsync()
    {
        ModelState.Clear();

        var email = Input.Email?.Trim();
        var storedEmail = HttpContext.Session.GetString(ResetEmailKey);

        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(storedEmail) ||
            !string.Equals(
                email,
                storedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            ClearResetState();

            ModelState.AddModelError(
                string.Empty,
                "Please start a new password reset request.");

            return Page();
        }

        Input.Email = storedEmail;

        // Re-check the account before allowing another code request.
        var userId = HttpContext.Session.GetString(ResetUserIdKey);

        var user = userId == null
            ? null
            : await _userManager.FindByIdAsync(userId);

        if (user == null ||
            !string.Equals(
                user.Email,
                storedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            ClearRequestLimit(storedEmail);
            ClearResetState();

            Input.Email = storedEmail;
            StatusMessage = AccountDoesNotExistMessage;

            CodeSent = false;
            CodeVerified = false;
            CanRequestCode = true;
            CanResendCode = false;
            CodeExpiresAtUnix = null;
            LockoutExpiresAtUnix = null;

            return Page();
        }

        if (!TryRegisterCodeRequest(
                storedEmail,
                out var sendCount,
                out var lockoutExpiry))
        {
            CodeSent = true;
            CodeVerified = false;
            CanResendCode = false;
            CanRequestCode = false;
            LockoutExpiresAtUnix = lockoutExpiry;

            SetCodeExpiryForDisplay();

            StatusMessage =
                "You have used all 3 reset-code requests. Please try again when the countdown reaches 00:00:00.";

            return Page();
        }

        HttpContext.Session.SetString(
            ResetSendCountKey,
            sendCount.ToString());

        CodeSent = true;
        CanResendCode = sendCount < MaximumCodeSends;
        CanRequestCode = sendCount < MaximumCodeSends;
        LockoutExpiresAtUnix = lockoutExpiry;

        StatusMessage = BuildRequestStatus(sendCount);

        var sent = await GenerateAndSendCodeAsync(user, storedEmail);

        if (!sent)
        {
            ClearResetState();

            Input.Email = storedEmail;

            ModelState.AddModelError(
                string.Empty,
                "We couldn't send the reset email right now. Please try again later.");

            CodeSent = false;
            CanResendCode = false;

            SetLockoutForDisplay(storedEmail);

            return Page();
        }

        SetCodeExpiryForDisplay();
        SetLockoutForDisplay(storedEmail);

        return Page();
    }

    // ============================================================
    // VERIFY RESET CODE
    // ============================================================

    public IActionResult OnPostVerifyCode()
    {
        ModelState.Clear();

        var email = Input.Email?.Trim();
        var submittedCode = Input.Code?.Trim();

        var storedEmail = HttpContext.Session.GetString(ResetEmailKey);
        var storedUserId = HttpContext.Session.GetString(ResetUserIdKey);
        var storedHash = HttpContext.Session.GetString(ResetCodeHashKey);
        var storedExpiry = HttpContext.Session.GetString(ResetExpiryKey);
        var verified = HttpContext.Session.GetString(ResetVerifiedKey);

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(submittedCode))
        {
            ModelState.AddModelError(
                string.Empty,
                "Enter your email address and verification code.");

            RestoreVerificationState();
            return Page();
        }

        if (!string.Equals(
                email,
                storedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                string.Empty,
                "This reset request is no longer valid. Please start a new request.");

            RestoreVerificationState();
            return Page();
        }

        // Do not allow the same code to be verified twice.
        if (verified == "true")
        {
            StatusMessage =
                "This verification code has already been used. Continue with your new password or start a new reset request.";

            CodeSent = true;
            CodeVerified = true;
            CanResendCode = false;
            CanRequestCode = false;
            CodeExpiresAtUnix = null;

            return Page();
        }

        if (storedExpiry == null ||
            !long.TryParse(storedExpiry, out var expiryTimestamp))
        {
            ModelState.AddModelError(
                string.Empty,
                "This reset code is no longer valid. Please request a new code.");

            RestoreVerificationState();
            return Page();
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= expiryTimestamp)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your reset code has expired. Request a new code to continue.");

            RestoreVerificationState();
            return Page();
        }

        if (storedUserId == null ||
            storedHash == null ||
            verified != "false")
        {
            ModelState.AddModelError(
                string.Empty,
                "This reset code is invalid. Please request a new code.");

            RestoreVerificationState();
            return Page();
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(
                string.Empty,
                "Please enter a valid email address.");

            RestoreVerificationState();
            return Page();
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(
                submittedCode,
                @"^\d{6}$"))
        {
            ModelState.AddModelError(
                nameof(Input.Code),
                "Enter the six-digit verification code.");

            RestoreVerificationState();
            return Page();
        }

        var submittedHash = HashCodeValue(storedUserId, submittedCode);

        var expectedBytes = Encoding.UTF8.GetBytes(storedHash);
        var submittedBytes = Encoding.UTF8.GetBytes(submittedHash);

        if (expectedBytes.Length != submittedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                submittedBytes))
        {
            ModelState.AddModelError(
                nameof(Input.Code),
                "The verification code is incorrect. Please check it and try again.");

            RestoreVerificationState();
            return Page();
        }

        // The code has been successfully verified.
        HttpContext.Session.SetString(ResetVerifiedKey, "true");

        HttpContext.Session.SetString(
            ResetVerifiedExpiryKey,
            DateTimeOffset.UtcNow
                .Add(VerifiedLifetime)
                .ToUnixTimeSeconds()
                .ToString());

        Input.Email = storedEmail!;

        CodeSent = true;
        CodeVerified = true;
        CanResendCode = false;
        CanRequestCode = false;
        CodeExpiresAtUnix = null;

        StatusMessage =
            "Your email has been verified. You can now create a new password.";

        return Page();
    }

    // ============================================================
    // RESET PASSWORD
    // ============================================================

    public async System.Threading.Tasks.Task<IActionResult>
        OnPostResetPasswordAsync()
    {
        ModelState.Clear();

        if (!TryValidateModel(Input, nameof(Input)))
        {
            CodeSent = true;
            CodeVerified = true;
            CanResendCode = false;
            CanRequestCode = false;

            return Page();
        }

        var storedUserId = HttpContext.Session.GetString(ResetUserIdKey);
        var storedEmail = HttpContext.Session.GetString(ResetEmailKey);
        var verified = HttpContext.Session.GetString(ResetVerifiedKey);
        var verifiedExpiry = HttpContext.Session.GetString(ResetVerifiedExpiryKey);

        if (storedUserId == null ||
            storedEmail == null ||
            verified != "true" ||
            verifiedExpiry == null ||
            !long.TryParse(verifiedExpiry, out var verifiedExpiryTimestamp) ||
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= verifiedExpiryTimestamp)
        {
            ClearResetState();

            ModelState.AddModelError(
                string.Empty,
                "Your verification has expired. Please request a new reset code.");

            return Page();
        }

        if (!string.Equals(
                Input.Email.Trim(),
                storedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            ClearResetState();

            ModelState.AddModelError(
                string.Empty,
                "The password could not be reset. Please request a new code.");

            return Page();
        }

        var user = await _userManager.FindByIdAsync(storedUserId);

        if (user == null ||
            !string.Equals(
                user.Email,
                storedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            ClearRequestLimit(storedEmail);
            ClearResetState();

            Input.Email = storedEmail;

            ModelState.AddModelError(
                string.Empty,
                AccountDoesNotExistMessage);

            return Page();
        }

        var resetToken =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            resetToken,
            Input.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            // Allow correction of password-policy errors within
            // the five-minute verification window.
            CodeSent = true;
            CodeVerified = true;
            CanResendCode = false;
            CanRequestCode = false;

            return Page();
        }

        ClearResetState();

        _logger.LogInformation(
            "Password successfully reset for user {UserId}.",
            user.Id);

        return RedirectToPage("/Account/SignIn/SignIn");
    }

    // ============================================================
    // REQUEST LIMIT
    // ============================================================

    private bool TryRegisterCodeRequest(
        string email,
        out int sendCount,
        out long? lockoutExpiry)
    {
        var key = email.Trim().ToUpperInvariant();

        while (true)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!RequestLimits.TryGetValue(key, out var current))
            {
                var firstRequest = new RequestLimit(1, null);

                if (RequestLimits.TryAdd(key, firstRequest))
                {
                    sendCount = 1;
                    lockoutExpiry = null;
                    return true;
                }

                continue;
            }

            if (current.LockoutExpiresAtUnix.HasValue)
            {
                if (now < current.LockoutExpiresAtUnix.Value)
                {
                    sendCount = current.SendCount;
                    lockoutExpiry = current.LockoutExpiresAtUnix;
                    return false;
                }

                // The 24-hour lockout has ended.
                if (RequestLimits.TryUpdate(
                        key,
                        new RequestLimit(0, null),
                        current))
                {
                    continue;
                }

                continue;
            }

            if (current.SendCount >= MaximumCodeSends)
            {
                var newLockoutExpiry = DateTimeOffset.UtcNow
                    .Add(RequestLockoutLifetime)
                    .ToUnixTimeMilliseconds();

                var locked = new RequestLimit(
                    MaximumCodeSends,
                    newLockoutExpiry);

                if (RequestLimits.TryUpdate(key, locked, current))
                {
                    sendCount = MaximumCodeSends;
                    lockoutExpiry = newLockoutExpiry;
                    return false;
                }

                continue;
            }

            var nextCount = current.SendCount + 1;

            long? nextLockoutExpiry = null;

            if (nextCount >= MaximumCodeSends)
            {
                nextLockoutExpiry = DateTimeOffset.UtcNow
                    .Add(RequestLockoutLifetime)
                    .ToUnixTimeMilliseconds();
            }

            var updated = new RequestLimit(
                nextCount,
                nextLockoutExpiry);

            if (RequestLimits.TryUpdate(key, updated, current))
            {
                sendCount = nextCount;
                lockoutExpiry = nextLockoutExpiry;
                return true;
            }
        }
    }

    private string BuildRequestStatus(int sendCount)
    {
        var remaining = MaximumCodeSends - sendCount;

        if (remaining == 2)
        {
            return $"{GenericStatusMessage} You have 2 more code requests available.";
        }

        if (remaining == 1)
        {
            return $"{GenericStatusMessage} You have 1 more code request available.";
        }

        return $"{GenericStatusMessage} You have used all 3 code requests. You can request another code when the 24-hour countdown ends.";
    }

    private void SetLockoutForDisplay(string email)
    {
        var key = email.Trim().ToUpperInvariant();

        if (RequestLimits.TryGetValue(key, out var limit) &&
            limit.LockoutExpiresAtUnix.HasValue &&
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <
            limit.LockoutExpiresAtUnix.Value)
        {
            LockoutExpiresAtUnix = limit.LockoutExpiresAtUnix;
            CanRequestCode = false;
            CanResendCode = false;
        }
        else
        {
            LockoutExpiresAtUnix = null;
            CanRequestCode = true;
        }
    }

    // ============================================================
    // VALIDATION AND EMAIL
    // ============================================================

    private bool ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Input.Email))
        {
            ModelState.AddModelError(
                "Input.Email",
                "Please enter your email address.");

            return false;
        }

        var email = Input.Email.Trim();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(
                "Input.Email",
                "Please enter a valid email address.");

            return false;
        }

        return true;
    }

    private async System.Threading.Tasks.Task<bool> GenerateAndSendCodeAsync(
        ApplicationUser user,
        string email)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6");

        var codeHash = HashCodeValue(user.Id, code);

        var expiresAt = DateTimeOffset.UtcNow.Add(CodeLifetime);

        HttpContext.Session.SetString(ResetUserIdKey, user.Id);
        HttpContext.Session.SetString(ResetEmailKey, email);
        HttpContext.Session.SetString(ResetCodeHashKey, codeHash);

        HttpContext.Session.SetString(
            ResetExpiryKey,
            expiresAt.ToUnixTimeMilliseconds().ToString());

        HttpContext.Session.SetString(ResetVerifiedKey, "false");
        HttpContext.Session.Remove(ResetVerifiedExpiryKey);

        try
        {
            var safeCode = System.Net.WebUtility.HtmlEncode(code);

            await _emailSender.SendEmailAsync(
                email,
                "KeepTrack Password Reset Code",
                $"""
                <div style="font-family: Arial, sans-serif; color: #333; line-height: 1.6;">
                    <h2>Reset your KeepTrack password</h2>
                    <p>We received a request to reset the password for your KeepTrack account.</p>
                    <p>Your verification code is:</p>
                    <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">
                        {safeCode}
                    </p>
                    <p>This code expires in 60 seconds and can only be used once.</p>
                    <p>If you did not request a password reset, you can ignore this email.</p>
                    <p>Best regards,<br>The KeepTrack Team</p>
                </div>
                """);

            _logger.LogInformation(
                "Password reset code email sent for user {UserId}.",
                user.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send a password reset code for user {UserId}.",
                user.Id);

            return false;
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void RestoreVerificationState()
    {
        var storedEmail = HttpContext.Session.GetString(ResetEmailKey);
        var sendCountValue = HttpContext.Session.GetString(ResetSendCountKey);

        Input.Email = storedEmail ?? Input.Email;

        CodeSent = !string.IsNullOrWhiteSpace(storedEmail);

        CodeVerified =
            HttpContext.Session.GetString(ResetVerifiedKey) == "true";

        CanResendCode =
            int.TryParse(sendCountValue, out var count) &&
            count < MaximumCodeSends &&
            !CodeVerified;

        CanRequestCode = CanResendCode;

        SetCodeExpiryForDisplay();

        if (!string.IsNullOrWhiteSpace(storedEmail))
        {
            SetLockoutForDisplay(storedEmail);
        }
    }

    private void SetCodeExpiryForDisplay()
    {
        var storedExpiry = HttpContext.Session.GetString(ResetExpiryKey);

        if (long.TryParse(storedExpiry, out var expiryTimestamp))
        {
            CodeExpiresAtUnix = expiryTimestamp;
        }
        else
        {
            CodeExpiresAtUnix = null;
        }
    }

    private string HashCodeValue(string userId, string code)
    {
        var bytes = Encoding.UTF8.GetBytes($"{userId}:{code}");

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private void ClearResetState()
    {
        ClearResetSession(HttpContext.Session);

        CodeSent = false;
        CodeVerified = false;
        CanResendCode = true;
        CanRequestCode = true;
        CodeExpiresAtUnix = null;
        LockoutExpiresAtUnix = null;
        StatusMessage = null;
    }
}
