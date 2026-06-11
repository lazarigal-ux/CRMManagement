using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using CRMManagement.Infrastructure.Identity;
using CRMManagement.Web.Configuration.Auth;

namespace CRMManagement.Web.Pages.Auth;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OidcAuthOptions _oidc;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<OidcAuthOptions> oidc)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _oidc = oidc.Value;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Home/Index");
    }

    public IActionResult OnPostChallenge(string? returnUrl = null)
    {
        var authorityOk = Uri.TryCreate(_oidc.Authority, UriKind.Absolute, out var authorityUri)
                          && (string.Equals(authorityUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

        if (!(_oidc.Enabled && authorityOk && !string.IsNullOrWhiteSpace(_oidc.ClientId)))
        {
            return RedirectToPage("/Home/Index");
        }

        returnUrl ??= Url.Content("~/");

        var redirectUrl = Url.Page("/Auth/ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("oidc", redirectUrl);
        return new ChallengeResult("oidc", properties);
    }

    public async Task<IActionResult> OnGetCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            return RedirectToPage("/Home/Index", new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("/Home/Index", new { returnUrl });
        }

        // Existing external login mapping
        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        // Auto-provision a local user and link the external login.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var username = info.Principal.FindFirstValue("preferred_username")
                       ?? info.Principal.FindFirstValue(ClaimTypes.Name)
                       ?? email
                       ?? $"user_{Guid.NewGuid():N}";

        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            username = $"user_{Guid.NewGuid():N}";
        }

        // Try existing user by email first.
        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(email),
                IsActive = true
            };

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                return RedirectToPage("/Home/Index", new { returnUrl, error = $"Failed to create account: {errors}" });
            }

            try
            {
                // Default role (best-effort; roles are seeded at startup).
                await _userManager.AddToRoleAsync(user, "User");
            }
            catch
            {
                // no-op
            }
        }

        if (!user.IsActive)
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Home/Index", new { returnUrl });
        }

        var addLogin = await _userManager.AddLoginAsync(user, info);
        if (!addLogin.Succeeded)
        {
            return RedirectToPage("/Home/Index", new { returnUrl });
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        await _signInManager.UpdateExternalAuthenticationTokensAsync(info);

        return LocalRedirect(returnUrl);
    }
}
