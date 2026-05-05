using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Tickets;

public sealed class UpsertModel : PageModel
{
    private readonly ITicketService _svc;
    public UpsertModel(ITicketService svc) => _svc = svc;

    [BindProperty] public TicketUpsertDto Input { get; set; } =
        new(null, "", "", null, null, null, "Open", "Medium", "Issue", "Web", null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new TicketUpsertDto(d.Id, d.TicketNumber, d.Subject, d.Description, d.AccountId, d.ContactId, d.Status, d.Priority, d.Type, d.Channel, d.ReportedBy, d.OwnerUserId);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Tickets/Details", new { id });
    }
}
