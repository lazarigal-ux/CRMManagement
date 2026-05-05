using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Invoices;

public sealed class DetailsModel : PageModel
{
    private readonly IInvoiceService _svc;
    public DetailsModel(IInvoiceService svc) => _svc = svc;

    public InvoiceDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        return Item is null ? NotFound() : Page();
    }
}
