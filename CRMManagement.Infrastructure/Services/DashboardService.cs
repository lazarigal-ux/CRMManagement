using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) { _db = db; }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var openLeads = await _db.Leads.AsNoTracking().CountAsync(l => l.Status != "Converted" && l.Status != "Unqualified", ct);
        var openOpps = await _db.Opportunities.AsNoTracking().CountAsync(o => o.Status == "Open", ct);
        var wonThisMonth = await _db.Opportunities.AsNoTracking().CountAsync(o => o.Status == "Won" && o.UpdatedAt >= monthStart, ct);
        var openTickets = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status != "Closed" && t.Status != "Resolved", ct);
        var totalPipeline = await _db.Opportunities.AsNoTracking().Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;
        var wonAmount = await _db.Opportunities.AsNoTracking().Where(o => o.Status == "Won" && o.UpdatedAt >= monthStart).SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;

        var top = await _db.Opportunities.AsNoTracking()
            .Where(o => o.Status == "Open")
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .Select(o => new OpportunityListItemDto(o.Id, o.Name, o.AccountId, o.Amount, o.Currency, o.StageId, o.Status, o.CloseDate))
            .ToListAsync(ct);

        var recent = await _db.Activities.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new ActivityListItemDto(a.Id, a.Type, a.Subject, a.Status, a.DueDate, a.OwnerUserId, a.RelatedType, a.RelatedId))
            .ToListAsync(ct);

        return new DashboardSummaryDto(openLeads, openOpps, wonThisMonth, openTickets, totalPipeline, wonAmount, top, recent);
    }
}
