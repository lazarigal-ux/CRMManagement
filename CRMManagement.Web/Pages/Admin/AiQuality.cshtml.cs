using CRMManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Web.Pages.Admin;

[Authorize(Roles = "Admin,SalesManager")]
public sealed class AiQualityModel : PageModel
{
    private readonly AppDbContext _db;
    public AiQualityModel(AppDbContext db) => _db = db;

    public sealed record ModeRow(string Mode, int Total, int Success, int Failed, int Up, int Down, int Perfect, int? AvgMs);
    public sealed record RecentRow(Guid Id, DateTime CreatedAt, string Mode, string Provider, bool Success, short Feedback, string? Instruction, string? ResultText, int? TotalMs);
    public sealed record ProviderRow(string Provider, int Total, int Failed, int? AvgMs);

    public IReadOnlyList<ModeRow> ByMode { get; private set; } = Array.Empty<ModeRow>();
    public IReadOnlyList<ProviderRow> ByProvider { get; private set; } = Array.Empty<ProviderRow>();
    public IReadOnlyList<RecentRow> Recent { get; private set; } = Array.Empty<RecentRow>();
    public int TotalCalls { get; private set; }
    public int SuccessRatePct { get; private set; }
    public int FlaggedCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var logs = await _db.AiInteractionLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= since)
            .ToListAsync(ct);

        TotalCalls = logs.Count;
        SuccessRatePct = TotalCalls == 0 ? 0 : (int)Math.Round(100.0 * logs.Count(l => l.Success) / TotalCalls);
        FlaggedCount = logs.Count(l => l.Feedback < 0);

        ByMode = logs
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Mode) ? "(unset)" : l.Mode)
            .Select(g => new ModeRow(
                g.Key,
                g.Count(),
                g.Count(l => l.Success),
                g.Count(l => !l.Success),
                g.Count(l => l.Feedback == 1),
                g.Count(l => l.Feedback == -1),
                g.Count(l => l.Feedback == 2),
                g.Where(l => l.TotalMs.HasValue).DefaultIfEmpty().Average(l => (int?)(l?.TotalMs ?? 0)) is double avg ? (int)avg : (int?)null))
            .OrderByDescending(r => r.Total)
            .ToList();

        ByProvider = logs
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Provider) ? "(unset)" : l.Provider)
            .Select(g => new ProviderRow(
                g.Key,
                g.Count(),
                g.Count(l => !l.Success),
                g.Where(l => l.TotalMs.HasValue).DefaultIfEmpty().Average(l => (int?)(l?.TotalMs ?? 0)) is double avg ? (int)avg : (int?)null))
            .OrderByDescending(r => r.Total)
            .ToList();

        Recent = logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(40)
            .Select(l => new RecentRow(
                l.Id,
                l.CreatedAt,
                string.IsNullOrWhiteSpace(l.Mode) ? "(unset)" : l.Mode,
                l.Provider,
                l.Success,
                l.Feedback,
                Trim(l.Instruction, 200),
                Trim(l.ResultText, 200),
                l.TotalMs))
            .ToList();
    }

    private static string? Trim(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max] + "…";
}
