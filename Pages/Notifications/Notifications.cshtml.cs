
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Notifications;

public class NotificationsModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Notification> Notifications { get; set; } = new();

    public int UnreadCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        await CreateOverdueNotificationsAsync(user.Id);

        Notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        UnreadCount = Notifications.Count(n => !n.IsRead);

        return Page();
    }

    private async System.Threading.Tasks.Task CreateOverdueNotificationsAsync(string userId)
    {
        // Task dates are stored as local task date/time values.
        var now = DateTime.Now;

        // Find unfinished tasks whose due date and time have passed.
        var overdueTasks = await _context.Tasks
            .Where(t =>
                t.UserId == userId &&
                !t.IsCompleted &&
                t.DueDate.HasValue &&
                t.DueDate.Value <= now)
            .ToListAsync();

        if (overdueTasks.Count == 0)
        {
            return;
        }

        // Find tasks that already have an overdue notification.
        // This prevents duplicate notifications on subsequent visits.
        var overdueTaskIds = overdueTasks
            .Select(t => t.Id)
            .ToList();

        var existingTaskIds = await _context.Notifications
            .Where(n =>
                n.UserId == userId &&
                n.Type == "OverdueTask" &&
                n.TaskId.HasValue &&
                overdueTaskIds.Contains(n.TaskId.Value))
            .Select(n => n.TaskId!.Value)
            .Distinct()
            .ToListAsync();

        var existingIds = existingTaskIds.ToHashSet();

        foreach (var task in overdueTasks)
        {
            if (existingIds.Contains(task.Id))
            {
                continue;
            }

            var dueDate = task.DueDate!.Value;

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = "OverdueTask",
                Title = "Task overdue",
                Message = $"Your task \"{task.Title}\" was due on " +
                          $"{dueDate:dd MMM yyyy, HH:mm} and is not yet completed.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TaskId = task.Id
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == id && n.UserId == user.Id);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        if (unreadNotifications.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
