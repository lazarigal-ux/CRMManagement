using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.PurchaseOrders;

public sealed class IndexModel : PageModel
{
    private readonly IPurchaseOrderService _svc;
    public IndexModel(IPurchaseOrderService svc) => _svc = svc;

    public IReadOnlyList<PurchaseOrderListItemDto> Items { get; private set; } = Array.Empty<PurchaseOrderListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? Status { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<PurchaseOrderListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(p =>
                p.PoNumber.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || p.Subject.Contains(Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Status))
            q = q.Where(p => string.Equals(p.Status, Status, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
