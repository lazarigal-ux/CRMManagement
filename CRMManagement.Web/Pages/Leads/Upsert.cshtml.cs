using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Leads;

public sealed class UpsertModel : PageModel
{
    private readonly ILeadService _svc;
    public UpsertModel(ILeadService svc) => _svc = svc;

    [BindProperty] public LeadUpsertDto Input { get; set; } =
        new(null, "", "", null, null, null, null, null, "New", null, 0, null, null, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new LeadUpsertDto(d.Id, d.FirstName, d.LastName, d.Company, d.Title, d.Email, d.Phone, d.Source, d.Status, d.Rating, d.Score, d.OwnerUserId, d.Industry, d.Website, d.Description);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Leads/Details", new { id });
    }
}
