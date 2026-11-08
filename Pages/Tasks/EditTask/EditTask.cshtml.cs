
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Tasks.EditTask;

public class EditTaskModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditTaskModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public EditTaskInput Input { get; set; } = new();

    public List<string> AllowedReminderUnits { get; private set; } = new()
    {
        "Hour"
    };

    public class EditTaskInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a task title.")]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Start immediately")]
        public bool StartImmediately { get; set; } = true;

        [Display(Name = "Starting Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Start Time")]
        public string? StartTime { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Due Time")]
        public string? DueTime { get; set; }

        [Display(Name = "Set your reminder")]
        public bool ReminderEnabled { get; set; }

        [Display(Name = "Reminder Value")]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Reminder value must be at least 1."
        )]
        public int? ReminderValue { get; set; }

        [Display(Name = "Reminder Unit")]
        public string? ReminderUnit { get; set; }

        [Display(Name = "Reminder Time")]
        public string? ReminderTime { get; set; }

        [Display(Name = "Reminder Interval")]
        public string? ReminderInterval { get; set; }

        [Display(Name = "Default Reminder")]
        public string ReminderTiming { get; set; } = "Halfway";

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }
    }

    // =====================================================
    // DATE AND TIME HELPERS
    // =====================================================

    private static DateTime? CombineDateAndTime(
        DateTime? date,
        string? time)
    {
        if (!date.HasValue ||
            !TimeOnly.TryParse(time, out var parsedTime))
        {
            return null;
        }

        return date.Value.Date.Add(parsedTime.ToTimeSpan());
    }

    private DateTime GetStartDateTime()
    {
        if (Input.StartImmediately)
        {
            return DateTime.Now;
        }

        return CombineDateAndTime(
            Input.StartDate,
            Input.StartTime
        ) ?? DateTime.MinValue;
    }

    private DateTime? GetDueDateTime()
    {
        return CombineDateAndTime(
            Input.DueDate,
            Input.DueTime
        );
    }

    // =====================================================
    // DURATION-BASED REMINDER UNITS
    // =====================================================

    private static List<string> GetAllowedReminderUnits(
        DateTime start,
        DateTime due)
    {
        var duration = due - start;

        if (duration <= TimeSpan.Zero)
        {
            return new List<string>();
        }

        // Tasks lasting up to 24 hours use Hour intervals.
        if (duration <= TimeSpan.FromHours(24))
        {
            return new List<string>
            {
                "Hour"
            };
        }

        // Longer tasks support these units.
        var units = new List<string>
        {
            "Hour",
            "Day",
            "Month"
        };

        // Add Week once the task spans at least one
        // calendar month.
        if (due >= start.AddMonths(1))
        {
            units.Add("Week");
        }

        // Add Year once the task spans at least one
        // calendar year.
        if (due >= start.AddYears(1))
        {
            units.Add("Year");
        }

        return units;
    }

    private void UpdateAllowedReminderUnits()
    {
        var due = GetDueDateTime();

        if (!due.HasValue)
        {
            AllowedReminderUnits = new List<string>
            {
                "Hour"
            };

            return;
        }

        var start = GetStartDateTime();

        if (start == DateTime.MinValue)
        {
            AllowedReminderUnits = new List<string>
            {
                "Hour"
            };

            return;
        }

        AllowedReminderUnits = GetAllowedReminderUnits(
            start,
            due.Value
        );
    }

    // =====================================================
    // VALIDATION: DATES AND TIMES
    // =====================================================

    private void ValidateDateAndTime()
    {
        DateTime? startDateTime = null;

        // A scheduled task requires both start date and time.
        if (!Input.StartImmediately)
        {
            if (!Input.StartDate.HasValue)
            {
                ModelState.AddModelError(
                    "Input.StartDate",
                    "Please select a schedule start date."
                );
            }

            if (!TimeOnly.TryParse(Input.StartTime, out _))
            {
                ModelState.AddModelError(
                    "Input.StartTime",
                    "Please select a valid schedule start time."
                );
            }

            startDateTime = CombineDateAndTime(
                Input.StartDate,
                Input.StartTime
            );
        }
        else
        {
            startDateTime = DateTime.Now;
        }

        // A due time cannot exist without a due date.
        if (!Input.DueDate.HasValue &&
            !string.IsNullOrWhiteSpace(Input.DueTime))
        {
            ModelState.AddModelError(
                "Input.DueDate",
                "Please select a due date when entering a due time."
            );
        }

        // A selected due date requires a valid due time.
        if (Input.DueDate.HasValue &&
            !TimeOnly.TryParse(Input.DueTime, out _))
        {
            ModelState.AddModelError(
                "Input.DueTime",
                "Please select a valid due time."
            );
        }

        var dueDateTime = GetDueDateTime();

        // If both dates are available, due must be after start.
        if (startDateTime.HasValue &&
            dueDateTime.HasValue &&
            dueDateTime.Value <= startDateTime.Value)
        {
            ModelState.AddModelError(
                "Input.DueDate",
                "The due date and time must be after the task start."
            );
        }
    }

    // =====================================================
    // VALIDATION: REMINDERS
    // =====================================================

    private void ValidateReminder()
    {
        if (!Input.ReminderEnabled)
        {
            return;
        }

        // Reminders require a due date and time.
        if (!Input.DueDate.HasValue ||
            !TimeOnly.TryParse(Input.DueTime, out _))
        {
            ModelState.AddModelError(
                "Input.ReminderEnabled",
                "Set a valid due date and time before enabling reminders."
            );

            return;
        }

        var due = GetDueDateTime();

        if (!due.HasValue)
        {
            return;
        }

        var start = GetStartDateTime();

        if (start == DateTime.MinValue ||
            due.Value <= start)
        {
            ModelState.AddModelError(
                "Input.ReminderEnabled",
                "Reminders require a due date and time after the task start."
            );

            return;
        }

        var allowedUnits = GetAllowedReminderUnits(
            start,
            due.Value
        );

        if (string.IsNullOrWhiteSpace(Input.ReminderUnit) ||
            !allowedUnits.Contains(
                Input.ReminderUnit,
                StringComparer.OrdinalIgnoreCase
            ))
        {
            ModelState.AddModelError(
                "Input.ReminderUnit",
                "Choose a reminder unit allowed by the task duration."
            );

            return;
        }

        // Hour reminders use an elapsed HH:mm:ss interval.
        if (string.Equals(
            Input.ReminderUnit,
            "Hour",
            StringComparison.OrdinalIgnoreCase))
        {
            var intervalText = Input.ReminderInterval?.Trim();

            var validFormat =
                !string.IsNullOrWhiteSpace(intervalText) &&
                Regex.IsMatch(
                    intervalText,
                    @"^\d+:[0-5]\d:[0-5]\d$"
                );

            if (!validFormat ||
                !TimeSpan.TryParse(
                    intervalText,
                    out var interval
                ) ||
                interval <= TimeSpan.Zero)
            {
                ModelState.AddModelError(
                    "Input.ReminderInterval",
                    "Enter a valid reminder interval in HH:mm:ss format."
                );

                return;
            }

            if (interval >= due.Value - start)
            {
                ModelState.AddModelError(
                    "Input.ReminderInterval",
                    "The reminder interval must be shorter than the task duration."
                );
            }

            return;
        }

        // Day, Week, Month and Year use a numeric interval
        // and a time of day.
        if (!Input.ReminderValue.HasValue ||
            Input.ReminderValue.Value < 1)
        {
            ModelState.AddModelError(
                "Input.ReminderValue",
                "Enter a reminder interval of at least 1."
            );
        }

        if (!TimeOnly.TryParse(Input.ReminderTime, out _))
        {
            ModelState.AddModelError(
                "Input.ReminderTime",
                "Select a valid reminder time."
            );
        }
    }

    // =====================================================
    // GET: LOAD EXISTING TASK
    // =====================================================

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(task =>
                task.Id == id &&
                task.UserId == user.Id
            );

        if (task == null)
        {
            return NotFound();
        }

        Input = new EditTaskInput
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,

            StartImmediately = task.StartImmediately,
            StartDate = task.StartDate,
            StartTime = task.StartDate.HasValue
                ? task.StartDate.Value.ToString("HH:mm")
                : null,

            DueDate = task.DueDate,
            DueTime = task.DueDate.HasValue
                ? task.DueDate.Value.ToString("HH:mm")
                : null,

            ReminderEnabled = task.ReminderEnabled,
            ReminderValue = task.ReminderValue,
            ReminderUnit = task.ReminderUnit,
            ReminderTime = Convert.ToString(task.ReminderTime),
            ReminderInterval = task.ReminderInterval,
            ReminderTiming = task.ReminderTiming,

            IsCompleted = task.IsCompleted
        };

        UpdateAllowedReminderUnits();

        ModelState.Clear();

        return Page();
    }

    // =====================================================
    // POST: SAVE CHANGES
    // =====================================================

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateDateAndTime();
        ValidateReminder();
        UpdateAllowedReminderUnits();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage("/Account/SignIn/SignIn");
        }

        var task = await _context.Tasks
            .FirstOrDefaultAsync(task =>
                task.Id == Input.Id &&
                task.UserId == user.Id
            );

        if (task == null)
        {
            return NotFound();
        }

        DateTime? startDate = null;

        if (!Input.StartImmediately)
        {
            startDate = CombineDateAndTime(
                Input.StartDate,
                Input.StartTime
            );
        }

        DateTime? dueDate = CombineDateAndTime(
            Input.DueDate,
            Input.DueTime
        );

        // =================================================
        // SAVE TASK DETAILS
        // =================================================

        task.Title = Input.Title.Trim();

        task.Description = string.IsNullOrWhiteSpace(Input.Description)
            ? string.Empty
            : Input.Description.Trim();

        task.IsCompleted = Input.IsCompleted;

        // =================================================
        // SAVE START AND DUE DATES
        // =================================================

        task.StartImmediately = Input.StartImmediately;
        task.StartDate = startDate;
        task.DueDate = dueDate;

        // =================================================
        // SAVE REMINDER SETTINGS
        // =================================================

        task.ReminderEnabled = Input.ReminderEnabled;

        if (Input.ReminderEnabled)
        {
            task.ReminderUnit = Input.ReminderUnit;

            if (string.Equals(
                Input.ReminderUnit,
                "Hour",
                StringComparison.OrdinalIgnoreCase))
            {
                task.ReminderValue = null;
                task.ReminderTime = null;
                task.ReminderInterval = Input.ReminderInterval?.Trim();
            }
            else
            {
                task.ReminderValue = Input.ReminderValue;
                task.ReminderTime = Input.ReminderTime;
                task.ReminderInterval = null;
            }
        }
        else
        {
            task.ReminderValue = null;
            task.ReminderUnit = null;
            task.ReminderTime = null;
            task.ReminderInterval = null;
        }

        task.ReminderTiming = Input.ReminderTiming;

        await _context.SaveChangesAsync();

        return RedirectToPage("/Tasks/Tasks");
    }
}
