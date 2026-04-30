using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Dashboard;

public sealed class IndexModel : PageModel
{
    private readonly IDashboardService _svc;
    public IndexModel(IDashboardService svc) => _svc = svc;

    public DashboardSummaryDto? Summary { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Summary = await _svc.GetSummaryAsync(ct);
    }
}
