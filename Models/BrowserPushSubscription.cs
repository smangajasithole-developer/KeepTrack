
namespace keeptrack.Models;

public class BrowserPushSubscription
{
    public int Id { get; set; }

    // The user who owns this browser subscription.
    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    // Browser push endpoint and encryption keys.
    public string Endpoint { get; set; } = "";

    public string P256dh { get; set; } = "";

    public string Auth { get; set; } = "";

    // Subscription management.
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}
