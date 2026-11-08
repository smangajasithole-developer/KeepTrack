
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace keeptrack.Pages.Tasks;

public class TasksModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TasksModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =====================================================
    // TASKS
    // =====================================================

    public List<keeptrack.Models.Task> Tasks { get; set; } = new();

    // =====================================================
    // DATE FORMAT
    // =====================================================

    public string DateFormat { get; set; } = "yyyy/MM/dd";

    // =====================================================
    // FILTER
    // =====================================================

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    // =====================================================
    // SEARCH
    // =====================================================

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = "";

    // =====================================================
    // START DATE DISPLAY
    // =====================================================

    public string GetStartDisplay(keeptrack.Models.Task task)
    {
        if (!task.StartDate.HasValue)
        {
            return task.StartImmediately
                ? "Right away — start time unavailable"
                : "Not scheduled";
        }

        string startDateTime = task.StartDate.Value.ToString(
            $"{DateFormat} 'at' HH:mm",
            CultureInfo.InvariantCulture
        );

        return task.StartImmediately
            ? $"Right away — {startDateTime}"
            : startDateTime;
    }

    // =====================================================
    // REMINDER DISPLAY
    // =====================================================

    public string GetReminderDisplay(keeptrack.Models.Task task)
    {
        if (!task.ReminderEnabled)
        {
            return "Off";
        }

        if (
            string.Equals(
                task.ReminderUnit,
                "Hour",
                StringComparison.OrdinalIgnoreCase
            )
            && !string.IsNullOrWhiteSpace(task.ReminderInterval)
        )
        {
            return $"Every {task.ReminderInterval}";
        }

        var reminder = $"Every {task.ReminderValue} {task.ReminderUnit}";

        var reminderTime = Convert.ToString(task.ReminderTime);

        if (!string.IsNullOrWhiteSpace(reminderTime))
        {
            reminder += $" at {reminderTime}";
        }

        return reminder;
    }

    // =====================================================
    // GET
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var userSettings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.UserId == user.Id);

        string[] allowedDateFormats =
        {
            "yyyy/MM/dd",
            "dd/MM/yyyy",
            "dd MMM yyyy",
            "yyyy MMM dd",
            "MMMM dd, yyyy"
        };

        if (
            userSettings != null &&
            !string.IsNullOrWhiteSpace(userSettings.DateFormat) &&
            allowedDateFormats.Contains(userSettings.DateFormat)
        )
        {
            DateFormat = userSettings.DateFormat;
        }

        // Always restrict results to the signed-in user's tasks.
        var query = _context.Tasks
            .Where(task => task.UserId == user.Id);

        Filter = (Filter ?? "all").Trim().ToLowerInvariant();
        SearchTerm = (SearchTerm ?? "").Trim();

        var now = DateTime.Now;

        // Apply the selected task status filter.
        switch (Filter)
        {
            case "pending":
                query = query.Where(task =>
                    !task.IsCompleted &&
                    (!task.DueDate.HasValue || task.DueDate.Value > now)
                );
                break;

            case "overdue":
                query = query.Where(task =>
                    !task.IsCompleted &&
                    task.DueDate.HasValue &&
                    task.DueDate.Value <= now
                );
                break;

            case "completed":
                query = query.Where(task => task.IsCompleted);
                break;

            case "all":
                break;

            default:
                Filter = "all";
                break;
        }

        // Search task titles and descriptions.
        // PostgreSQL ILIKE provides case-insensitive matching.
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var pattern = $"%{SearchTerm}%";

            query = query.Where(task =>
                EF.Functions.ILike(task.Title, pattern) ||
                (
                    task.Description != null &&
                    EF.Functions.ILike(task.Description, pattern)
                )
            );
        }

        Tasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync();

        return Page();
    }

    // =====================================================
    // DELETE
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(task =>
                task.Id == id &&
                task.UserId == user.Id
            );

        if (task == null)
        {
            return NotFound();
        }

        // Preserve notification history without retaining a task reference.
        var relatedNotifications = await _context.Notifications
            .Where(notification =>
                notification.TaskId == task.Id &&
                notification.UserId == user.Id)
            .ToListAsync();

        foreach (var notification in relatedNotifications)
        {
            notification.TaskId = null;
        }

        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    // =====================================================
    // COMPLETE
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(task =>
                task.Id == id &&
                task.UserId == user.Id
            );

        if (task == null)
        {
            return NotFound();
        }

        task.IsCompleted = true;

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
