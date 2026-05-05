using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Web.Pages.Pipelines;

[Authorize]
public sealed class ForecastModel : PageModel
{
    private readonly AppDbContext _db;
    public ForecastModel(AppDbContext db) => _db = db;

    public sealed record MonthlyForecastRow(int Year, int Month, int DealCount, decimal Total, decimal Weighted);
    public sealed record StageForecastRow(string PipelineName, string StageName, int Probability, int DealCount, decimal Total, decimal Weighted);
    public sealed record StaleDealRow(Guid OpportunityId, string Name, string Stage, decimal Amount, string Currency, int DaysInStage, int? SlaHours, string? Owner);

    public IReadOnlyList<MonthlyForecastRow> ByMonth { get; private set; } = Array.Empty<MonthlyForecastRow>();
    public IReadOnlyList<StageForecastRow> ByStage { get; private set; } = Array.Empty<StageForecastRow>();
    public IReadOnlyList<StaleDealRow> StaleDeals { get; private set; } = Array.Empty<StaleDealRow>();

    public decimal TotalOpenWeighted { get; private set; }
    public decimal TotalOpenAmount { get; private set; }
    public int TotalOpenCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var openDeals = await _db.Opportunities
            .AsNoTracking()
            .Include(o => o.Stage)
            .Include(o => o.Pipeline)
            .Where(o => o.Status == "Open")
            .ToListAsync(ct);

        TotalOpenCount = openDeals.Count;
        TotalOpenAmount = openDeals.Sum(o => o.Amount);
        TotalOpenWeighted = openDeals.Sum(o => o.Amount * (decimal)((o.Stage?.Probability ?? o.Probability) / 100.0));

        ByMonth = openDeals
            .Where(o => o.CloseDate.HasValue)
            .GroupBy(o => new { Year = o.CloseDate!.Value.Year, Month = o.CloseDate!.Value.Month })
            .Select(g => new MonthlyForecastRow(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Sum(o => o.Amount),
                g.Sum(o => o.Amount * (decimal)((o.Stage?.Probability ?? o.Probability) / 100.0))))
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ToList();

        ByStage = openDeals
            .GroupBy(o => new { Pipeline = o.Pipeline?.Name ?? "(no pipeline)", Stage = o.Stage?.Name ?? "(no stage)", Prob = o.Stage?.Probability ?? o.Probability })
            .Select(g => new StageForecastRow(
                g.Key.Pipeline, g.Key.Stage, g.Key.Prob,
                g.Count(),
                g.Sum(o => o.Amount),
                g.Sum(o => o.Amount * (decimal)(g.Key.Prob / 100.0))))
            .OrderByDescending(r => r.Weighted)
            .ToList();

        StaleDeals = openDeals
            .Select(o =>
            {
                var entered = o.StageEnteredAt ?? o.CreatedAt;
                var hours = (int)(now - entered).TotalHours;
                var sla = o.Stage?.SlaHours;
                return new
                {
                    Opp = o,
                    Hours = hours,
                    Sla = sla,
                    IsStale = sla.HasValue ? hours > sla.Value : hours > 24 * 14,
                };
            })
            .Where(x => x.IsStale)
            .OrderByDescending(x => x.Hours)
            .Take(50)
            .Select(x => new StaleDealRow(
                x.Opp.Id,
                x.Opp.Name,
                x.Opp.Stage?.Name ?? "(no stage)",
                x.Opp.Amount,
                x.Opp.Currency,
                x.Hours / 24,
                x.Sla,
                null))
            .ToList();
    }
}
