using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class TimelineService : ITimelineService
{
    private readonly AppDbContext _db;
    public TimelineService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TimelineItemDto>> GetForEntityAsync(
        AiEntityKind kind, Guid id, int limit, CancellationToken ct)
    {
        var relatedType = kind.ToString();
        limit = Math.Clamp(limit, 1, 200);

        var activitiesTask = _db.Activities
            .AsNoTracking()
            .Where(a => a.RelatedType == relatedType && a.RelatedId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new TimelineItemDto(
                a.Id,
                TimelineItemKind.Activity,
                a.CreatedAt,
                null,
                $"{a.Type}: {a.Subject}",
                a.Description ?? "",
                null,
                a.Status))
            .ToListAsync(ct);

        var notesTask = _db.Notes
            .AsNoTracking()
            .Where(n => n.RelatedType == relatedType && n.RelatedId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new TimelineItemDto(
                n.Id,
                TimelineItemKind.Note,
                n.CreatedAt,
                null,
                "Note",
                n.Body,
                null,
                null))
            .ToListAsync(ct);

        var commsQ = _db.Communications.AsNoTracking().AsQueryable();
        commsQ = kind switch
        {
            AiEntityKind.Contact     => commsQ.Where(c => c.ContactId == id),
            AiEntityKind.Account     => commsQ.Where(c => c.AccountId == id),
            AiEntityKind.Opportunity => commsQ.Where(c => c.OpportunityId == id),
            AiEntityKind.Lead        => commsQ.Where(c => c.LeadId == id),
            _ => commsQ.Where(_ => false),
        };

        var commsTask = commsQ
            .OrderByDescending(c => c.OccurredAt)
            .Take(limit)
            .Select(c => new TimelineItemDto(
                c.Id,
                c.Provider == "email" ? TimelineItemKind.Email
                    : c.Provider == "whatsapp" ? TimelineItemKind.WhatsApp
                    : TimelineItemKind.Other,
                c.OccurredAt,
                c.Direction,
                c.Subject ?? (c.Provider == "whatsapp" ? "WhatsApp message" : c.Provider),
                c.Body ?? "",
                c.FromAddress ?? c.ToAddress,
                c.Provider))
            .ToListAsync(ct);

        await Task.WhenAll(activitiesTask, notesTask, commsTask);

        return activitiesTask.Result
            .Concat(notesTask.Result)
            .Concat(commsTask.Result)
            .OrderByDescending(t => t.At)
            .Take(limit)
            .ToList();
    }
}
