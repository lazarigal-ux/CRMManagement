using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Opportunities;

public sealed class UpsertModel : PageModel
{
    private readonly IOpportunityService _svc;
    public UpsertModel(IOpportunityService svc) => _svc = svc;

    [BindProperty] public OpportunityUpsertDto Input { get; set; } =
        new(null, "", null, null, Guid.Empty, Guid.Empty, 0m, "USD", null, 0, "Open", null, null, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new OpportunityUpsertDto(d.Id, d.Name, d.AccountId, d.ContactId, d.PipelineId, d.StageId, d.Amount, d.Currency, d.CloseDate, d.Probability, d.Status, d.LeadSource, d.OwnerUserId, d.Description, d.NextStep);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Opportunities/Details", new { id });
    }
}
