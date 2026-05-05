using CRMManagement.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin;

[Authorize(Roles = "Admin,SalesManager")]
public sealed class OutboxModel : PageModel
{
    private readonly IIntegrationOutboxService _svc;
    public OutboxModel(IIntegrationOutboxService svc) => _svc = svc;

    public IReadOnlyList<OutboxRow> Rows { get; private set; } = Array.Empty<OutboxRow>();
    public string? StatusFilter { get; private set; }

    public async Task OnGetAsync(string? status, CancellationToken ct)
    {
        StatusFilter = string.IsNullOrWhiteSpace(status) ? null : status;
        Rows = await _svc.ListAsync(200, StatusFilter, ct);
    }

    public async Task<IActionResult> OnPostRetryAsync(Guid id, CancellationToken ct)
    {
        await _svc.RetryAsync(id, ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRetryAllAsync(CancellationToken ct)
    {
        await _svc.RetryAllFailedAsync(ct);
        return RedirectToPage();
    }
}
