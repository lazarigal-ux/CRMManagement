using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    // "Untouched" matches Zoho's default — open deals with no activity in the last 7 days.
    private const int UntouchedDaysThreshold = 7;

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(AppDbContext db, IConfiguration config, ICurrentUserService currentUser)
    {
        _db = db;
        _config = config;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(ClaimsPrincipal? principal, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = monthStart.AddMonths(-1);
        var nextMonthStart = monthStart.AddMonths(1);
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);
        var untouchedCutoff = now.AddDays(-UntouchedDaysThreshold);

        Guid? meId = null;
        try { meId = await _currentUser.GetCurrentUserIdAsync(principal, ct); }
        catch { /* dashboard works for unauthenticated viewers — My-* sections just stay empty */ }

        var openLeads = await _db.Leads.AsNoTracking().CountAsync(l => l.Status != "Converted" && l.Status != "Unqualified", ct);
        var openLeadsPrev = await _db.Leads.AsNoTracking()
            .CountAsync(l => l.CreatedAt < monthStart && l.Status != "Converted" && l.Status != "Unqualified", ct);

        var openOpps = await _db.Opportunities.AsNoTracking().CountAsync(o => o.Status == "Open", ct);
        var wonThisMonth = await _db.Opportunities.AsNoTracking().CountAsync(o => o.Status == "Won" && o.UpdatedAt >= monthStart, ct);
        var wonPrevMonth = await _db.Opportunities.AsNoTracking()
            .CountAsync(o => o.Status == "Won" && o.UpdatedAt >= prevMonthStart && o.UpdatedAt < monthStart, ct);

        var openTickets = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status != "Closed" && t.Status != "Resolved", ct);
        var totalPipeline = await _db.Opportunities.AsNoTracking().Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;
        var wonAmount = await _db.Opportunities.AsNoTracking().Where(o => o.Status == "Won" && o.UpdatedAt >= monthStart).SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;
        var wonAmountPrev = await _db.Opportunities.AsNoTracking()
            .Where(o => o.Status == "Won" && o.UpdatedAt >= prevMonthStart && o.UpdatedAt < monthStart)
            .SumAsync(o => (decimal?)o.Amount, ct) ?? 0m;

        var top = await _db.Opportunities.AsNoTracking()
            .Where(o => o.Status == "Open")
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .Select(o => new OpportunityListItemDto(o.Id, o.Name, o.AccountId, o.Amount, o.Currency, o.StageId, o.Status, o.CloseDate))
            .ToListAsync(ct);

        var closingThisMonth = await _db.Opportunities.AsNoTracking()
            .Where(o => o.Status == "Open" && o.CloseDate >= monthStart && o.CloseDate < nextMonthStart)
            .OrderBy(o => o.CloseDate)
            .Take(10)
            .Select(o => new OpportunityListItemDto(o.Id, o.Name, o.AccountId, o.Amount, o.Currency, o.StageId, o.Status, o.CloseDate)
            {
                Probability      = o.Probability,
                LeadSource       = o.LeadSource,
                Description      = o.Description,
                NextStep         = o.NextStep,
                Type             = o.Type,
                ZohoCreatedTime  = o.ZohoCreatedTime,
                ZohoModifiedTime = o.ZohoModifiedTime,
                StageName        = _db.PipelineStages.Where(s => s.Id == o.StageId).Select(s => s.Name).FirstOrDefault(),
                AccountName      = o.AccountId == null
                    ? null
                    : _db.Accounts.Where(a => a.Id == o.AccountId).Select(a => a.Name).FirstOrDefault(),
                OwnerName        = o.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == o.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var recent = await _db.Activities.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new ActivityListItemDto(a.Id, a.Type, a.Subject, a.Status, a.DueDate, a.OwnerUserId, a.RelatedType, a.RelatedId)
            {
                Priority            = a.Priority,
                Description         = a.Description,
                StartAt             = a.StartAt,
                EndAt               = a.EndAt,
                Location            = a.Location,
                CompletedAt         = a.CompletedAt,
                ActivityType        = a.ActivityType,
                CallType            = a.CallType,
                CallDurationSeconds = a.CallDurationSeconds,
                EventTitle          = a.EventTitle,
                ZohoCreatedTime     = a.ZohoCreatedTime,
                ZohoModifiedTime    = a.ZohoModifiedTime,
                OwnerName           = a.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == a.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var revenueTarget = _config.GetValue<decimal?>("Dashboard:MonthlyRevenueTarget") ?? 0m;

        // ── "My ___" panels (mirror Zoho's home dashboard) ────────────────────────────
        // The Zoho importer matches owner by email; if the Zoho org's user emails don't
        // map to local users, OwnerUserId is null on every imported row. We treat
        // null-owner rows as "mine" so a single-user mirror install isn't blank.
        var meId2 = meId;

        // Optional probability cap: Zoho home dashboard widgets often filter "My Open Deals"
        // by probability (e.g. < 75%) so high-probability "almost closed" deals appear in a
        // separate widget. If the user configures Dashboard:OpenDealMaxProbability, apply it
        // here so the local count matches Zoho's home widget. Default is no cap.
        var openDealMaxProbability = _config.GetValue<int?>("Dashboard:OpenDealMaxProbability");
        var myOpenDeals = await _db.Opportunities.AsNoTracking()
            .CountAsync(o => o.Status == "Open"
                             && (o.OwnerUserId == meId2 || o.OwnerUserId == null)
                             && (openDealMaxProbability == null || o.Probability < openDealMaxProbability), ct);

        // "Untouched" mirrors Zoho's Last_Activity_Time IS NULL semantics: open deals with no
        // recent activity touching them. We can't trust o.UpdatedAt (EF stamps it on every
        // import save), so we look at related activities instead.
        var myUntouchedDeals = await _db.Opportunities.AsNoTracking()
            .CountAsync(o => o.Status == "Open"
                             && (o.OwnerUserId == meId2 || o.OwnerUserId == null)
                             && !_db.Activities.Any(a => a.RelatedType == "Opportunity"
                                                      && a.RelatedId == o.Id
                                                      && a.CreatedAt >= untouchedCutoff), ct);

        // "My Calls Today" mirrors Zoho's home dashboard semantics: open calls scheduled for
        // today PLUS any overdue uncompleted calls (so a missed call from yesterday still
        // shows as something you owe today). Calls with no DueDate count too — they're open
        // todos. Closed/Completed/Cancelled calls are excluded.
        var myCallsToday = await _db.Activities.AsNoTracking()
            .CountAsync(a => a.Type == "Call"
                             && (a.OwnerUserId == meId2 || a.OwnerUserId == null)
                             && a.Status != "Completed" && a.Status != "Closed" && a.Status != "Cancelled"
                             && (a.DueDate == null || a.DueDate < tomorrowStart), ct);

        var myLeadsCount = await _db.Leads.AsNoTracking()
            .CountAsync(l => (l.OwnerUserId == meId2 || l.OwnerUserId == null)
                             && l.Status != "Converted" && l.Status != "Unqualified", ct);

        var myOpenTasks = await _db.Activities.AsNoTracking()
            .Where(a => a.Type == "Task"
                        && (a.OwnerUserId == meId2 || a.OwnerUserId == null)
                        && a.Status != "Completed" && a.Status != "Closed")
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new
            {
                a.Id, a.Subject, a.DueDate, a.Status, a.Priority, a.RelatedType, a.RelatedId,
                a.Description, a.StartAt, a.EndAt, a.CompletedAt, a.Location, a.ActivityType,
                a.ZohoCreatedTime, a.ZohoModifiedTime, a.OwnerUserId,
                AccountName = a.RelatedType == "Account"
                    ? _db.Accounts.Where(x => x.Id == a.RelatedId).Select(x => x.Name).FirstOrDefault()
                    : null,
                OwnerName = a.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == a.OwnerUserId).Select(u => u.UserName).FirstOrDefault()
            })
            .ToListAsync(ct);
        var myOpenTaskDtos = myOpenTasks
            .Select(a => new DashboardTaskDto(a.Id, a.Subject, a.DueDate, a.Status, a.Priority, a.RelatedType, a.RelatedId, a.AccountName)
            {
                Description       = a.Description,
                StartAt           = a.StartAt,
                EndAt             = a.EndAt,
                CompletedAt       = a.CompletedAt,
                Location          = a.Location,
                ActivityType      = a.ActivityType,
                OwnerName         = a.OwnerName,
                ZohoCreatedTime   = a.ZohoCreatedTime,
                ZohoModifiedTime  = a.ZohoModifiedTime,
            })
            .ToList();

        var myMeetingsRaw = await _db.Activities.AsNoTracking()
            .Where(a => (a.Type == "Event" || a.Type == "Meeting")
                        && (a.OwnerUserId == meId2 || a.OwnerUserId == null)
                        && (a.StartAt == null || a.StartAt >= todayStart))
            .OrderBy(a => a.StartAt ?? DateTime.MaxValue)
            .Take(10)
            .Select(a => new
            {
                a.Id, a.Subject, a.StartAt, a.EndAt, a.Location, a.Status, a.RelatedType, a.RelatedId,
                a.Description, a.ZohoCreatedTime, a.ZohoModifiedTime,
                AccountName = a.RelatedType == "Account"
                    ? _db.Accounts.Where(x => x.Id == a.RelatedId).Select(x => x.Name).FirstOrDefault()
                    : null,
                OwnerName = a.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == a.OwnerUserId).Select(u => u.UserName).FirstOrDefault()
            })
            .ToListAsync(ct);
        var myMeetings = myMeetingsRaw
            .Select(m => new MeetingListItemDto(m.Id, m.Subject, m.StartAt, m.EndAt, m.Location, m.Status, m.RelatedType, m.RelatedId, m.AccountName)
            {
                OwnerName        = m.OwnerName,
                Description      = m.Description,
                ZohoCreatedTime  = m.ZohoCreatedTime,
                ZohoModifiedTime = m.ZohoModifiedTime,
            })
            .ToList();

        // Today's leads: prefer leads created today; if zero, widen to last 7 days so
        // the panel still has useful content in a freshly-imported mirror install.
        var todaysLeads = await _db.Leads.AsNoTracking()
            .Where(l => l.CreatedAt >= todayStart && l.CreatedAt < tomorrowStart)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new DashboardLeadDto(l.Id, l.FirstName, l.LastName, l.Company, l.Email, l.Phone, l.Source, l.Status, l.CreatedAt)
            {
                Title           = l.Title,
                Mobile          = l.Mobile,
                Industry        = l.Industry,
                Website         = l.Website,
                ZohoCreatedTime = l.ZohoCreatedTime,
                OwnerName       = l.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == l.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        if (todaysLeads.Count == 0)
        {
            var weekStart = todayStart.AddDays(-7);
            todaysLeads = await _db.Leads.AsNoTracking()
                .Where(l => l.CreatedAt >= weekStart)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new DashboardLeadDto(l.Id, l.FirstName, l.LastName, l.Company, l.Email, l.Phone, l.Source, l.Status, l.CreatedAt)
                {
                    Title           = l.Title,
                    Mobile          = l.Mobile,
                    Industry        = l.Industry,
                    Website         = l.Website,
                    ZohoCreatedTime = l.ZohoCreatedTime,
                    OwnerName       = l.OwnerUserId == null
                        ? null
                        : _db.Users.Where(u => u.Id == l.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
                })
                .ToListAsync(ct);
        }

        // Pipeline funnel: count + total per stage for the current user's open deals.
        var stageGroups = await _db.Opportunities.AsNoTracking()
            .Where(o => o.Status == "Open"
                        && (o.OwnerUserId == meId2 || o.OwnerUserId == null))
            .GroupBy(o => o.StageId)
            .Select(g => new { StageId = g.Key, Count = g.Count(), Total = g.Sum(o => (decimal?)o.Amount) ?? 0m })
            .ToListAsync(ct);

        var stageNames = await _db.PipelineStages.AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.SortOrder })
            .ToListAsync(ct);
        var stageNameById = stageNames.ToDictionary(s => s.Id, s => s.Name);
        var stageOrderById = stageNames.ToDictionary(s => s.Id, s => s.SortOrder);

        var myPipelineByStage = stageGroups
            .Select(g => new PipelineStageTotalDto(
                stageNameById.TryGetValue(g.StageId, out var n) ? n : g.StageId.ToString(),
                g.Count,
                g.Total))
            .OrderBy(g => stageGroups
                .Where(x => stageNameById.TryGetValue(x.StageId, out var n) && n == g.StageName)
                .Select(x => stageOrderById.TryGetValue(x.StageId, out var so) ? so : int.MaxValue)
                .FirstOrDefault())
            .ToList();

        // Welcome name: prefer the authenticated principal's display name, fall back to user-name claim.
        var welcomeName = principal?.Identity?.Name
                          ?? principal?.FindFirst(ClaimTypes.GivenName)?.Value
                          ?? "there";

        return new DashboardSummaryDto(
            welcomeName,
            openLeads,
            openOpps,
            wonThisMonth,
            openTickets,
            totalPipeline,
            wonAmount,
            top,
            recent,
            openLeadsPrev,
            wonPrevMonth,
            wonAmountPrev,
            revenueTarget,
            closingThisMonth,
            myOpenDeals,
            myUntouchedDeals,
            myCallsToday,
            myLeadsCount,
            myOpenTaskDtos,
            myMeetings,
            todaysLeads,
            myPipelineByStage);
    }
}
