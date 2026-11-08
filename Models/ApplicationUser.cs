using Microsoft.AspNetCore.Identity;

namespace keeptrack.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;
}