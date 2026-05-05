using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Invoices;

public sealed class IndexModel : PageModel
{
    private readonly IInvoiceService _svc;
    public IndexModel(IInvoiceService svc) => _svc = svc;

    public IReadOnlyList<InvoiceListItemDto> Items { get; private set; } = Array.Empty<InvoiceListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? Status { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<InvoiceListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(i =>
                i.InvoiceNumber.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || (i.Subject?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(Status))
            q = q.Where(i => string.Equals(i.Status, Status, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
