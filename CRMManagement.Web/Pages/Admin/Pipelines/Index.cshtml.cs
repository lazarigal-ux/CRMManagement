using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Pipelines;

public sealed class IndexModel : PageModel
{
    private readonly IPipelineService _svc;
    public IndexModel(IPipelineService svc) => _svc = svc;

    public IReadOnlyList<PipelineDto> Items { get; private set; } = Array.Empty<PipelineDto>();

    public async Task OnGetAsync(CancellationToken ct) => Items = await _svc.ListAsync(ct);
}
