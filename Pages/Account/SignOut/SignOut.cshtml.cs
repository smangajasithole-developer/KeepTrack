using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace keeptrack.Pages.Account.SignOut;

public class SignOutModel : PageModel
{
    private readonly SignInManager<keeptrack.Models.ApplicationUser>
        _signInManager;


    public SignOutModel(
        SignInManager<keeptrack.Models.ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }


    public void OnGet()
    {
    }


    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();

        return RedirectToPage("/Home/Home");
    }
}