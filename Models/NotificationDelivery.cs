
namespace keeptrack.Models;

public class NotificationDelivery
{
    public int Id { get; set; }

    // The notification being delivered.
    public int NotificationId { get; set; }

    public Notification? Notification { get; set; }

    // The browser/device receiving it.
    public int BrowserPushSubscriptionId { get; set; }

    public BrowserPushSubscription? BrowserPushSubscription { get; set; }

    // Pending, Sent, Failed, or Expired.
    public string Status { get; set; } = "Pending";

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public string? LastError { get; set; }
}
