
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using keeptrack.Data;
using keeptrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace keeptrack.Pages.Tasks.AddTask;

public class AddTaskModel : PageModel
{
    private readonly PostgresKeepTrackDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private const string GuestDraftKey = "KeepTrack.PendingGuestTask";
    private const string GuestDraftConfirmationKey =
        "KeepTrack.ConfirmGuestTask";

    private const string ResumeDraftUrl =
        "/Tasks/AddTask?handler=ResumeDraft";

    private static readonly JsonSerializerOptions JsonOptions = new();

    public AddTaskModel(
        PostgresKeepTrackDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public AddTaskInput Input { get; set; } = new();

    public string DateFormat { get; private set; } = "yyyy/MM/dd";

    public bool IsConfirmingGuestDraft { get; private set; }

    private DateTime UserNow { get; set; }

    private static readonly string[] SupportedDateFormats =
    {
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd MMM yyyy",
        "yyyy MMM dd",
        "MMMM dd, yyyy"
    };

    public class AddTaskInput
    {
        [Required(ErrorMessage = "Please enter a task title.")]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Start immediately")]
        public bool StartImmediately { get; set; } = true;

        [Display(Name = "Scheduled Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Scheduled Start Time")]
        public string? StartTime { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Due Time")]
        public string? DueTime { get; set; }

        [Display(Name = "Set your reminder")]
        public bool ReminderEnabled { get; set; }

        [Display(Name = "Remind me every")]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Reminder interval must be at least 1.")]
        public int? ReminderValue { get; set; }

        [Display(Name = "Repeat unit")]
        public string? ReminderUnit { get; set; }

        [Display(Name = "Time of day")]
        public string? ReminderTime { get; set; }

        [Display(Name = "Remind me every after")]
        public string? ReminderInterval { get; set; }
    }

    // =====================================================
    // LOAD USER SETTINGS
    // =====================================================

    private async System.Threading.Tasks.Task LoadUserSettingsAsync()
    {
        DateFormat = "yyyy/MM/dd";

        var user = await _userManager.GetUserAsync(User);

        if (user != null)
        {
            var settings = await _context.UserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    settings => settings.UserId == user.Id);

            if (settings != null &&
                !string.IsNullOrWhiteSpace(settings.DateFormat) &&
                SupportedDateFormats.Contains(settings.DateFormat))
            {
                DateFormat = settings.DateFormat;
            }
        }

        SetUserNow();
    }

    private void SetUserNow()
    {
        UserNow = DateTime.SpecifyKind(
            DateTime.Now,
            DateTimeKind.Unspecified);
    }

    // =====================================================
    // NORMAL GET
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
    {
        await LoadUserSettingsAsync();

        return Page();
    }

    // =====================================================
    // RESUME GUEST DRAFT AFTER SIGN-IN OR SIGN-UP
    // URL: /Tasks/AddTask?handler=ResumeDraft
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult>
        OnGetResumeDraftAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToPage(
                "/Account/SignIn/SignIn",
                new { returnUrl = ResumeDraftUrl });
        }

        var draftJson = HttpContext.Session.GetString(GuestDraftKey);

        if (string.IsNullOrWhiteSpace(draftJson))
        {
            HttpContext.Session.Remove(GuestDraftConfirmationKey);

            return RedirectToPage();
        }

        AddTaskInput? draft;

        try
        {
            draft = JsonSerializer.Deserialize<AddTaskInput>(
                draftJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            draft = null;
        }

        if (draft == null)
        {
            ClearGuestDraft();
            return RedirectToPage();
        }

        Input = draft;

        HttpContext.Session.SetString(
            GuestDraftConfirmationKey,
            "true");

        IsConfirmingGuestDraft = true;

        await LoadUserSettingsAsync();

        return Page();
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

        return DateTime.SpecifyKind(
            date.Value.Date.Add(parsedTime.ToTimeSpan()),
            DateTimeKind.Unspecified);
    }

    private static bool TryParseReminderInterval(
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

    // =====================================================
    // AVAILABLE REMINDER UNITS
    // =====================================================

    private static HashSet<string> GetAllowedReminderUnits(
        DateTime start,
        DateTime due)
    {
        var allowed = new HashSet<string>
        {
            "Hour"
        };

        TimeSpan duration = due - start;

        if (duration <= TimeSpan.FromHours(24))
        {
            return allowed;
        }

        allowed.Add("Day");

        if (due < start.AddMonths(1))
        {
            return allowed;
        }

        allowed.Add("Month");
        allowed.Add("Week");

        if (due < start.AddYears(1))
        {
            return allowed;
        }

        allowed.Add("Year");

        return allowed;
    }

    // =====================================================
    // VALIDATE FORM
    // =====================================================

    private void ValidateForm(DateTime now)
    {
        DateTime? startDate;

        if (!Input.StartImmediately)
        {
            if (!Input.StartDate.HasValue)
            {
                ModelState.AddModelError(
                    "Input.StartDate",
                    "Please select a scheduled start date.");
            }

            if (!TimeOnly.TryParse(Input.StartTime, out _))
            {
                ModelState.AddModelError(
                    "Input.StartTime",
                    "Please select a valid scheduled start time.");
            }

            startDate = CombineDateAndTime(
                Input.StartDate,
                Input.StartTime);

            if (startDate.HasValue && startDate.Value <= now)
            {
                ModelState.AddModelError(
                    "Input.StartDate",
                    "The scheduled start must be in the future.");
            }
        }
        else
        {
            startDate = now;
        }

        DateTime? dueDate = CombineDateAndTime(
            Input.DueDate,
            Input.DueTime);

        if (Input.DueDate.HasValue &&
            !TimeOnly.TryParse(Input.DueTime, out _))
        {
            ModelState.AddModelError(
                "Input.DueTime",
                "Please select a valid due time.");
        }

        if (!Input.DueDate.HasValue &&
            !string.IsNullOrWhiteSpace(Input.DueTime))
        {
            ModelState.AddModelError(
                "Input.DueDate",
                "Please select a due date.");
        }

        if (dueDate.HasValue &&
            startDate.HasValue &&
            dueDate.Value <= startDate.Value)
        {
            ModelState.AddModelError(
                "Input.DueDate",
                "The due date and time must be after the task start.");
        }

        if (!Input.ReminderEnabled)
        {
            return;
        }

        if (!Input.DueDate.HasValue)
        {
            ModelState.AddModelError(
                "Input.DueDate",
                "A due date is required before enabling reminders.");
        }

        if (!TimeOnly.TryParse(Input.DueTime, out _))
        {
            ModelState.AddModelError(
                "Input.DueTime",
                "A valid due time is required before enabling reminders.");
        }

        if (!startDate.HasValue || !dueDate.HasValue)
        {
            return;
        }

        if (dueDate.Value <= startDate.Value)
        {
            return;
        }

        var allowedUnits = GetAllowedReminderUnits(
            startDate.Value,
            dueDate.Value);

        if (string.IsNullOrWhiteSpace(Input.ReminderUnit) ||
            !allowedUnits.Contains(Input.ReminderUnit))
        {
            ModelState.AddModelError(
                "Input.ReminderUnit",
                "Select a reminder unit allowed for this task's duration.");

            return;
        }

        if (Input.ReminderUnit == "Hour")
        {
            if (!TryParseReminderInterval(
                    Input.ReminderInterval,
                    out TimeSpan interval))
            {
                ModelState.AddModelError(
                    "Input.ReminderInterval",
                    "Enter a valid interval greater than zero in HH:mm:ss format, for example 00:30:00.");
            }
            else if (interval >= dueDate.Value - startDate.Value)
            {
                ModelState.AddModelError(
                    "Input.ReminderInterval",
                    "The reminder interval must be shorter than the task duration.");
            }

            return;
        }

        if (!Input.ReminderValue.HasValue ||
            Input.ReminderValue.Value < 1)
        {
            ModelState.AddModelError(
                "Input.ReminderValue",
                "Enter a reminder interval of at least 1.");
        }

        if (!TimeOnly.TryParse(Input.ReminderTime, out _))
        {
            ModelState.AddModelError(
                "Input.ReminderTime",
                "Please select a reminder time.");
        }
    }

    // =====================================================
    // POST
    // =====================================================

    public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
    {
        await LoadUserSettingsAsync();

        var user = await _userManager.GetUserAsync(User);

        bool confirmingDraft =
            HttpContext.Session.GetString(GuestDraftConfirmationKey)
            == "true";

        if (confirmingDraft)
        {
            if (user == null)
            {
                return RedirectToPage(
                    "/Account/SignIn/SignIn",
                    new { returnUrl = ResumeDraftUrl });
            }

            // Use the server-side session draft as the trusted source.
            var draftJson = HttpContext.Session.GetString(GuestDraftKey);

            if (string.IsNullOrWhiteSpace(draftJson))
            {
                ClearGuestDraft();
                return RedirectToPage();
            }

            AddTaskInput? savedDraft;

            try
            {
                savedDraft = JsonSerializer.Deserialize<AddTaskInput>(
                    draftJson,
                    JsonOptions);
            }
            catch (JsonException)
            {
                savedDraft = null;
            }

            if (savedDraft == null)
            {
                ClearGuestDraft();
                return RedirectToPage();
            }

            Input = savedDraft;
            IsConfirmingGuestDraft = true;
        }

        DateTime now = UserNow;

        ValidateForm(now);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (user == null)
        {
            // Store the valid guest draft in server-side session.
            HttpContext.Session.SetString(
                GuestDraftKey,
                JsonSerializer.Serialize(Input, JsonOptions));

            return RedirectToPage(
                "/Account/SignIn/SignIn",
                new { returnUrl = ResumeDraftUrl });
        }

        DateTime? startDate;

        if (Input.StartImmediately)
        {
            startDate = now;
        }
        else
        {
            startDate = CombineDateAndTime(
                Input.StartDate,
                Input.StartTime);
        }

        DateTime? dueDate = CombineDateAndTime(
            Input.DueDate,
            Input.DueTime);

        int? reminderValue = null;
        string? reminderUnit = null;
        string? reminderTime = null;
        string? reminderInterval = null;

        if (Input.ReminderEnabled)
        {
            reminderUnit = Input.ReminderUnit;

            if (Input.ReminderUnit == "Hour")
            {
                reminderInterval = Input.ReminderInterval!.Trim();
            }
            else
            {
                reminderValue = Input.ReminderValue;

                reminderTime = TimeOnly
                    .Parse(Input.ReminderTime!)
                    .ToString("HH:mm");
            }
        }

        var task = new keeptrack.Models.Task
        {
            Title = Input.Title.Trim(),

            Description = string.IsNullOrWhiteSpace(Input.Description)
                ? string.Empty
                : Input.Description.Trim(),

            IsCompleted = false,

            CreatedAt = DateTime.UtcNow,

            StartImmediately = Input.StartImmediately,
            StartDate = startDate,
            DueDate = dueDate,

            ReminderEnabled = Input.ReminderEnabled,
            ReminderValue = reminderValue,
            ReminderUnit = reminderUnit,
            ReminderTime = reminderTime,
            ReminderInterval = reminderInterval,

            ReminderTiming = "Halfway",

            UserId = user.Id
        };

        _context.Tasks.Add(task);

        await _context.SaveChangesAsync();

        if (confirmingDraft)
        {
            ClearGuestDraft();
        }

        return RedirectToPage("/Tasks/Tasks");
    }

    // =====================================================
    // CLEAR SESSION DRAFT
    // =====================================================

    private void ClearGuestDraft()
    {
        HttpContext.Session.Remove(GuestDraftKey);
        HttpContext.Session.Remove(GuestDraftConfirmationKey);
    }
}
