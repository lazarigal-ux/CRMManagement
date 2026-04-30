using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Reports;

public sealed class IndexModel : PageModel
{
    private readonly IDashboardService _dash;
    private readonly IOpportunityService _opps;
    private readonly IPipelineService _pipelines;
    public IndexModel(IDashboardService dash, IOpportunityService opps, IPipelineService pipelines)
    { _dash = dash; _opps = opps; _pipelines = pipelines; }

    public DashboardSummaryDto? Summary { get; private set; }
    public IReadOnlyList<StageTotal> StageTotals { get; private set; } = Array.Empty<StageTotal>();

    public sealed record StageTotal(string StageName, int Count, decimal Total);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Summary = await _dash.GetSummaryAsync(ct);
        var pipelines = await _pipelines.ListAsync(ct);
        var stageMap = pipelines.SelectMany(p => p.Stages).ToDictionary(s => s.Id, s => s.Name);
        var opps = await _opps.ListAsync(ct);
        StageTotals = opps
            .GroupBy(o => o.StageId)
            .Select(g => new StageTotal(
                stageMap.TryGetValue(g.Key, out var n) ? n : g.Key.ToString(),
                g.Count(),
                g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();
    }
}
