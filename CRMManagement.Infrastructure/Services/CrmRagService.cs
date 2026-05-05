using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class CrmRagService : ICrmRagService
{
    private readonly AppDbContext _db;
    public CrmRagService(AppDbContext db) => _db = db;

    public async Task<EntityContextDto?> LoadContextAsync(AiEntityKind kind, Guid id, CancellationToken ct)
    {
        return kind switch
        {
            AiEntityKind.Opportunity => await LoadOpportunityAsync(id, ct),
            AiEntityKind.Lead        => await LoadLeadAsync(id, ct),
            AiEntityKind.Contact     => await LoadContactAsync(id, ct),
            AiEntityKind.Account     => await LoadAccountAsync(id, ct),
            _ => null,
        };
    }

    private async Task<EntityContextDto?> LoadOpportunityAsync(Guid id, CancellationToken ct)
    {
        var opp = await _db.Opportunities
            .AsNoTracking()
            .Include(o => o.Account)
            .Include(o => o.Stage)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (opp is null) return null;

        var snapshot =
            $"Deal: {opp.Name}\n" +
            $"Account: {opp.Account?.Name ?? "(none)"}\n" +
            $"Amount: {opp.Amount:0.##} {opp.Currency}\n" +
            $"Stage: {opp.Stage?.Name ?? "(none)"} ({opp.Probability}%)\n" +
            $"Status: {opp.Status}\n" +
            $"Close date: {opp.CloseDate:yyyy-MM-dd}\n" +
            $"Next step: {opp.NextStep ?? "(none)"}\n" +
            $"Description: {opp.Description ?? "(none)"}";

        var activities = await LoadRelatedActivitiesAsync("Opportunity", id, ct);
        var notes = await LoadRelatedNotesAsync("Opportunity", id, ct);
        var comms = await LoadCommsAsync(opportunityId: id, accountId: opp.AccountId, ct: ct);

        return new EntityContextDto(
            AiEntityKind.Opportunity, id, opp.Name, snapshot, activities, comms, notes);
    }

    private async Task<EntityContextDto?> LoadLeadAsync(Guid id, CancellationToken ct)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null) return null;

        var fullName = $"{lead.FirstName} {lead.LastName}".Trim();
        var snapshot =
            $"Lead: {fullName}\n" +
            $"Company: {lead.Company ?? "(none)"}\n" +
            $"Title: {lead.Title ?? "(none)"}\n" +
            $"Email: {lead.Email ?? "(none)"}\n" +
            $"Phone: {lead.Phone ?? "(none)"}\n" +
            $"Source: {lead.Source ?? "(unknown)"}\n" +
            $"Status: {lead.Status}\n" +
            $"Score: {lead.Score}\n" +
            $"Description: {lead.Description ?? "(none)"}";

        var activities = await LoadRelatedActivitiesAsync("Lead", id, ct);
        var notes = await LoadRelatedNotesAsync("Lead", id, ct);
        var comms = await LoadCommsAsync(leadId: id, ct: ct);

        return new EntityContextDto(AiEntityKind.Lead, id, fullName, snapshot, activities, comms, notes);
    }

    private async Task<EntityContextDto?> LoadContactAsync(Guid id, CancellationToken ct)
    {
        var contact = await _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contact is null) return null;

        var fullName = $"{contact.FirstName} {contact.LastName}".Trim();
        var account = contact.AccountId.HasValue
            ? await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == contact.AccountId, ct)
            : null;

        var snapshot =
            $"Contact: {fullName}\n" +
            $"Account: {account?.Name ?? "(none)"}\n" +
            $"Title: {contact.Title ?? "(none)"}\n" +
            $"Email: {contact.Email ?? "(none)"}\n" +
            $"Phone: {contact.Phone ?? "(none)"}\n" +
            $"Mobile: {contact.Mobile ?? "(none)"}\n" +
            $"Description: {contact.Description ?? "(none)"}";

        var activities = await LoadRelatedActivitiesAsync("Contact", id, ct);
        var notes = await LoadRelatedNotesAsync("Contact", id, ct);
        var comms = await LoadCommsAsync(contactId: id, accountId: contact.AccountId, ct: ct);

        return new EntityContextDto(AiEntityKind.Contact, id, fullName, snapshot, activities, comms, notes);
    }

    private async Task<EntityContextDto?> LoadAccountAsync(Guid id, CancellationToken ct)
    {
        var acc = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (acc is null) return null;

        var snapshot =
            $"Account: {acc.Name}\n" +
            $"Industry: {acc.Industry ?? "(unknown)"}\n" +
            $"Website: {acc.Website ?? "(none)"}\n" +
            $"Phone: {acc.Phone ?? "(none)"}\n" +
            $"Annual revenue: {acc.AnnualRevenue?.ToString("0.##") ?? "(unknown)"}\n" +
            $"Employees: {acc.EmployeeCount?.ToString() ?? "(unknown)"}\n" +
            $"Description: {acc.Description ?? "(none)"}";

        var activities = await LoadRelatedActivitiesAsync("Account", id, ct);
        var notes = await LoadRelatedNotesAsync("Account", id, ct);
        var comms = await LoadCommsAsync(accountId: id, ct: ct);

        return new EntityContextDto(AiEntityKind.Account, id, acc.Name, snapshot, activities, comms, notes);
    }

    private async Task<IReadOnlyList<string>> LoadRelatedActivitiesAsync(string relatedType, Guid id, CancellationToken ct)
    {
        return await _db.Activities
            .AsNoTracking()
            .Where(a => a.RelatedType == relatedType && a.RelatedId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .Select(a =>
                $"[{a.CreatedAt:yyyy-MM-dd}] {a.Type}: {a.Subject}" +
                (string.IsNullOrWhiteSpace(a.Description) ? "" : $" — {a.Description}"))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<string>> LoadRelatedNotesAsync(string relatedType, Guid id, CancellationToken ct)
    {
        return await _db.Notes
            .AsNoTracking()
            .Where(n => n.RelatedType == relatedType && n.RelatedId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .Select(n => $"[{n.CreatedAt:yyyy-MM-dd}] {n.Body}")
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<string>> LoadCommsAsync(
        Guid? contactId = null,
        Guid? accountId = null,
        Guid? opportunityId = null,
        Guid? leadId = null,
        CancellationToken ct = default)
    {
        var q = _db.Set<CommunicationRecord>().AsNoTracking().AsQueryable();
        q = q.Where(c =>
            (contactId.HasValue     && c.ContactId     == contactId) ||
            (accountId.HasValue     && c.AccountId     == accountId) ||
            (opportunityId.HasValue && c.OpportunityId == opportunityId) ||
            (leadId.HasValue        && c.LeadId        == leadId));

        return await q
            .OrderByDescending(c => c.OccurredAt)
            .Take(10)
            .Select(c => $"[{c.OccurredAt:yyyy-MM-dd HH:mm}] {c.Direction} {c.Provider}" +
                         (string.IsNullOrWhiteSpace(c.Subject) ? "" : $" — {c.Subject}") +
                         (string.IsNullOrWhiteSpace(c.Body) ? "" : $": {Trim(c.Body!, 240)}"))
            .ToListAsync(ct);
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
