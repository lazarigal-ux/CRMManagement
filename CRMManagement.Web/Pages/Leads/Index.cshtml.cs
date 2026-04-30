using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Leads;

public sealed class IndexModel : PageModel
{
    private readonly ILeadService _svc;
    public IndexModel(ILeadService svc) => _svc = svc;

    public IReadOnlyList<LeadListItemDto> Items { get; private set; } = Array.Empty<LeadListItemDto>();

    [FromQuery] public string? Status { get; set; }
    [FromQuery] public string? Rating { get; set; }
    [FromQuery] public Guid? OwnerUserId { get; set; }
    [FromQuery] public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<LeadListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(x => string.Equals(x.Status, Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Rating)) q = q.Where(x => string.Equals(x.Rating, Rating, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            q = q.Where(x => (x.FirstName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.LastName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.Company ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.Email ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        Items = q.ToList();
    }
}
