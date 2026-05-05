using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Portal.Quotes;

[AllowAnonymous]
public sealed class IndexModel : PageModel
{
    private readonly IQuotePortalService _portal;
    public IndexModel(IQuotePortalService portal) => _portal = portal;

    public PublicQuoteDto? Quote { get; private set; }
    public bool TokenInvalid { get; private set; }
    public Guid Token { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? token, CancellationToken ct)
    {
        if (token is null || token == Guid.Empty) { TokenInvalid = true; return Page(); }
        Token = token.Value;
        Quote = await _portal.GetByTokenAsync(token.Value, ct);
        if (Quote is null) { TokenInvalid = true; }
        return Page();
    }
}
