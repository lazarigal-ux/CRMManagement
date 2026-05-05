using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Opportunities;

public sealed class IndexModel : PageModel
{
    private readonly IOpportunityService _svc;
    public IndexModel(IOpportunityService svc) => _svc = svc;

    public IReadOnlyList<OpportunityListItemDto> Items { get; private set; } = Array.Empty<OpportunityListItemDto>();

    [FromQuery] public Guid? StageId { get; set; }
    [FromQuery] public string? Status { get; set; }
    [FromQuery] public Guid? OwnerUserId { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<OpportunityListItemDto> q = all;
        if (StageId is { } sid) q = q.Where(o => o.StageId == sid);
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(o => string.Equals(o.Status, Status, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
