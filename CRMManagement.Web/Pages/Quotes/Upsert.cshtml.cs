using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Quotes;

public sealed class UpsertModel : PageModel
{
    private readonly IQuoteService _svc;
    public UpsertModel(IQuoteService svc) => _svc = svc;

    [BindProperty] public QuoteUpsertDto Input { get; set; } =
        new(null, "", "", null, null, null, "Draft", null, 0m, 0m, 0m, 0m, "USD", null, null);

    [BindProperty] public QuoteLineUpsertDto NewLine { get; set; } =
        new(null, null, null, 1m, 0m, 0m, 0);

    public QuoteDetailDto? Existing { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            Existing = await _svc.GetAsync(gid, ct);
            if (Existing is null) return NotFound();
            var d = Existing;
            Input = new QuoteUpsertDto(d.Id, d.QuoteNumber, d.Name, d.AccountId, d.OpportunityId, d.ContactId, d.Status, d.ExpiresAt, d.Subtotal, d.Discount, d.Tax, d.Total, d.Currency, d.Notes, d.OwnerUserId);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddLineAsync(Guid id, CancellationToken ct)
    {
        await _svc.AddQuoteLineAsync(id, NewLine, ct);
        return RedirectToPage(new { id });
    }
}
