using CRMManagement.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IZohoConnectionService _zoho;

    public IndexModel(IZohoConnectionService zoho) => _zoho = zoho;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        // Preserve iframe/standalone query flags when redirecting.
        // Use PathBase so /crm is retained when running behind nginx reverse proxy.
        var qs = Request?.QueryString.HasValue == true ? Request.QueryString.Value : string.Empty;
        var pb = Request?.PathBase.HasValue == true ? Request.PathBase.Value : string.Empty;

        // If no company cookie is set, redirect to company selection page.
        var rawCompany = Request?.Cookies["crm_company"];
        if (string.IsNullOrWhiteSpace(rawCompany) || !int.TryParse(rawCompany, out var cid) || cid <= 0)
            return Redirect(pb + "/CompanySelect" + qs);

        // First-run: send admins/salesmen to the Zoho connection page until they've authorized.
        if (User.IsInRole("Admin") || User.IsInRole("SalesManager"))
        {
            var conn = await _zoho.GetAsync(ct);
            if (conn is null || conn.Status != "Connected" || !conn.HasRefreshToken)
                return Redirect(pb + "/Admin/Zoho/Import" + qs);
        }

        return Redirect(pb + "/Home" + qs);
    }
}
