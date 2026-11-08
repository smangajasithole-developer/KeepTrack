
using System.ComponentModel.DataAnnotations;
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Feedback;

public class FeedbackModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<FeedbackModel> _logger;

    public FeedbackModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<FeedbackModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public FeedbackInputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class FeedbackInputModel
    {
        [Required(ErrorMessage = "Please select a feedback category.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your feedback.")]
        [StringLength(
            2000,
            MinimumLength = 10,
            ErrorMessage = "Feedback must be between 10 and 2000 characters.")]
        public string Message { get; set; } = string.Empty;
    }

    private static readonly string[] AllowedCategories =
    {
        "Suggestion",
        "Bug Report",
        "General Feedback"
    };

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        Input.Category = Input.Category?.Trim() ?? string.Empty;
        Input.Message = Input.Message?.Trim() ?? string.Empty;

        if (!AllowedCategories.Contains(Input.Category))
        {
            ModelState.AddModelError(
                "Input.Category",
                "Please select a valid feedback category.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var feedback = new keeptrack.Models.Feedback
        {
            UserId = user.Id,
            Category = Input.Category,
            Message = Input.Message,
            SubmittedAt = DateTime.UtcNow
        };

        try
        {
            _context.Feedback.Add(feedback);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Feedback {FeedbackId} saved for user {UserId}.",
                feedback.Id,
                user.Id);

            SuccessMessage = "Thank you! Your feedback has been submitted successfully.";
            Input = new FeedbackInputModel();

            return RedirectToPage("/Feedback/Feedback");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to save feedback for user {UserId}.",
                user.Id);

            ErrorMessage = "We couldn't save your feedback. Please try again.";
            return RedirectToPage("/Feedback/Feedback");
        }
    }
}
