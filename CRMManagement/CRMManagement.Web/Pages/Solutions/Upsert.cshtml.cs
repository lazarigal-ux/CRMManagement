using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Solutions;

public sealed class UpsertModel : PageModel
{
    private readonly ISolutionService _svc;
    public UpsertModel(ISolutionService svc) => _svc = svc;

    [BindProperty] public SolutionUpsertDto Input { get; set; } =
        new(null, "", "", null, null, null, "Draft", null, false, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new SolutionUpsertDto(d.Id, d.SolutionNumber, d.Title, d.Question, d.Answer, d.Category,
                d.Status, d.ProductId, d.Published, d.Comments, d.OwnerUserId);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input.SolutionNumber))
            ModelState.AddModelError("Input.SolutionNumber", "Solution Number is required.");
        if (string.IsNullOrWhiteSpace(Input.Title))
            ModelState.AddModelError("Input.Title", "Title is required.");
        if (!ModelState.IsValid) return Page();

        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Solutions/Details", new { id });
    }
}
