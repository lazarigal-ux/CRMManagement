using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Opportunities;

public sealed class DetailsModel : PageModel
{
    private readonly IOpportunityService _svc;
    public DetailsModel(IOpportunityService svc) => _svc = svc;

    public OpportunityDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAdvanceStageAsync(Guid id, Guid stageId, CancellationToken ct)
    {
        await _svc.AdvanceStageAsync(id, stageId, ct);
        return RedirectToPage(new { id });
    }
}
