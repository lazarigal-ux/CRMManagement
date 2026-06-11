using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Opportunities;

public sealed class KanbanModel : PageModel
{
    private readonly IOpportunityService _svc;
    private readonly IPipelineService _pipelines;
    public KanbanModel(IOpportunityService svc, IPipelineService pipelines)
    { _svc = svc; _pipelines = pipelines; }

    public IReadOnlyList<PipelineDto> Pipelines { get; private set; } = Array.Empty<PipelineDto>();
    public IReadOnlyList<OpportunityListItemDto> Items { get; private set; } = Array.Empty<OpportunityListItemDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Pipelines = await _pipelines.ListAsync(ct);
        Items = await _svc.ListAsync(ct);
    }

    public async Task<IActionResult> OnPostAdvanceStageAsync(Guid id, Guid stageId, CancellationToken ct)
    {
        await _svc.AdvanceStageAsync(id, stageId, ct);
        return RedirectToPage();
    }
}
