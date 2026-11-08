
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Dashboard;

public class DashboardModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private static readonly string[] SupportedDateFormats =
    {
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd MMM yyyy",
        "yyyy MMM dd",
        "MMMM dd, yyyy"
    };

    // Saved user preferences.
    public string DateFormat { get; set; } = "yyyy/MM/dd";

    public string StartPage { get; set; } = "Dashboard";

    // Current local date and time.
    public DateTime UserNow { get; private set; }

    // Dashboard counts.
    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int PendingTasks { get; set; }

    public int OverdueTasks { get; set; }

    // Task collections.
    public List<keeptrack.Models.Task> RecentTasks { get; set; } = new();

    public List<keeptrack.Models.Task> PendingTaskList { get; set; } = new();

    public List<keeptrack.Models.Task> OverdueTaskList { get; set; } = new();

    public List<keeptrack.Models.Task> CalendarTasks { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            SetUserNow();
            return Page();
        }

        // Load the user's remaining general settings.
        var userSettings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.UserId == user.Id);

        if (userSettings != null)
        {
            if (!string.IsNullOrWhiteSpace(userSettings.DateFormat) &&
                SupportedDateFormats.Contains(userSettings.DateFormat))
            {
                DateFormat = userSettings.DateFormat;
            }

            if (!string.IsNullOrWhiteSpace(userSettings.StartPage))
            {
                StartPage = userSettings.StartPage;
            }
        }

        SetUserNow();

        // Task start and due dates are stored as wall-clock values.
        DateTime now = DateTime.SpecifyKind(
            UserNow,
            DateTimeKind.Unspecified);

        var query = _context.Tasks
            .Where(task => task.UserId == user.Id);

        // OVERVIEW
        TotalTasks = await query.CountAsync();

        CompletedTasks = await query
            .CountAsync(task => task.IsCompleted);

        OverdueTasks = await query.CountAsync(task =>
            !task.IsCompleted &&
            task.DueDate.HasValue &&
            task.DueDate.Value <= now);

        PendingTasks = await query.CountAsync(task =>
            !task.IsCompleted &&
            (!task.DueDate.HasValue ||
             task.DueDate.Value > now));

        // RECENT TASKS
        RecentTasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .Take(5)
            .ToListAsync();

        // PENDING TASKS
        PendingTaskList = await query
            .Where(task =>
                !task.IsCompleted &&
                (!task.DueDate.HasValue ||
                 task.DueDate.Value > now))
            .OrderBy(task => task.DueDate.HasValue ? 0 : 1)
            .ThenBy(task => task.DueDate)
            .Take(5)
            .ToListAsync();

        // OVERDUE TASKS
        OverdueTaskList = await query
            .Where(task =>
                !task.IsCompleted &&
                task.DueDate.HasValue &&
                task.DueDate.Value <= now)
            .OrderByDescending(task => task.DueDate)
            .Take(5)
            .ToListAsync();

        // CALENDAR TASKS
        CalendarTasks = await query
            .Where(task =>
                task.StartDate.HasValue ||
                task.DueDate.HasValue)
            .OrderBy(task => task.DueDate ?? task.StartDate)
            .ToListAsync();

        return Page();
    }

    private void SetUserNow()
    {
        UserNow = DateTime.Now;
    }

    // CreatedAt is stored in UTC. Display it in server-local time.
    public string FormatCreatedAt(DateTime createdAt)
    {
        DateTime utcCreatedAt = DateTime.SpecifyKind(
            createdAt,
            DateTimeKind.Utc);

        return utcCreatedAt
            .ToLocalTime()
            .ToString(DateFormat);
    }

    // Use the current local time for overdue checks.
    public bool IsOverdue(keeptrack.Models.Task task)
    {
        DateTime now = DateTime.SpecifyKind(
            UserNow,
            DateTimeKind.Unspecified);

        return !task.IsCompleted &&
               task.DueDate.HasValue &&
               task.DueDate.Value <= now;
    }
}
