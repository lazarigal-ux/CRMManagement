using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Quotes;

public sealed class DetailsModel : PageModel
{
    private readonly IQuoteService _svc;
    public DetailsModel(IQuoteService svc) => _svc = svc;

    public QuoteDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }
}
