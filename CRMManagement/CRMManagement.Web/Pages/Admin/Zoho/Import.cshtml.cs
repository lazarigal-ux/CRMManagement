using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Zoho;

[Authorize(Roles = "Admin,SalesManager")]
public sealed class ImportModel : PageModel
{
    private const string OAuthStateCookie = "zoho_oauth_state";
    private const string CallbackPath = "/Admin/Zoho/Callback";

    private readonly IZohoConnectionService _connections;

    public ImportModel(IZohoConnectionService connections)
    {
        _connections = connections;
    }

    public ZohoConnectionDto? Connection { get; private set; }
    public string CallbackUrl { get; private set; } = "";
    public string? ConfigError { get; private set; }
    public string? AuthorizeUrl { get; private set; }

    [BindProperty(SupportsGet = true)] public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Connection = await _connections.GetAsync(ct);
        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
        CallbackUrl = $"{Request.Scheme}://{Request.Host}{pathBase}{CallbackPath}";

        // Already connected? Bounce to Home — first-run plumbing is done.
        if (Connection is not null && Connection.Status == "Connected" && Connection.HasRefreshToken)
        {
            return Redirect(pathBase + "/Home");
        }

        // Not configured at all (operator hasn't set Zoho:ClientId in config) — render a notice.
        if (Connection is null || !Connection.HasClientSecret || string.IsNullOrWhiteSpace(Connection.ClientId))
        {
            ConfigError = "Zoho is not configured. Set Zoho:ClientId and Zoho:ClientSecret in appsettings (or environment variables) and restart.";
            return Page();
        }

        // If the user came back from Callback with an error, show it instead of looping back into Zoho.
        if (!string.IsNullOrEmpty(Error))
        {
            return Page();
        }

        // Configured but no refresh token yet — build the Zoho auth URL.
        // We render a page (instead of a 302) so JavaScript can break out of any embedding
        // iframe (Brainium /General/Crm) and navigate the TOP window to accounts.zoho.com,
        // which refuses to be iframed (X-Frame-Options).
        var state = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(OAuthStateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
        });

        AuthorizeUrl = await _connections.BuildAuthorizeUrlAsync(CallbackUrl, state, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync(CancellationToken ct)
    {
        await _connections.DisconnectAsync(ct);
        return RedirectToPage();
    }
}
