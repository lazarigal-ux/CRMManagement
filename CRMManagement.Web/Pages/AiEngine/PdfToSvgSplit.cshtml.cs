using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.AiEngine;

public sealed class PdfToSvgSplitModel : PageModel
{
    public string? ErrorMessage { get; set; }

    public void OnGet() { }
}
