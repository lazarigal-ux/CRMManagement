using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Campaigns;

public sealed class DetailsModel : PageModel
{
    private readonly ICampaignService _svc;
    public DetailsModel(ICampaignService svc) => _svc = svc;

    public CampaignDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }
}
