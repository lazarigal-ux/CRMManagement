using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Products;

public sealed class IndexModel : PageModel
{
    private readonly IProductService _svc;
    public IndexModel(IProductService svc) => _svc = svc;

    public IReadOnlyList<ProductListItemDto> Items { get; private set; } = Array.Empty<ProductListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? Family { get; set; }
    [FromQuery] public bool? ActiveOnly { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<ProductListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(p => p.Name.Contains(Search, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Family)) q = q.Where(p => string.Equals(p.Family, Family, StringComparison.OrdinalIgnoreCase));
        if (ActiveOnly == true) q = q.Where(p => p.IsActive);
        Items = q.ToList();
    }
}
