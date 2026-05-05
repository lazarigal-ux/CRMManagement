using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Tickets;

public sealed class IndexModel : PageModel
{
    private readonly ITicketService _svc;
    public IndexModel(ITicketService svc) => _svc = svc;

    public IReadOnlyList<TicketListItemDto> Items { get; private set; } = Array.Empty<TicketListItemDto>();

    [FromQuery] public string? Status { get; set; }
    [FromQuery] public string? Priority { get; set; }
    [FromQuery] public Guid? OwnerUserId { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<TicketListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(t => string.Equals(t.Status, Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Priority)) q = q.Where(t => string.Equals(t.Priority, Priority, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
