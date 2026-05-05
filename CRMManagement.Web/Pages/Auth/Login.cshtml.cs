using CRMManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public LoginModel(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? DevBypassError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // If the user is already authenticated (e.g. via embed token sign-in),
        // skip showing the login form and redirect to the return URL.
        if (User?.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        // Launcher dev-mode: silently sign in the seeded 'admin' user so the standalone
        // sandbox lands on the dashboard without the parent LDataBrain iframe being present.
        if (Environment.GetEnvironmentVariable("CRM_DEV_BYPASS_AUTH") == "1")
        {
            var admin = await _users.FindByNameAsync("admin");
            if (admin is { IsActive: true })
            {
                await _signIn.SignInAsync(admin, isPersistent: false);
                return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
            }

            DevBypassError = "CRM_DEV_BYPASS_AUTH is set but the seeded 'admin' user wasn't found or is inactive. "
                + "Run the app once with the database reachable so DbInitializer seeds it, then retry.";
        }

        // Not authenticated — render the page which sends a postMessage
        // to the parent LDataBrain app asking it to reload with a fresh embed token.
        return Page();
    }
}
