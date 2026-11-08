
using keeptrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Data;

public class KeepTrackDbContext : IdentityDbContext<ApplicationUser>
{
    public KeepTrackDbContext(
        DbContextOptions<KeepTrackDbContext> options)
        : base(options)
    {
    }

    // =====================================================
    // TASKS
    // =====================================================

    public DbSet<keeptrack.Models.Task> Tasks { get; set; }

    // =====================================================
    // USER SETTINGS
    // =====================================================

    public DbSet<UserSettings> UserSettings { get; set; }

    // =====================================================
    // FEEDBACK
    // =====================================================

    public DbSet<Feedback> Feedback { get; set; }

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    public DbSet<Notification> Notifications { get; set; }
}
