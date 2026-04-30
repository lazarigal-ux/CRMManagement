using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        // If the user is already authenticated (e.g. via embed token sign-in),
        // skip showing the login form and redirect to the return URL.
        if (User?.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        // Not authenticated — render the page which sends a postMessage
        // to the parent LDataBrain app asking it to reload with a fresh embed token.
        return Page();
    }
}
