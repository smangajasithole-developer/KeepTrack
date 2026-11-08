using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using keeptrack.Models;

namespace keeptrack.Pages.Home
{
    public class HomeModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult OnGet()
        {
            // Signed-in users belong on the Dashboard, not the Home page.
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Dashboard/Dashboard");
            }

            return Page();
        }
    }
}