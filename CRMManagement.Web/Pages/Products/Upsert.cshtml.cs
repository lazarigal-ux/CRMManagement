using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Products;

public sealed class UpsertModel : PageModel
{
    private readonly IProductService _svc;
    public UpsertModel(IProductService svc) => _svc = svc;

    [BindProperty] public ProductUpsertDto Input { get; set; } =
        new(null, "", "", null, null, true, 0m, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new ProductUpsertDto(d.Id, d.Sku, d.Name, d.Description, d.Family, d.IsActive, d.StandardPrice, d.Cost, d.Unit);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Products/Details", new { id });
    }
}
