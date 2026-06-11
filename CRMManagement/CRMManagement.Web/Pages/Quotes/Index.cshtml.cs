using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Quotes;

public sealed class IndexModel : PageModel
{
    private readonly IQuoteService _svc;
    public IndexModel(IQuoteService svc) => _svc = svc;

    public IReadOnlyList<QuoteListItemDto> Items { get; private set; } = Array.Empty<QuoteListItemDto>();

    [FromQuery] public string? Status { get; set; }
    [FromQuery] public Guid? AccountId { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<QuoteListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(x => string.Equals(x.Status, Status, StringComparison.OrdinalIgnoreCase));
        if (AccountId is { } aid) q = q.Where(x => x.AccountId == aid);
        Items = q.ToList();
    }
}
