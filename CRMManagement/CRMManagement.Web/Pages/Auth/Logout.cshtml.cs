using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Home/Index");
    }

    public IActionResult OnGet()
    {
        // Prefer POST, but support GET for simplicity.
        return Page();
    }
}
