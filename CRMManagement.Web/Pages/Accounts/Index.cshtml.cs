using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Accounts;

public sealed class IndexModel : PageModel
{
    private readonly IAccountService _svc;
    public IndexModel(IAccountService svc) => _svc = svc;

    public IReadOnlyList<AccountListItemDto> Items { get; private set; } = Array.Empty<AccountListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? Industry { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<AccountListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Industry))
            q = q.Where(x => string.Equals(x.Industry, Industry, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
