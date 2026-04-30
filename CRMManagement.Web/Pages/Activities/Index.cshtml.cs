using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Activities;

public sealed class IndexModel : PageModel
{
    private readonly IActivityService _svc;
    public IndexModel(IActivityService svc) => _svc = svc;

    public IReadOnlyList<ActivityListItemDto> Items { get; private set; } = Array.Empty<ActivityListItemDto>();

    [FromQuery] public string? Type { get; set; }
    [FromQuery] public string? Status { get; set; }
    [FromQuery] public DateTime? From { get; set; }
    [FromQuery] public DateTime? To { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<ActivityListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Type)) q = q.Where(a => string.Equals(a.Type, Type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(a => string.Equals(a.Status, Status, StringComparison.OrdinalIgnoreCase));
        if (From is { } f) q = q.Where(a => a.DueDate >= f);
        if (To is { } t) q = q.Where(a => a.DueDate <= t);
        Items = q.ToList();
    }
}
