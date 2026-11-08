
namespace keeptrack.Models;

public class Notification
{
    public int Id { get; set; }

    // The user who owns this notification.
    public string UserId { get; set; } = "";

    // OverdueTask, TaskReminder, or SystemUpdate.
    public string Type { get; set; } = "";

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    // When the notification was created (UTC).
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Whether the user has read it.
    public bool IsRead { get; set; }

    // Optional link to the task that caused the notification.
    public int? TaskId { get; set; }

    // Optional task navigation relationship.
    public Task? Task { get; set; }

    // The scheduled date and time this reminder represents.
    public DateTime? ScheduledFor { get; set; }
}
