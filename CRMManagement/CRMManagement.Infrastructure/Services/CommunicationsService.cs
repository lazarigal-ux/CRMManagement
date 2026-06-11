using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class CommunicationsService : ICommunicationsService
{
    private readonly AppDbContext _db;
    public CommunicationsService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommunicationRecordDto>> ListForEntityAsync(
        AiEntityKind kind, Guid id, int limit, CancellationToken ct)
    {
        var q = _db.Set<CommunicationRecord>().AsNoTracking().AsQueryable();
        q = kind switch
        {
            AiEntityKind.Contact     => q.Where(c => c.ContactId == id),
            AiEntityKind.Account     => q.Where(c => c.AccountId == id),
            AiEntityKind.Opportunity => q.Where(c => c.OpportunityId == id),
            AiEntityKind.Lead        => q.Where(c => c.LeadId == id),
            _ => q.Where(_ => false),
        };

        return await q
            .OrderByDescending(c => c.OccurredAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(c => new CommunicationRecordDto(
                c.Id, c.Provider, c.Direction, c.OccurredAt,
                c.FromAddress, c.ToAddress, c.Subject, c.Body,
                c.ContactId, c.AccountId, c.OpportunityId, c.LeadId))
            .ToListAsync(ct);
    }

    public async Task<Guid> IngestAsync(IngestCommunicationDto dto, CancellationToken ct)
    {
        // Try to attach by phone or email. Cheap heuristics — refined in Phase 2.
        var (contactId, accountId) = await ResolveByAddressAsync(dto, ct);

        var record = new CommunicationRecord
        {
            Id = Guid.NewGuid(),
            Provider = dto.Provider,
            Direction = dto.Direction,
            OccurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt,
            FromAddress = dto.FromAddress,
            ToAddress = dto.ToAddress,
            Subject = dto.Subject,
            Body = dto.Body,
            ExternalId = dto.ExternalId,
            ContactId = contactId,
            AccountId = accountId,
        };

        _db.Set<CommunicationRecord>().Add(record);
        await _db.SaveChangesAsync(ct);
        return record.Id;
    }

    private async Task<(Guid? contactId, Guid? accountId)> ResolveByAddressAsync(IngestCommunicationDto dto, CancellationToken ct)
    {
        var addresses = new[] { dto.FromAddress, dto.ToAddress }
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!.Trim())
            .ToArray();
        if (addresses.Length == 0) return (null, null);

        var contact = await _db.Contacts
            .AsNoTracking()
            .Where(c => addresses.Contains(c.Email!) || addresses.Contains(c.Phone!) || addresses.Contains(c.Mobile!))
            .Select(c => new { c.Id, c.AccountId })
            .FirstOrDefaultAsync(ct);
        if (contact is not null) return (contact.Id, contact.AccountId);

        var account = await _db.Accounts
            .AsNoTracking()
            .Where(a => addresses.Contains(a.Email!) || addresses.Contains(a.Phone!))
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        return (null, account);
    }
}
