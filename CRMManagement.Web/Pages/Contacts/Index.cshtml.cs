using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Contacts;

public sealed class IndexModel : PageModel
{
    private readonly IContactService _svc;
    public IndexModel(IContactService svc) => _svc = svc;

    public IReadOnlyList<ContactListItemDto> Items { get; private set; } = Array.Empty<ContactListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public Guid? AccountId { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<ContactListItemDto> q = all;
        if (AccountId is { } aid) q = q.Where(c => c.AccountId == aid);
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(c => (c.FirstName + " " + c.LastName).Contains(Search, StringComparison.OrdinalIgnoreCase)
                          || (c.Email ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
