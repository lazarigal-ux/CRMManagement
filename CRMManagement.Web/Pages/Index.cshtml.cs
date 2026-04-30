using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Preserve iframe/standalone query flags when redirecting.
        // Use PathBase so /crm is retained when running behind nginx reverse proxy.
        var qs = Request?.QueryString.HasValue == true ? Request.QueryString.Value : string.Empty;
        var pb = Request?.PathBase.HasValue == true ? Request.PathBase.Value : string.Empty;

        // If no company cookie is set, redirect to company selection page.
        var rawCompany = Request?.Cookies["crm_company"];
        if (string.IsNullOrWhiteSpace(rawCompany) || !int.TryParse(rawCompany, out var cid) || cid <= 0)
            return Redirect(pb + "/CompanySelect" + qs);

        return Redirect(pb + "/Dashboard" + qs);
    }
}
