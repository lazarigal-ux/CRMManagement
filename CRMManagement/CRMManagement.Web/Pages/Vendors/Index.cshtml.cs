using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Vendors;

public sealed class IndexModel : PageModel
{
    private readonly IVendorService _svc;
    public IndexModel(IVendorService svc) => _svc = svc;

    public IReadOnlyList<VendorListItemDto> Items { get; private set; } = Array.Empty<VendorListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public bool? ActiveOnly { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<VendorListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(v =>
                v.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || (v.Email?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (v.Category?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        if (ActiveOnly == true) q = q.Where(v => v.IsActive);
        Items = q.ToList();
    }
}
