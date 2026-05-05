using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Solutions;

public sealed class DetailsModel : PageModel
{
    private readonly ISolutionService _svc;
    public DetailsModel(ISolutionService svc) => _svc = svc;

    public SolutionDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        return Item is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return RedirectToPage("/Solutions/Index");
    }
}
