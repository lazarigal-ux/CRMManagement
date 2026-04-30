using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Products;

public sealed class DetailsModel : PageModel
{
    private readonly IProductService _svc;
    public DetailsModel(IProductService svc) => _svc = svc;

    public ProductDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }
}
