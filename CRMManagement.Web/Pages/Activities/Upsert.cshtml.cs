using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Activities;

public sealed class UpsertModel : PageModel
{
    private readonly IActivityService _svc;
    public UpsertModel(IActivityService svc) => _svc = svc;

    [BindProperty] public ActivityUpsertDto Input { get; set; } =
        new(null, "Task", "", null, null, null, null, "Open", null, null, null, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, string? relatedType, Guid? relatedId, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new ActivityUpsertDto(d.Id, d.Type, d.Subject, d.Description, d.StartAt, d.EndAt, d.DueDate, d.Status, d.Priority, d.OwnerUserId, d.RelatedType, d.RelatedId, d.Location);
        }
        else if (!string.IsNullOrEmpty(relatedType) || relatedId is not null)
        {
            Input = Input with { RelatedType = relatedType, RelatedId = relatedId };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Activities/Details", new { id });
    }
}
