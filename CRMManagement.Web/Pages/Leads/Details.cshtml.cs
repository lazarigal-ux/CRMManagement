using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Leads;

public sealed class DetailsModel : PageModel
{
    private readonly ILeadService _svc;
    public DetailsModel(ILeadService svc) => _svc = svc;

    public LeadDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid id, CancellationToken ct)
    {
        await _svc.ConvertLeadAsync(id, ct);
        return RedirectToPage(new { id });
    }
}
