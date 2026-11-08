
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Statistics;

public class StatisticsModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StatisticsModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // USER PREFERENCES
    public string DateFormat { get; set; } = "yyyy/MM/dd";

    private static readonly string[] SupportedDateFormats =
    {
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd MMM yyyy",
        "yyyy MMM dd",
        "MMMM dd, yyyy"
    };

    // OVERVIEW
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int CompletionPercentage { get; set; }

    // SCHEDULING INSIGHTS
    public int ScheduledTasks { get; set; }
    public int StartImmediatelyTasks { get; set; }
    public int TasksWithDueDate { get; set; }
    public int TasksWithoutDueDate { get; set; }

    // TASK ACTIVITY
    public List<ActivityData> Activity { get; set; } = new();

    public class ActivityData
    {
        public string Label { get; set; } = string.Empty;
        public string FullDateLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Page();
        }

        // Load the user's saved date format.
        var settings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.UserId == user.Id);

        if (settings != null &&
            !string.IsNullOrWhiteSpace(settings.DateFormat) &&
            SupportedDateFormats.Contains(settings.DateFormat))
        {
            DateFormat = settings.DateFormat;
        }

        var query = _context.Tasks
            .Where(task => task.UserId == user.Id);

        // Use the current server-local time for task deadline comparisons.
        // Browser-based date and time displays use the device's local time.
        DateTime localNow = DateTime.Now;

        // OVERVIEW
        TotalTasks = await query.CountAsync();

        CompletedTasks = await query.CountAsync(
            task => task.IsCompleted);

        OverdueTasks = await query.CountAsync(task =>
            !task.IsCompleted &&
            task.DueDate.HasValue &&
            task.DueDate.Value < localNow);

        PendingTasks = await query.CountAsync(task =>
            !task.IsCompleted &&
            (!task.DueDate.HasValue ||
             task.DueDate.Value >= localNow));

        CompletionPercentage = TotalTasks > 0
            ? (int)Math.Round(
                (double)CompletedTasks / TotalTasks * 100)
            : 0;

        // SCHEDULING INSIGHTS
        ScheduledTasks = await query.CountAsync(
            task => !task.StartImmediately);

        StartImmediatelyTasks = await query.CountAsync(
            task => task.StartImmediately);

        TasksWithDueDate = await query.CountAsync(
            task => task.DueDate.HasValue);

        TasksWithoutDueDate = await query.CountAsync(
            task => !task.DueDate.HasValue);

        // TASK ACTIVITY: the last seven calendar days.
        DateTime today = DateTime.Today;
        DateTime startDate = today.AddDays(-6);
        DateTime endDate = today.AddDays(1);

        // CreatedAt is stored in UTC. Convert the local date boundaries
        // to UTC so the database query covers the correct date range.
        DateTime startUtc = startDate.ToUniversalTime();
        DateTime endUtc = endDate.ToUniversalTime();

        var activityRecords = await query
            .Where(task =>
                task.CreatedAt >= startUtc &&
                task.CreatedAt < endUtc)
            .Select(task => task.CreatedAt)
            .ToListAsync();

        var activityData = activityRecords
            .Select(createdAt =>
            {
                // CreatedAt values are stored in UTC.
                DateTime utcCreatedAt = DateTime.SpecifyKind(
                    createdAt,
                    DateTimeKind.Utc);

                return utcCreatedAt.ToLocalTime().Date;
            })
            .GroupBy(date => date)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        Activity = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                DateTime date = startDate.AddDays(offset);

                activityData.TryGetValue(date, out int count);

                return new ActivityData
                {
                    Label = date.ToString("ddd"),
                    FullDateLabel = date.ToString(DateFormat),
                    Count = count
                };
            })
            .ToList();

        return Page();
    }
}
