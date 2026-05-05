using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Tickets;

public sealed class DetailsModel : PageModel
{
    private readonly ITicketService _svc;
    public DetailsModel(ITicketService svc) => _svc = svc;

    public TicketDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id, CancellationToken ct)
    {
        await _svc.CloseTicketAsync(id, ct);
        return RedirectToPage(new { id });
    }
}
