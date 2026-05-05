using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Web.Pages.Admin.Zoho;

[Authorize(Roles = "Admin,SalesManager")]
public sealed class CallbackModel : PageModel
{
    private const string OAuthStateCookie = "zoho_oauth_state";
    private const string CallbackPath = "/Admin/Zoho/Callback";

    private readonly IZohoConnectionService _connections;
    private readonly IZohoImportService _imports;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(
        IZohoConnectionService connections,
        IZohoImportService imports,
        ILogger<CallbackModel> logger)
    {
        _connections = connections;
        _imports = imports;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? error, CancellationToken ct)
    {
        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;

        if (!string.IsNullOrEmpty(error))
            return RedirectToPage("/Admin/Zoho/Import", new { error });

        var expectedState = Request.Cookies[OAuthStateCookie];
        Response.Cookies.Delete(OAuthStateCookie);

        if (string.IsNullOrEmpty(expectedState) || !string.Equals(expectedState, state, StringComparison.Ordinal))
            return RedirectToPage("/Admin/Zoho/Import", new { error = "Invalid OAuth state. Please try again." });

        if (string.IsNullOrEmpty(code))
            return RedirectToPage("/Admin/Zoho/Import", new { error = "Authorization code missing in callback." });

        var callbackUrl = $"{Request.Scheme}://{Request.Host}{pathBase}{CallbackPath}";

        try
        {
            await _connections.CompleteCallbackAsync(code, callbackUrl, ct);
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Admin/Zoho/Import", new { error = ex.Message });
        }

        // Kick off the first import in the background so data starts loading immediately —
        // the user lands inside Brainium's CRM iframe and can poll /api/zoho-import/status for progress.
        try
        {
            var allModules = new ZohoImportRequest(
                Leads: true, Contacts: true, Accounts: true, Deals: true,
                Products: true, Quotes: true, Activities: true, Campaigns: true,
                Tickets: true, Invoices: true, Orders: true, Notes: true,
                Vendors: true, PurchaseOrders: true, Solutions: true);
            await _imports.StartImportAsync(allModules, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-OAuth import auto-start failed; user can re-trigger via /api/internal/zoho-import/run-and-wait.");
        }

        // In launcher dev-mode the parent Brainium app isn't running, so /General/Crm would 404.
        // Bounce back to our own import-status page instead.
        if (Environment.GetEnvironmentVariable("CRM_DEV_BYPASS_AUTH") == "1")
        {
            return RedirectToPage("/Admin/Zoho/Import");
        }

        // Zoho redirected the *top* window here (it refused to be iframed), so we're now outside
        // Brainium's chrome. Bounce the top window back to /General/Crm so Brainium re-iframes
        // CRMManagement — this time the refresh token exists, so the iframe loads Dashboard cleanly.
        var brainiumHome = $"{Request.Scheme}://{Request.Host}/General/Crm";
        return Redirect(brainiumHome);
    }
}
