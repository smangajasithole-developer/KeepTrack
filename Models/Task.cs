
namespace keeptrack.Models;

public class Task
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }


    // =====================================================
    // TASK DATES
    // =====================================================

    public bool StartImmediately { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }


    // =====================================================
    // REMINDER
    // =====================================================

    public bool ReminderEnabled { get; set; }

    public int? ReminderValue { get; set; }

    // Supported values: Hour, Day, Week, Month, Year
    public string? ReminderUnit { get; set; }

    // For Day/Week/Month/Year reminders: time of day, HH:mm
    public string? ReminderTime { get; set; }

    // For Hour reminders: elapsed interval, HH:mm:ss
    public string? ReminderInterval { get; set; }

    public string ReminderTiming { get; set; } = "Halfway";


    // =====================================================
    // USER
    // =====================================================

    public string UserId { get; set; } = "";

    public ApplicationUser User { get; set; } = null!;
}
