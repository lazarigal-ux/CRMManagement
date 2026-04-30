using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Campaigns;

public sealed class UpsertModel : PageModel
{
    private readonly ICampaignService _svc;
    public UpsertModel(ICampaignService svc) => _svc = svc;

    [BindProperty] public CampaignUpsertDto Input { get; set; } =
        new(null, "", "Email", "Planned", null, null, null, null, null, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new CampaignUpsertDto(d.Id, d.Name, d.Type, d.Status, d.StartDate, d.EndDate, d.BudgetedCost, d.ActualCost, d.ExpectedRevenue, d.Description, d.OwnerUserId);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Campaigns/Details", new { id });
    }
}
