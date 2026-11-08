
using keeptrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Data;

public class PostgresKeepTrackDbContext
    : IdentityDbContext<ApplicationUser>
{
    public PostgresKeepTrackDbContext(
        DbContextOptions<PostgresKeepTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<keeptrack.Models.Task> Tasks { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<Feedback> Feedback { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<BrowserPushSubscription> BrowserPushSubscriptions { get; set; }
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =====================================================
        // TASK DATE CONFIGURATION
        // =====================================================

        // Task scheduling uses South African local wall-clock time.
        builder.Entity<keeptrack.Models.Task>()
            .Property(task => task.StartDate)
            .HasColumnType("timestamp without time zone");

        builder.Entity<keeptrack.Models.Task>()
            .Property(task => task.DueDate)
            .HasColumnType("timestamp without time zone");


        // =====================================================
        // NOTIFICATION CONFIGURATION
        // =====================================================

        // Scheduled reminder occurrences use the same local time.
        builder.Entity<Notification>()
            .Property(notification => notification.ScheduledFor)
            .HasColumnType("timestamp without time zone");

        // Prevent duplicate TaskReminder occurrences.
        builder.Entity<Notification>()
            .HasIndex(notification => new
            {
                notification.UserId,
                notification.TaskId,
                notification.ScheduledFor
            })
            .HasDatabaseName("IX_Notifications_TaskReminderOccurrence")
            .IsUnique()
            .HasFilter(
                "\"Type\" = 'TaskReminder' " +
                "AND \"TaskId\" IS NOT NULL " +
                "AND \"ScheduledFor\" IS NOT NULL"
            );

        // Prevent more than one OverdueTask notification per task and user.
        builder.Entity<Notification>()
            .HasIndex(notification => new
            {
                notification.UserId,
                notification.TaskId
            })
            .HasDatabaseName("IX_Notifications_OverdueTask")
            .IsUnique()
            .HasFilter(
                "\"Type\" = 'OverdueTask' " +
                "AND \"TaskId\" IS NOT NULL"
            );


        // =====================================================
        // BROWSER PUSH SUBSCRIPTIONS
        // =====================================================

        builder.Entity<BrowserPushSubscription>(entity =>
        {
            entity.HasKey(subscription => subscription.Id);

            entity.Property(subscription => subscription.Endpoint)
                .IsRequired();

            entity.Property(subscription => subscription.P256dh)
                .IsRequired();

            entity.Property(subscription => subscription.Auth)
                .IsRequired();

            entity.HasIndex(subscription => subscription.Endpoint)
                .IsUnique();

            entity.HasIndex(subscription => subscription.UserId);

            entity.HasOne(subscription => subscription.User)
                .WithMany()
                .HasForeignKey(subscription => subscription.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // NOTIFICATION DELIVERY TRACKING
        // =====================================================

        builder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(delivery => delivery.Id);

            entity.Property(delivery => delivery.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(delivery => new
            {
                delivery.NotificationId,
                delivery.BrowserPushSubscriptionId
            })
            .IsUnique();

            entity.HasIndex(delivery => new
            {
                delivery.Status,
                delivery.NextAttemptAt
            });

            entity.HasOne(delivery => delivery.Notification)
                .WithMany()
                .HasForeignKey(delivery => delivery.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(delivery => delivery.BrowserPushSubscription)
                .WithMany()
                .HasForeignKey(delivery => delivery.BrowserPushSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
