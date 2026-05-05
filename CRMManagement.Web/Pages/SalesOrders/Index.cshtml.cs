using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.SalesOrders;

public sealed class IndexModel : PageModel
{
    private readonly IOrderService _svc;
    public IndexModel(IOrderService svc) => _svc = svc;

    public IReadOnlyList<OrderListItemDto> Items { get; private set; } = Array.Empty<OrderListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? Status { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<OrderListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(o =>
                o.OrderNumber.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || (o.Subject?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(Status))
            q = q.Where(o => string.Equals(o.Status, Status, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
