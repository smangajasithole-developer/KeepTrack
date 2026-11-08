
namespace keeptrack.Models;

public class UserSettings
{
    public int Id { get; set; }


    // =====================================================
    // USER
    // =====================================================

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;


    // =====================================================
    // GENERAL
    // =====================================================

    public string Language { get; set; } = "English";

    public string TimeZone { get; set; } = "Africa/Johannesburg";

    public string DateFormat { get; set; } = "yyyy/MM/dd";

    public string StartPage { get; set; } = "Dashboard";


    // =====================================================
    // TASK PREFERENCES
    // =====================================================

    public bool DefaultStartImmediately { get; set; } = true;

    public bool DefaultReminderEnabled { get; set; }

    public int? DefaultReminderValue { get; set; }

    public string? DefaultReminderUnit { get; set; }


    // =====================================================
    // PERSONALISATION
    // =====================================================

    public string Appearance { get; set; } = "System Default";

    public string AccentColour { get; set; } = "Default";

    public string TaskLayout { get; set; } = "Comfortable";
}
