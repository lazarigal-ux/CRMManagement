using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.SalesOrders;

public sealed class DetailsModel : PageModel
{
    private readonly IOrderService _svc;
    public DetailsModel(IOrderService svc) => _svc = svc;

    public OrderDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        return Item is null ? NotFound() : Page();
    }
}
