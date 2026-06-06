using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Tags;

[Authorize(Roles = "Admin")]
public sealed class UpsertModel : PageModel
{
    private readonly ITagService _svc;
    public UpsertModel(ITagService svc) => _svc = svc;

    [BindProperty] public TagUpsertDto Input { get; set; } = new(null, "", null, "General");

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new TagUpsertDto(d.Id, d.Name, d.Color, d.Scope);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Admin/Tags/Index");
    }
}
