using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.ClassMappings;

[Authorize(Roles = "Admin,SalesManager")]
public sealed class IndexModel : PageModel
{
    private readonly IClassProductMappingService _svc;
    private readonly IProductService _products;

    public IndexModel(IClassProductMappingService svc, IProductService products)
    {
        _svc = svc;
        _products = products;
    }

    public IReadOnlyList<ClassProductMappingDto> Mappings { get; private set; } = Array.Empty<ClassProductMappingDto>();
    public IReadOnlyList<ProductListItemDto> Products { get; private set; } = Array.Empty<ProductListItemDto>();

    [BindProperty] public string? Label { get; set; }
    [BindProperty] public Guid ProductId { get; set; }
    [BindProperty] public decimal Multiplier { get; set; } = 1.0m;
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Mappings = await _svc.ListAsync(ct);
        Products = await _products.ListAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Label) || ProductId == Guid.Empty)
        {
            TempData["error"] = "Label and product are required.";
            return RedirectToPage();
        }
        await _svc.UpsertAsync(new ClassProductMappingUpsertDto(
            null, Label!, ProductId, Multiplier, Notes, IsActive), ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return RedirectToPage();
    }
}
