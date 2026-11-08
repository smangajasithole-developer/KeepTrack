
using System.Globalization;
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace keeptrack.Services;

public class TaskReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskReminderBackgroundService> _logger;

    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(30);

    public TaskReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TaskReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while processing task reminders.");
            }

            try
            {
                await System.Threading.Tasks.Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async System.Threading.Tasks.Task ProcessRemindersAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<PostgresKeepTrackDbContext>();

        // Task dates are stored as local wall-clock values.
        var now = DateTime.SpecifyKind(
            DateTime.Now,
            DateTimeKind.Unspecified);

        var tasks = await context.Tasks
            .Where(t =>
                t.ReminderEnabled &&
                !t.IsCompleted &&
                t.StartDate.HasValue &&
                t.DueDate.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var start = DateTime.SpecifyKind(
                task.StartDate!.Value,
                DateTimeKind.Unspecified);

            var due = DateTime.SpecifyKind(
                task.DueDate!.Value,
                DateTimeKind.Unspecified);

            if (now < start || due <= start)
            {
                continue;
            }

            var scheduledFor = GetLatestReminderOccurrence(
                task,
                start,
                now);

            if (!scheduledFor.HasValue)
            {
                continue;
            }

            if (scheduledFor.Value < start ||
                scheduledFor.Value > now)
            {
                continue;
            }

            var alreadyExists = await context.Notifications
                .AnyAsync(n =>
                    n.UserId == task.UserId &&
                    n.TaskId == task.Id &&
                    n.Type == "TaskReminder" &&
                    n.ScheduledFor == scheduledFor.Value,
                    cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            context.Notifications.Add(new Notification
            {
                UserId = task.UserId,
                TaskId = task.Id,
                Type = "TaskReminder",
                Title = "Task reminder",
                Message =
                    $"Reminder for \"{task.Title}\". " +
                    $"It was due on {due:dd MMM yyyy, HH:mm}." +
                    (now > due
                        ? " This task is overdue and still needs to be completed."
                        : ""),
                CreatedAt = DateTime.UtcNow,
                ScheduledFor = scheduledFor.Value,
                IsRead = false
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static DateTime? GetLatestReminderOccurrence(
        keeptrack.Models.Task task,
        DateTime start,
        DateTime now)
    {
        if (string.Equals(
                task.ReminderUnit,
                "Hour",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseInterval(
                    task.ReminderInterval,
                    out var interval))
            {
                return null;
            }

            var elapsed = now - start;

            if (elapsed < interval)
            {
                return null;
            }

            long hourOccurrenceNumber =
                elapsed.Ticks / interval.Ticks;

            if (hourOccurrenceNumber < 1)
            {
                return null;
            }

            var scheduled = start.AddTicks(
                hourOccurrenceNumber * interval.Ticks);

            return scheduled <= now ? scheduled : null;
        }

        if (!task.ReminderValue.HasValue ||
            task.ReminderValue.Value < 1 ||
            !TimeOnly.TryParse(
                task.ReminderTime,
                CultureInfo.InvariantCulture,
                out var reminderTime))
        {
            return null;
        }

        int value = task.ReminderValue.Value;
        int occurrenceNumber;
        var reminderOffset = reminderTime.ToTimeSpan();

        switch (task.ReminderUnit)
        {
            case "Day":
            {
                var elapsedDays = (now.Date - start.Date).TotalDays;
                occurrenceNumber = (int)(elapsedDays / value);

                var candidate = start.Date
                    .AddDays((double)occurrenceNumber * value)
                    .Add(reminderOffset);

                if (candidate < start)
                {
                    occurrenceNumber++;
                }
                else if (candidate > now)
                {
                    occurrenceNumber--;
                }

                if (occurrenceNumber < 0)
                {
                    return null;
                }

                var scheduled = start.Date
                    .AddDays((double)occurrenceNumber * value)
                    .Add(reminderOffset);

                return scheduled >= start && scheduled <= now
                    ? scheduled
                    : null;
            }

            case "Week":
            {
                int days = checked(value * 7);
                var elapsedDays = (now.Date - start.Date).TotalDays;
                occurrenceNumber = (int)(elapsedDays / days);

                var candidate = start.Date
                    .AddDays((double)occurrenceNumber * days)
                    .Add(reminderOffset);

                if (candidate < start)
                {
                    occurrenceNumber++;
                }
                else if (candidate > now)
                {
                    occurrenceNumber--;
                }

                if (occurrenceNumber < 0)
                {
                    return null;
                }

                var scheduled = start.Date
                    .AddDays((double)occurrenceNumber * days)
                    .Add(reminderOffset);

                return scheduled >= start && scheduled <= now
                    ? scheduled
                    : null;
            }

            case "Month":
            {
                int monthsElapsed =
                    (now.Year - start.Year) * 12 +
                    now.Month - start.Month;

                occurrenceNumber = monthsElapsed / value;

                var candidate = start.Date
                    .AddMonths(checked(occurrenceNumber * value))
                    .Add(reminderOffset);

                if (candidate < start)
                {
                    occurrenceNumber++;
                }
                else if (candidate > now)
                {
                    occurrenceNumber--;
                }

                if (occurrenceNumber < 0)
                {
                    return null;
                }

                var scheduled = start.Date
                    .AddMonths(checked(occurrenceNumber * value))
                    .Add(reminderOffset);

                return scheduled >= start && scheduled <= now
                    ? scheduled
                    : null;
            }

            case "Year":
            {
                int yearsElapsed = now.Year - start.Year;
                occurrenceNumber = yearsElapsed / value;

                var candidate = start.Date
                    .AddYears(checked(occurrenceNumber * value))
                    .Add(reminderOffset);

                if (candidate < start)
                {
                    occurrenceNumber++;
                }
                else if (candidate > now)
                {
                    occurrenceNumber--;
                }

                if (occurrenceNumber < 0)
                {
                    return null;
                }

                var scheduled = start.Date
                    .AddYears(checked(occurrenceNumber * value))
                    .Add(reminderOffset);

                return scheduled >= start && scheduled <= now
                    ? scheduled
                    : null;
            }

            default:
                return null;
        }
    }

    private static bool TryParseInterval(
        string? value,
        out TimeSpan interval)
    {
        interval = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] formats =
        {
            @"hh\:mm\:ss",
            @"h\:mm\:ss"
        };

        return TimeSpan.TryParseExact(
                   value.Trim(),
                   formats,
                   CultureInfo.InvariantCulture,
                   out interval)
               && interval > TimeSpan.Zero;
    }
}
