using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Campaigns;

public sealed class IndexModel : PageModel
{
    private readonly ICampaignService _svc;
    public IndexModel(ICampaignService svc) => _svc = svc;

    public IReadOnlyList<CampaignListItemDto> Items { get; private set; } = Array.Empty<CampaignListItemDto>();

    [FromQuery] public string? Type { get; set; }
    [FromQuery] public string? Status { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<CampaignListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Type)) q = q.Where(c => string.Equals(c.Type, Type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(c => string.Equals(c.Status, Status, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
