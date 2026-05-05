using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Activities;

public sealed class DetailsModel : PageModel
{
    private readonly IActivityService _svc;
    public DetailsModel(IActivityService svc) => _svc = svc;

    public ActivityDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken ct)
    {
        await _svc.CompleteAsync(id, ct);
        return RedirectToPage(new { id });
    }
}
